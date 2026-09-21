using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using WarHack.Blizzard.Interfaces;
using WarHack.Logging;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Singleton service that holds Blizzard token for app lifetime.
/// </summary>
/// <remarks>
/// Holding an HttpClient in a field here will pin one handler forever,
/// defeating the purpose of using the factory at all.
/// </remarks>
public sealed class BlizzardTokenService : IBlizzardTokenService
{
    public const string HttpClientName = "blizzard-oauth-client";
    private readonly IHttpClientFactory _clientFactory;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardTokenService> _log;
    private readonly ILogShipHarbor _logBoat;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public BlizzardTokenService(IHttpClientFactory clientFactory, IOptions<BlizzardOptions> options, ILogger<BlizzardTokenService> log, ILogShipHarbor logBoat)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _log = log;
        _logBoat = logBoat;
    }

    private bool HasValidToken => _accessToken is not null && DateTimeOffset.UtcNow < _expiresAt;

    /// <inheritdoc />
    public bool HasToken => HasValidToken;

    /// <inheritdoc />
    /// <remarks>
    /// Read without taking the semaphore. A caller racing a refresh can see a
    /// slightly stale expiry, which is acceptable for reporting and not worth
    /// making every request wait on the gate.
    /// </remarks>
    public DateTimeOffset? ExpiresAt => _accessToken is null ? null : _expiresAt;

    public void ClearToken()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MinValue;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (HasValidToken)
        {
            return _accessToken;
        }

        await _semaphore.WaitAsync(ct);
        try
        {
            // stops concurrent callers from all refreshing at once
            if (HasValidToken)
            {
                return _accessToken;
            }

            var token = await FetchTokenAsync(ct);
            if (token is null)
            {
                return null;
            }

            Store(token);
            return _accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Proactive refresh with retriever bg service
    /// </summary>
    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var token = await FetchTokenAsync(ct);
            if (token is null)
            {
                return false;
            }

            Store(token);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stores token values
    /// </summary>

    private void Store(BlizzardToken token)
    {
        _accessToken = token.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow
            .AddSeconds(token.ExpiresIn)
            .Subtract(_options.TokenRefreshTime);

        _log.LogInformation("Acquired Blizzard token, valid until {ExpiresAt:u}.", _expiresAt);
    }

    private async Task<BlizzardToken?> FetchTokenAsync(CancellationToken ct)
    {
        try
        {
            var tokenClient = _clientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                ])
            };

            // blizzard asks for "basic" header
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await tokenClient.SendAsync(request, ct);
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                if (status == 401)
                {
                    _log.LogError("Blizzard rejected the client credentials. Check Blizzard:ClientId and Blizzard:ClientSecret.");
                    _logBoat.Ship($"Blizzard rejected the client credentials (401) at {_options.TokenUrl}.",
                                  "Error", nameof(BlizzardTokenService));
                }
                else
                {
                    _log.LogError("Token request failed: {Status} {Reason}", status, response.ReasonPhrase);
                }
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<BlizzardToken>(ct);

            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                _log.LogError("Token endpoint returned no access_token.");
                return null;
            }

            return token;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
        {
            _log.LogError(ex, "Blizzard token request threw.");
            return null;
        }
    }
}
