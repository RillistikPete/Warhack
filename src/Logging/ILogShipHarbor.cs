using System.Threading.Channels;

namespace WarHack.Logging;

public interface ILogShipHarbor
{
    /// <summary>Write to the normal ILogger pipeline (console, Azure, wherever it's wired).</summary>
    void PrintToOut(string message);

    /// <summary>
    /// Queue a row for the log table. Returns immediately — the write happens on a
    /// background writer, so callers never block on the database.
    /// </summary>
    void Ship(string message, string level = "Information", string? logger = null);
}

/// <summary>Singleton hand-off between callers and <see cref="LogShipWriter"/>.</summary>
public sealed class LogShipChannel
{
    private readonly Channel<LogQueue> _channel = Channel.CreateBounded<LogQueue>(
        new BoundedChannelOptions(capacity: 2048)
        {
            // Never block the request thread. Under a flood, drop the oldest queued row.
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ChannelWriter<LogQueue> Writer => _channel.Writer;
    public ChannelReader<LogQueue> Reader => _channel.Reader;
}

public sealed class Logship : ILogShipHarbor
{
    private readonly ILogger<Logship> _logger;
    private readonly LogShipChannel _channel;

    public Logship(ILogger<Logship> logger, LogShipChannel channel)
    {
        _logger = logger;
        _channel = channel;
    }

    public void PrintToOut(string message) => _logger.LogInformation("{Message}", message);

    public void Ship(string message, string level = "Information", string? logger = null)
    {
        var entry = new LogQueue
        {
            DateAdded = DateTimeOffset.UtcNow,
            Level = level,
            Message = message,
            ProjectName = "WarHack",
            Logger = logger ?? nameof(Logship)
        };

        // TryWrite, not WriteAsync: logging must never be able to stall the caller.
        if (!_channel.Writer.TryWrite(entry))
            _logger.LogWarning("Log ship queue full; dropped a row.");
    }
}
