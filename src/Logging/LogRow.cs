namespace WarHack.Logging;

/// <summary>
/// Projection for the log viewer
/// </summary>
/// <remarks>
/// EF does not track the entire entity, it only selects these columns.
/// </remarks>
public sealed record LogRow(int Id, DateTimeOffset DateAdded, string Level, string Logger, string Message);

public sealed record LevelCount(string Level, int Count);
