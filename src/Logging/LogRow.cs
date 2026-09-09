namespace WarHack.Logging;

/// <summary>
/// Projection for the log viewer. EF does not track, only selects these columns.
/// </summary>
public sealed record LogRow(int Id, DateTimeOffset DateAdded, string Level, string Logger, string Message);

public sealed record LevelCount(string Level, int Count);
