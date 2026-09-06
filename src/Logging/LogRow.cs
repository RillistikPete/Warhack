namespace WarHack.Logging;

/// <summary>
/// Projection target for the log viewer. Not an entity -- EF materializes these
/// straight from a SELECT of exactly these columns, and never tracks them.
/// </summary>
public sealed record LogRow(int Id, DateTimeOffset DateAdded, string Level, string Logger, string Message);

public sealed record LevelCount(string Level, int Count);
