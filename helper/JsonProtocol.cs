using System.Text.Json;

namespace ProToolsHuiBridge;

internal sealed class JsonProtocol
{
    private readonly object _writeLock = new();
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public void Send(object payload)
    {
        var line = JsonSerializer.Serialize(payload, Options);
        lock (_writeLock)
        {
            Console.Out.WriteLine(line);
            Console.Out.Flush();
        }
    }

    public void Log(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.Flush();
    }
}
