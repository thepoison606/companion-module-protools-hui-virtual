using Windows.Devices.Midi2;
using Windows.Devices.Midi2.Enumeration;
using Windows.Devices.Midi2.Transports.Virtual;
using Windows.Devices.Midi2.Utilities.Messages;

namespace ProToolsHuiBridge;

internal sealed class MidiVirtualDeviceHost : IDisposable
{
    private readonly string _endpointName;
    private readonly int _releaseMs;
    private readonly JsonProtocol _json;
    private readonly object _sendLock = new();

    private MidiSession? _session;
    private MidiEndpointConnection? _connection;
    private MidiVirtualDevice? _virtualDevice;
    private HuiController? _hui;

    public MidiVirtualDeviceHost(string endpointName, int releaseMs, JsonProtocol json)
    {
        _endpointName = endpointName;
        _releaseMs = releaseMs;
        _json = json;
    }

    public void Start()
    {
        if (!MidiApi.EnsureServiceAvailable())
            throw new InvalidOperationException("Windows MIDI Services is not available. Install/enable Windows MIDI Services and try again.");

        if (!MidiVirtualDeviceManager.IsTransportAvailable)
            throw new InvalidOperationException("Windows MIDI Services Virtual Device transport is not available on this system.");

        var config = BuildCreationConfig();

        _session = MidiSession.Create($"Companion HUI: {_endpointName}");
        if (_session is null)
            throw new InvalidOperationException("Unable to create Windows MIDI Services session.");

        _virtualDevice = MidiVirtualDeviceManager.CreateVirtualDevice(config);
        if (_virtualDevice is null)
            throw new InvalidOperationException("Unable to create Windows MIDI Services virtual device.");

        // Let the virtual-device plugin consume endpoint discovery/config messages itself.
        _virtualDevice.SuppressHandledMessages = true;

        _connection = _session.CreateEndpointConnection(_virtualDevice.DeviceEndpointDeviceId);
        if (_connection is null)
            throw new InvalidOperationException("Unable to create device-side MIDI endpoint connection.");

        _connection.AddMessageProcessingPlugin(_virtualDevice);
        _hui = new HuiController(SendMidi1, _json, _releaseMs);
        _connection.MessageReceived += OnMidiMessageReceived;

        if (!_connection.Open())
            throw new InvalidOperationException("Unable to open device-side MIDI endpoint connection.");

        _json.Send(new
        {
            @event = "ready",
            endpoint = _endpointName,
            midi1CompatibilityPortsRequested = !config.CreateOnlyUmpEndpoints
        });
        SendState();
    }

    public async Task ToggleMuteAsync(int track)
    {
        EnsureStarted();
        await _hui!.ToggleMuteAsync(track).ConfigureAwait(false);
    }

    public async Task SetMuteAsync(int track, bool muted)
    {
        EnsureStarted();
        await _hui!.SetMuteAsync(track, muted).ConfigureAwait(false);
    }

    public void SendState()
    {
        EnsureStarted();
        _json.Send(new
        {
            @event = "state",
            connected = _hui!.Connected,
            mutes = _hui.GetMuteSnapshot(),
        });
    }

    private MidiVirtualDeviceCreationConfig BuildCreationConfig()
    {
        var declaredEndpointInfo = new MidiDeclaredEndpointInfo
        {
            Name = _endpointName,
            ProductInstanceId = "COMPANION_PT_HUI_001",
            SpecificationVersionMajor = 1,
            SpecificationVersionMinor = 1,
            SupportsMidi10Protocol = true,
            SupportsMidi20Protocol = false,
            SupportsReceivingJitterReductionTimestamps = false,
            SupportsSendingJitterReductionTimestamps = false,
            HasStaticFunctionBlocks = true,
        };

        var declaredDeviceIdentity = new MidiDeclaredDeviceIdentity();

        var userSuppliedInfo = new MidiEndpointUserSuppliedInfo
        {
            Name = _endpointName,
            Description = "Bitfocus Companion prototype HUI endpoint for Pro Tools",
        };

        var config = new MidiVirtualDeviceCreationConfig(
            _endpointName,
            "Virtual HUI MIDI 1.0 endpoint for Pro Tools mute control",
            "Companion prototype",
            declaredEndpointInfo,
            declaredDeviceIdentity,
            userSuppliedInfo
        )
        {
            // Critical for Pro Tools / legacy MIDI clients: ask Windows MIDI Services
            // to create MIDI 1.0 compatibility ports in addition to the UMP endpoint.
            CreateOnlyUmpEndpoints = false,
        };

        var huiBlock = new MidiFunctionBlock
        {
            Number = 0,
            Name = "HUI Control",
            IsActive = true,
            UIHint = MidiFunctionBlockUIHint.Bidirectional,
            FirstGroup = new MidiGroup(0),
            GroupCount = 1,
            Direction = MidiFunctionBlockDirection.Bidirectional,
            RepresentsMidi10Connection = MidiFunctionBlockRepresentsMidi10Connection.YesBandwidthUnrestricted,
            MaxSystemExclusive8Streams = 0,
            MidiCIMessageVersionFormat = 0,
        };

        config.FunctionBlocks.Add(huiBlock);
        return config;
    }

    private void OnMidiMessageReceived(IMidiMessageReceivedEventSource sender, MidiMessageReceivedEventArgs args)
    {
        var word = args.PeekFirstWord();
        var messageType = (byte)((word >> 28) & 0x0F);

        // UMP message type 0x2 is MIDI 1.0 Channel Voice, exactly what HUI uses here.
        if (messageType != 0x02) return;

        var status = (byte)((word >> 16) & 0xFF);
        var data1 = (byte)((word >> 8) & 0xFF);
        var data2 = (byte)(word & 0xFF);

        _hui?.HandleMidi1(status, data1, data2);
    }

    private void SendMidi1(byte status, byte data1, byte data2)
    {
        var connection = _connection;
        if (connection is null) return;

        var message = MidiMessageConverter.ConvertMidi1Message(
            MidiClock.TimestampConstantSendImmediately,
            new MidiGroup(0),
            status,
            data1,
            data2
        );

        lock (_sendLock)
        {
            var result = connection.SendSingleMessagePacket(message);
            if (MidiEndpointConnection.SendMessageFailed(result))
            {
                _json.Send(new { @event = "warning", message = $"MIDI send failed: {result}" });
            }
        }
    }

    private void EnsureStarted()
    {
        if (_connection is null || _hui is null)
            throw new InvalidOperationException("Virtual MIDI device is not started.");
    }

    public void Dispose()
    {
        try
        {
            if (_connection is not null)
                _connection.MessageReceived -= OnMidiMessageReceived;
        }
        catch
        {
            // best-effort shutdown
        }

        _hui?.Dispose();
        _hui = null;

        try
        {
            if (_session is not null && _connection is not null)
                _session.DisconnectEndpointConnection(_connection.ConnectionId);
        }
        catch
        {
            // best-effort shutdown
        }

        _connection = null;
        _virtualDevice = null;
        _session?.Dispose();
        _session = null;
    }
}
