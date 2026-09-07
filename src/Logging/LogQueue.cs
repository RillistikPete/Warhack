using System.ComponentModel.DataAnnotations;

namespace WarHack.Logging;

/// <summary>One shipped log row</summary>
public class LogQueue
{
    public int Id { get; set; }
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(32)]
    public string Level { get; set; } = "Information";

    [MaxLength(128)]
    public string ProjectName { get; set; } = "WarHack";

    [MaxLength(256)]
    public string Logger { get; set; } = "";

    public string Message { get; set; } = "";
}
