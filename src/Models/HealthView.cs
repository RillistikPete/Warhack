namespace WarHack.Models;

/// <summary>
/// Process health
/// </summary>
/// <param name="Name">Dependency being checked.</param>
/// <param name="Ok">False when unhealthy</param>
/// <param name="Detail">Notes</param>
public sealed record HealthCheck(string Name, bool Ok, string? Detail = null);

/// <summary>
/// What the endpoint returns.
/// </summary>
public sealed record HealthReport(
    string Status,
    string Environment,
    string Version,
    DateTimeOffset UtcNow,
    TimeSpan Uptime,
    IReadOnlyList<HealthCheck> Checks);
