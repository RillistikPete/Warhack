using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WarHack.Blizzard.Interfaces;
using WarHack.Data;
using WarHack.Logging;
using WarHack.Models;
using WarHack.Security;

namespace WarHack.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private static readonly DateTimeOffset StartedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime();
    private readonly WarHackDbContext _db;
    private readonly IBlizzardTokenService _tokens;
    private readonly LogShipChannel _logChannel;
    private readonly IHostEnvironment _env;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        WarHackDbContext db,
        IBlizzardTokenService tokens,
        LogShipChannel logChannel,
        IHostEnvironment env,
        ILogger<HealthController> logger)
    {
        _db = db;
        _tokens = tokens;
        _logChannel = logChannel;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Proves the process is up and answering
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new { status = "alive", utc = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Confirms an API key works and reports what it is allowed to do
    /// </summary>
    /// <remarks>
    /// It's not a health check, so it has an absolute route - /api/caller
    /// </remarks>
    [HttpGet("/api/caller")]
    [Authorize(AuthenticationSchemes = ApiKeyDefaults.Scheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ApiCaller()
    {
        return Ok(new
        {
            client = User.Identity?.Name,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            scheme = User.Identity?.AuthenticationType
        });
    }

    /// <summary>Health report</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = ApiKeyDefaults.Scheme, Roles = "Diagnostics")]
    [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthReport), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var checks = new List<HealthCheck>
        {
            await CheckDatabaseAsync(ct),
            CheckBlizzardToken(),
            CheckLogQueue()
        };

        var healthy = checks.All(c => c.Ok);

        var report = new HealthReport(
            Status: healthy ? "healthy" : "unhealthy",
            Environment: _env.EnvironmentName,
            Version: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            UtcNow: DateTimeOffset.UtcNow,
            Uptime: DateTimeOffset.UtcNow - StartedAt,
            Checks: checks);

        return healthy ? Ok(report) : StatusCode(StatusCodes.Status503ServiceUnavailable, report);
    }

    private async Task<HealthCheck> CheckDatabaseAsync(CancellationToken ct)
    {
        // prevent hanging if db is slow or hanging
        using var ltSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ltSource.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            var timer = Stopwatch.StartNew();
            var reachable = await _db.Database.CanConnectAsync(ltSource.Token);
            timer.Stop();

            return new HealthCheck("database", reachable, reachable ? $"{timer.ElapsedMilliseconds} ms" : "could not connect");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check could not reach the database.");

            return new HealthCheck("database", false, ex.GetType().Name);
        }
    }

    private HealthCheck CheckBlizzardToken()
    {
        var expiresAt = _tokens.ExpiresAt;

        if (!_tokens.HasToken || expiresAt is null)
        {
            return new HealthCheck("blizzard-token", false, "no cached token");
        }

        var remaining = expiresAt.Value - DateTimeOffset.UtcNow;

        return new HealthCheck("blizzard-token", true, $"refreshes in {remaining.TotalMinutes:F0} min");
    }

    private HealthCheck CheckLogQueue()
    {
        var depth = _logChannel.QueueDepth;

        if (depth < 0)
        {
            return new HealthCheck("log-queue", true, "depth unavailable");
        }

        // flag when channel drops the oldest row at capacity
        // to avoid silently losing log lines
        var healthy = depth < LogShipChannel.PressureThreshold;

        return new HealthCheck("log-queue", healthy, $"{depth}/{LogShipChannel.Capacity} queued");
    }
}
