using WarHack.Data;

namespace WarHack.Logging;

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
        try
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            {
                var batch = SendUpToBatch();
                if (batch.Count == 0) continue;

                await WriteAsync(batch, stoppingToken);
                await Task.Delay(FlushInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Runs when the host is shutting down
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();

        await base.StopAsync(cancellationToken);

        var remaining = SendUpToBatch(int.MaxValue);
        if (remaining.Count > 0)
        {
            _log.LogInformation("Shutting down: flushing {Count} queued log rows.", remaining.Count);
            await WriteAsync(remaining, cancellationToken);
        }
    }

    private List<LogQueue> SendUpToBatch(int max = MaxBatch)
    {
        var batch = new List<LogQueue>(Math.Min(max, MaxBatch));
        while (batch.Count < max && _channel.Reader.TryRead(out var entry))
            batch.Add(entry);
        return batch;
    }

    private async Task WriteAsync(List<LogQueue> batch, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WarHackDbContext>();
            db.LogQueue.AddRange(batch);
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _log.LogWarning("Shutdown cancelled a write of {Count} log rows.", batch.Count);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to ship {Count} log rows.", batch.Count);
        }
    }
}
