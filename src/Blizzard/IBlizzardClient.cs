using WarHack.Domain;

namespace WarHack.Blizzard;

/// <summary>
/// Transport-level access to the Blizzard API. Auth, retries, timeouts and the
/// namespace/locale query parameters are applied by the handler pipeline, so
/// callers only supply the resource path and any extra query values.
/// </summary>
public interface IBlizzardClient
{
    /// <summary>GET a single resource and deserialize it to <typeparamref name="T"/>.</summary>
    /// <param name="resourceUri">Path relative to the API base, e.g. "profile/wow/character/tichondrius/gav".</param>
    /// <param name="query">Extra query values. Null values are skipped.</param>
    Task<ApiResult<T>> GetAsync<T>(
        string resourceUri,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken ct = default);

    /// <summary>POST a body and deserialize the response.</summary>
    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string resourceUri,
        TRequest body,
        CancellationToken ct = default);

    /// <summary>PUT a body and deserialize the response.</summary>
    Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
        string resourceUri,
        TRequest body,
        CancellationToken ct = default);

    /// <summary>DELETE a resource. Returns status only.</summary>
    Task<ApiResult> DeleteAsync(
        string resourceUri,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken ct = default);
}
