using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using WarHack.Blizzard;
using WarHack.Domain;
using WarHack.Logging;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Typed client for Blizzard's OAuth2 client-credentials
/// </summary>
public sealed class BlizzardTokenClient : IBlizzardTokenClient
{
    private readonly HttpClient _tokenClient;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardTokenClient> _log;
    private readonly ILogShipHarbor _logBoat;

    public BlizzardTokenClient(
        HttpClient http,
        IOptions<BlizzardOptions> options,
        ILogger<BlizzardTokenClient> log,
        ILogShipHarbor ship)
    {
        _tokenClient = http;
        _options = options.Value;
        _log = log;
        _logBoat = ship;
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

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await _tokenClient.SendAsync(request, ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var detail = $"Token request failed: {status} {response.ReasonPhrase}";
                _log.LogError("{Detail}", detail);

                if (status == 401)
                {
                    detail = "Blizzard rejected the client credentials. Check Blizzard:ClientId and Blizzard:ClientSecret.";
                    _logBoat.Ship($"Blizzard rejected the client credentials (401) at {_options.TokenUrl}.", "Error", nameof(BlizzardTokenClient));
                }

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
