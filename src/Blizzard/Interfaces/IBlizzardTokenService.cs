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

    /// <summary>
    /// Fetch a new token and swap it in, whether or not the current one is still
    /// valid. On failure the existing token is left untouched, so a failed
    /// proactive refresh can never leave the app worse off than before.
    /// Returns true if a new token was stored.
    /// </summary>
    Task<bool> RefreshAsync(CancellationToken ct = default);

    void ClearToken();
}
