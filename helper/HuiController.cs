namespace ProToolsHuiBridge;

internal sealed class HuiController : IDisposable
{
    private const byte DeviceZoneSelectCc = 0x0F;
    private const byte DevicePortSelectCc = 0x2F;
    private const byte HostZoneSelectCc = 0x0C;
    private const byte HostPortSelectCc = 0x2C;
    private const byte MutePort = 0x02;
    private const byte SwitchPressedBit = 0x40;

    private readonly Action<byte, byte, byte> _sendMidi;
    private readonly JsonProtocol _json;
    private readonly int _releaseMs;
    private readonly bool?[] _mutes = new bool?[8];
    private readonly Timer _connectionTimer;
    private readonly object _stateLock = new();

    private int _hostZone = -1;
    private DateTime _lastPingUtc = DateTime.MinValue;
    private bool _connected;

    public HuiController(Action<byte, byte, byte> sendMidi, JsonProtocol json, int releaseMs)
    {
        _sendMidi = sendMidi;
        _json = json;
        _releaseMs = Math.Clamp(releaseMs, 1, 200);
        _connectionTimer = new Timer(CheckConnection, null, 500, 500);
    }

    public bool Connected
    {
        get
        {
            lock (_stateLock) return _connected;
        }
    }

    public bool?[] GetMuteSnapshot()
    {
        lock (_stateLock) return (bool?[])_mutes.Clone();
    }

    public void HandleMidi1(byte status, byte data1, byte data2)
    {
        // HUI host ping. Pro Tools sends 90 00 00 and expects 90 00 7F.
        if (status == 0x90 && data1 == 0x00 && data2 == 0x00)
        {
            _sendMidi(0x90, 0x00, 0x7F);
            MarkPing();
            return;
        }

        // HUI host -> surface switch/LED state. This direction uses CC 0x0C / 0x2C.
        if (status == 0xB0 && data1 == HostZoneSelectCc)
        {
            lock (_stateLock) _hostZone = data2;
            return;
        }

        if (status == 0xB0 && data1 == HostPortSelectCc)
        {
            int zone;
            lock (_stateLock) zone = _hostZone;

            if (zone is >= 0 and < 8 && (data2 & 0x0F) == MutePort)
            {
                var muted = (data2 & SwitchPressedBit) != 0;
                UpdateMute(zone, muted);
            }
        }
    }

    public async Task ToggleMuteAsync(int track)
    {
        ValidateTrack(track);
        var zone = (byte)(track - 1);

        // HUI surface -> host switch click. This direction uses CC 0x0F / 0x2F.
        SendSwitch(zone, pressed: true);
        await Task.Delay(_releaseMs).ConfigureAwait(false);
        SendSwitch(zone, pressed: false);
    }

    public async Task SetMuteAsync(int track, bool muted)
    {
        ValidateTrack(track);

        bool? current;
        lock (_stateLock) current = _mutes[track - 1];

        if (current is null)
        {
            _json.Send(new
            {
                @event = "warning",
                message = $"Mute state for HUI strip {track} is not known yet; setMute was not sent. Use toggleMute or wait for Pro Tools feedback."
            });
            return;
        }

        if (current.Value == muted) return;
        await ToggleMuteAsync(track).ConfigureAwait(false);
    }

    private void SendSwitch(byte zone, bool pressed)
    {
        _sendMidi(0xB0, DeviceZoneSelectCc, zone);
        _sendMidi(0xB0, DevicePortSelectCc, (byte)(MutePort | (pressed ? SwitchPressedBit : 0x00)));
    }

    private void MarkPing()
    {
        bool changed = false;
        lock (_stateLock)
        {
            _lastPingUtc = DateTime.UtcNow;
            if (!_connected)
            {
                _connected = true;
                changed = true;
            }
        }

        if (changed) _json.Send(new { @event = "connected", connected = true });
    }

    private void CheckConnection(object? state)
    {
        bool changed = false;
        lock (_stateLock)
        {
            if (_connected && DateTime.UtcNow - _lastPingUtc > TimeSpan.FromSeconds(3))
            {
                _connected = false;
                changed = true;
            }
        }

        if (changed) _json.Send(new { @event = "connected", connected = false });
    }

    private void UpdateMute(int zeroBasedTrack, bool muted)
    {
        bool changed;
        lock (_stateLock)
        {
            changed = _mutes[zeroBasedTrack] != muted;
            _mutes[zeroBasedTrack] = muted;
        }

        if (changed)
        {
            _json.Send(new { @event = "mute", track = zeroBasedTrack + 1, muted });
        }
    }

    private static void ValidateTrack(int track)
    {
        if (track is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(track), "HUI prototype supports strips 1 through 8.");
    }

    public void Dispose()
    {
        _connectionTimer.Dispose();
    }
}
