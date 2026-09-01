using System.Text.Json;

namespace ProToolsHuiBridge;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var json = new JsonProtocol();
        var (endpointName, releaseMs) = ParseArguments(args);

        using var host = new MidiVirtualDeviceHost(endpointName, releaseMs, json);

        try
        {
            host.Start();
        }
        catch (Exception ex)
        {
            json.Send(new { @event = "error", message = ex.Message, detail = ex.ToString() });
            return 2;
        }

        try
        {
            string? line;
            while ((line = await Console.In.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                bool shutdown;
                try
                {
                    shutdown = await HandleCommandAsync(host, line, json).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    json.Send(new { @event = "warning", message = ex.Message });
                    shutdown = false;
                }

                if (shutdown) break;
            }
        }
        finally
        {
            host.Dispose();
        }

        return 0;
    }

    private static async Task<bool> HandleCommandAsync(MidiVirtualDeviceHost host, string line, JsonProtocol json)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        var command = root.TryGetProperty("cmd", out var commandElement) ? commandElement.GetString() : null;

        switch (command)
        {
            case "toggleMute":
                await host.ToggleMuteAsync(ReadTrack(root)).ConfigureAwait(false);
                return false;

            case "setMute":
                if (!root.TryGetProperty("muted", out var mutedElement) ||
                    (mutedElement.ValueKind != JsonValueKind.True && mutedElement.ValueKind != JsonValueKind.False))
                    throw new ArgumentException("setMute requires boolean property 'muted'.");

                await host.SetMuteAsync(ReadTrack(root), mutedElement.GetBoolean()).ConfigureAwait(false);
                return false;

            case "getState":
                host.SendState();
                return false;

            case "shutdown":
                return true;

            default:
                json.Send(new { @event = "warning", message = $"Unknown helper command: {command ?? "(missing)"}" });
                return false;
        }
    }

    private static int ReadTrack(JsonElement root)
    {
        if (!root.TryGetProperty("track", out var trackElement) || !trackElement.TryGetInt32(out var track))
            throw new ArgumentException("Command requires integer property 'track'.");

        if (track is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(track), "Track must be 1 through 8.");

        return track;
    }

    private static (string EndpointName, int ReleaseMs) ParseArguments(string[] args)
    {
        var endpoint = "Companion Pro Tools HUI";
        var releaseMs = 20;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--endpoint" && i + 1 < args.Length)
            {
                endpoint = args[++i].Trim();
            }
            else if (args[i] == "--release-ms" && i + 1 < args.Length && int.TryParse(args[++i], out var parsed))
            {
                releaseMs = Math.Clamp(parsed, 1, 200);
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint)) endpoint = "Companion Pro Tools HUI";
        return (endpoint, releaseMs);
    }
}
