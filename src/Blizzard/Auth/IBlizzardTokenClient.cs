using WarHack.Domain;

namespace WarHack.Blizzard.Auth;

public interface IBlizzardTokenClient
{
    Task<ApiResult<BlizzardToken>> RequestTokenAsync(CancellationToken ct = default);
}

/// <summary>
/// Caching layer over <see cref="IBlizzardTokenClient"/>.
/// </summary>
public interface IBlizzardTokenProvider
{
    Task<ApiResult<string>> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Drop the cached token so the next call re-authenticates.
    /// </summary>
    void ClearToken();
}
