using System.Net;
using System.Net.Http.Headers;
using WarHack.Blizzard.Interfaces;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Delegating handler is middleware for outbound http
/// </summary>
public sealed class BlizzardAuthHandler : DelegatingHandler
{
    private readonly IBlizzardTokenService _tokenService;
    private readonly ILogger<BlizzardAuthHandler> _log;

    public BlizzardAuthHandler(IBlizzardTokenService tokenService, ILogger<BlizzardAuthHandler> log)
    {
        _tokenService = tokenService;
        _log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenService.GetAccessTokenAsync(ct);
        if (token is null)
        {
            _log.LogError("Skipping {Uri}: no Blizzard token available.", request.RequestUri);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "Blizzard authentication failed"
            };
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // 401, so grab new token
        _tokenService.ClearToken();
        var refreshedToken = await _tokenService.GetAccessTokenAsync(ct);

        if (refreshedToken is null || string.Equals(refreshedToken, token, StringComparison.Ordinal))
        {
            _log.LogError("401 from {Uri} and re-authentication did not produce a new token.", request.RequestUri);
            return response;
        }

        _log.LogInformation("401 from {Uri}; refreshed the token and retrying once.", request.RequestUri);
        response.Dispose();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
        return await base.SendAsync(request, ct);
    }
}
