namespace WarHack.Logging;

public sealed class LogRetentionOptions
{
    public const string SectionName = "LogRetention";

    public bool Enabled { get; init; } = true;

    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(12);

    public TimeSpan StartupDelay { get; init; } = TimeSpan.FromMinutes(2);
}
