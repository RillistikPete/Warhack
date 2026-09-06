using WarHack.Domain;

namespace WarHack.Blizzard.Auth;

/// <summary>Transport only: one POST to the token endpoint, no caching.</summary>
public interface IBlizzardTokenClient
{
    Task<ApiResult<BlizzardToken>> RequestTokenAsync(CancellationToken ct = default);
}

/// <summary>
/// Caching layer over <see cref="IBlizzardTokenClient"/>. This is what everything else asks for a token.
/// </summary>
public interface IBlizzardTokenProvider
{
    Task<ApiResult<string>> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Drop the cached token so the next call re-authenticates.</summary>
    void Invalidate();
}
