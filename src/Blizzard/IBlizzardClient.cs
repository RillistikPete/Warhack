using WarHack.Domain;

namespace WarHack.Blizzard;

public interface IBlizzardClient
{
    /// <summary>GET a single resource and deserialize it to <typeparamref name="T"/>.</summary>
    /// <param name="resourceUri">Path relative to the API base</param>
    /// <param name="query">Extra query values</param>
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
