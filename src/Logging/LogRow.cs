namespace WarHack.Logging;

/// <summary>
/// Projection for the log viewer. Not a tracked entity.
/// EF materializes these from a SELECT of exactly these columns.
/// </summary>
public sealed record LogRow(int Id, DateTimeOffset DateAdded, string Level, string Logger, string Message);

public sealed record LevelCount(string Level, int Count);
