namespace WarHack.Blizzard.Interfaces;

/// <summary>
/// Blizzard OAuth Service integration interface
/// </summary>
public interface IBlizzardTokenService
{
    /// <summary>
    /// Gets the current token or fetches
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Refresh token
    /// </summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);

    void ClearToken();
}
