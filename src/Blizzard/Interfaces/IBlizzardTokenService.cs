namespace WarHack.Blizzard.Interfaces;

/// <summary>
/// Blizzard OAuth Service integration
/// </summary>
public interface IBlizzardTokenService
{
    /// <summary>
    /// The current token, or fetches
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken ct = default);

    void ClearToken();
}
