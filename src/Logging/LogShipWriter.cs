using Microsoft.EntityFrameworkCore;
using WarHack.Data;

namespace WarHack.Logging;

/// <summary>
/// Drains the log channel into Postgres in batches. One scope and one round trip per
/// batch, off the request path. Replaces the old static LoggerLQ, which opened a
/// DbContext per message and serialized every caller behind a global lock.
/// </summary>
public sealed class LogShipWriter : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);
    private const int MaxBatch = 100;

    private readonly LogShipChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogShipWriter> _log;

    public LogShipWriter(LogShipChannel channel, IServiceScopeFactory scopeFactory, ILogger<LogShipWriter> log)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<LogQueue>(MaxBatch);

        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            batch.Clear();

            while (batch.Count < MaxBatch && _channel.Reader.TryRead(out var entry))
                batch.Add(entry);

            if (batch.Count == 0) continue;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WarHackDbContext>();
                db.LogQueue.AddRange(batch);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to ship {Count} log rows.", batch.Count);
            }

            await Task.Delay(FlushInterval, stoppingToken);
        }
    }
}
