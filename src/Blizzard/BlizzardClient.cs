using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WarHack.Domain;

namespace WarHack.Blizzard;

/// <summary>
/// Typed HttpClient for the Blizzard API. The HttpClient handed in by DI is already
/// configured with the base address, the auth handler, timing and the resilience
/// pipeline, so this class only deals with URIs, serialization and results.
/// </summary>
public sealed class BlizzardClient : IBlizzardClient
{
    // Blizzard speaks snake_case (average_item_level, equipped_items, ...).
    // Setting the policy once here means the DTOs stay clean C# names with no
    // [JsonPropertyName] on every property.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _bClient;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardClient> _log;

    public BlizzardClient(HttpClient bClient, IOptions<BlizzardOptions> options, ILogger<BlizzardClient> log)
    {
        _bClient = bClient;
        _options = options.Value;
        _log = log;
    }

    public async Task<ApiResult<T>> GetAsync<T>(string resourceUri, IReadOnlyDictionary<string, string?>? query = null, CancellationToken ct = default)
    {
        var uri = BuildUri(resourceUri, query);
        try
        {
            using var response = await _bClient.GetAsync(uri, ct);
            return await ReadAsync<T>(response, HttpMethod.Get, uri, ct);
        }
        catch (Exception ex) when (NotCancellation(ex, ct))
        {
            return Failure<T>(ex, HttpMethod.Get, uri);
        }
    }

    public async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string resourceUri, TRequest body, CancellationToken ct = default)
        => await SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Post, resourceUri, body, ct);

    public async Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string resourceUri, TRequest body, CancellationToken ct = default)
        => await SendWithBodyAsync<TRequest, TResponse>(HttpMethod.Put, resourceUri, body, ct);

    public async Task<ApiResult> DeleteAsync(string resourceUri, IReadOnlyDictionary<string, string?>? query = null, CancellationToken ct = default)
    {
        var uri = BuildUri(resourceUri, query);
        try
        {
            using var response = await _bClient.DeleteAsync(uri, ct);
            if (response.IsSuccessStatusCode)
                return ApiResult.Ok((int)response.StatusCode);

            var body = await SafeReadAsync(response, ct);
            _log.LogWarning("DELETE {Uri} -> {Status}. {Body}", uri, (int)response.StatusCode, body);
            return ApiResult.Fail($"{(int)response.StatusCode} {response.ReasonPhrase}", (int)response.StatusCode);
        }
        catch (Exception ex) when (NotCancellation(ex, ct))
        {
            _log.LogError(ex, "DELETE {Uri} failed.", uri);
            return ApiResult.Fail(ex.Message);
        }
    }

    private async Task<ApiResult<TResponse>> SendWithBodyAsync<TRequest, TResponse>(HttpMethod method, string resourceUri, TRequest body, CancellationToken ct)
    {
        var uri = BuildUri(resourceUri, null);
        try
        {
            using var request = new HttpRequestMessage(method, uri)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };
            using var response = await _bClient.SendAsync(request, ct);
            return await ReadAsync<TResponse>(response, method, uri, ct);
        }
        catch (Exception ex) when (NotCancellation(ex, ct))
        {
            return Failure<TResponse>(ex, method, uri);
        }
    }

    private async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, HttpMethod method, string uri, CancellationToken ct)
    {
        var status = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadAsync(response, ct);
            _log.LogWarning("{Method} {Uri} -> {Status}. {Body}", method, uri, status, body);
            return ApiResult<T>.Fail($"{status} {response.ReasonPhrase}", status);
        }

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return value is null
            ? ApiResult<T>.Fail("Response body was empty or null.", status)
            : ApiResult<T>.Ok(value, status);
    }

    /// <summary>Appends the caller's query values plus Blizzard's required namespace/locale.</summary>
    private string BuildUri(string resourceUri, IReadOnlyDictionary<string, string?>? query)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["namespace"] = _options.Namespace,
            ["locale"] = _options.Locale
        };

        if (query is not null)
        {
            foreach (var (key, value) in query)
            {
                if (value is not null) values[key] = value;
            }
        }

        return QueryHelpers.AddQueryString(resourceUri.TrimStart('/'), values);
    }

    private ApiResult<T> Failure<T>(Exception ex, HttpMethod method, string uri)
    {
        _log.LogError(ex, "{Method} {Uri} failed.", method, uri);
        return ApiResult<T>.Fail(ex.Message);
    }

    /// <summary>True for real faults. Cancellation the caller asked for is rethrown, not swallowed.</summary>
    private static bool NotCancellation(Exception ex, CancellationToken ct)
        => !(ex is OperationCanceledException && ct.IsCancellationRequested);

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return body.Length > 500 ? body[..500] + "..." : body;
        }
        catch
        {
            return "<unreadable body>";
        }
    }
}
