namespace WarHack.Logging;

public interface ILogShipHarbor
{
    /// <summary>
    /// Queue a row for the log table. Returns immediately - the write happens on a
    /// background writer, so callers never block on the database.
    /// </summary>
    void Ship(string message, string level = "Information", string? logger = null);
}
