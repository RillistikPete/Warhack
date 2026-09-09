using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WarHack.Data;

namespace WarHack.Logging;

public sealed class LogRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LogRetentionOptions _options;
    private readonly ILogger<LogRetentionService> _log;

    public LogRetentionService(IServiceScopeFactory scopeFactory, IOptions<LogRetentionOptions> options, ILogger<LogRetentionService> log)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _log.LogInformation("Log retention is not enabled.");
            return;
        }
        try
        {
            await Task.Delay(_options.StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(_options.Interval);

            do
            {
                await SweepAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {

        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - _options.Retention;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WarHackDbContext>();

            // nothing is tracked
            var deleted = await db.LogQueue.Where(l => l.DateAdded < cutoff).ExecuteDeleteAsync(ct);

            if (deleted > 0)
            {
                _log.LogInformation("Log retention removed {Count} rows older than {Cutoff:u}.", deleted, cutoff);
            }
            else
            {
                _log.LogDebug("Log retention found nothing older than {Cutoff:u}.", cutoff);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Log retention sweep failed; will retry on the next interval.");
        }
    }
}
