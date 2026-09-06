using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using WarHack.Blizzard;
using WarHack.Domain;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Typed client for Blizzard's OAuth2 client-credentials grant. Deliberately has
/// its own HttpClient with no auth handler attached -- otherwise fetching a token
/// would require a token.
/// </summary>
public sealed class BlizzardTokenClient : IBlizzardTokenClient
{
    private readonly HttpClient _tokenClient;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardTokenClient> _log;

    public BlizzardTokenClient(HttpClient http, IOptions<BlizzardOptions> options, ILogger<BlizzardTokenClient> log)
    {
        _tokenClient = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<ApiResult<BlizzardToken>> RequestTokenAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                ])
            };

            // Client id/secret go in a Basic header, not the body.
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await _tokenClient.SendAsync(request, ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                // A 401 here means the credentials themselves are wrong -- say so
                // plainly rather than letting it surface as a confusing API error.
                var detail = status == 401
                    ? "Blizzard rejected the client credentials. Check Blizzard:ClientId and Blizzard:ClientSecret."
                    : $"Token request failed: {status} {response.ReasonPhrase}";

                _log.LogError("{Detail}", detail);
                return ApiResult<BlizzardToken>.Fail(detail, status);
            }

            var token = await response.Content.ReadFromJsonAsync<BlizzardToken>(ct);

            return token is null || string.IsNullOrWhiteSpace(token.AccessToken)
                ? ApiResult<BlizzardToken>.Fail("Token endpoint returned no access_token.", status)
                : ApiResult<BlizzardToken>.Ok(token, status);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
        {
            _log.LogError(ex, "Blizzard token request threw.");
            return ApiResult<BlizzardToken>.Fail(ex.Message);
        }
    }
}
