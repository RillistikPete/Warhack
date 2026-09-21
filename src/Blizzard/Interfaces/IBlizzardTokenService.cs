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

    /// <summary>
    /// True when a cached token is present and not yet past its refresh deadline.
    /// Reads the cache only; never triggers a fetch.
    /// </summary>
    bool HasToken { get; }

    /// <summary>
    /// When the cached token stops being used, or null if there is none.
    /// Reads the cache only; never triggers a fetch.
    /// </summary>
    DateTimeOffset? ExpiresAt { get; }
}
