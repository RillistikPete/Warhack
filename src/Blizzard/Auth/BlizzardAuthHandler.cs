using System.Net;
using System.Net.Http.Headers;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Delegating handler is middleware for outbound http
/// </summary>
public sealed class BlizzardAuthHandler : DelegatingHandler
{
    private readonly IBlizzardTokenProvider _tokenProvider;
    private readonly ILogger<BlizzardAuthHandler> _log;

    public BlizzardAuthHandler(IBlizzardTokenProvider tokens, ILogger<BlizzardAuthHandler> log)
    {
        _tokenProvider = tokens;
        _log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var getToken = await _tokenProvider.GetAccessTokenAsync(ct);
        if (!getToken.IsSuccess || getToken.Value is null)
        {
            _log.LogError("Skipping {Uri}: {Error}", request.RequestUri, getToken.Error);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "Blizzard authentication failed"
            };
        }
        // I used to use request.Headers.Add("Authorization", $"Bearer {getToken.Value}"),
        // but that's really meant for custom string header values. Using this is a bit safer:
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", getToken.Value);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        _tokenProvider.ClearToken();
        var second = await _tokenProvider.GetAccessTokenAsync(ct);

        if (!second.IsSuccess || second.Value is null ||
            string.Equals(second.Value, getToken.Value, StringComparison.Ordinal))
        {
            _log.LogError("401 from {Uri} and re-authentication did not produce a new token.", request.RequestUri);
            return response;
        }

        _log.LogInformation("401 from {Uri}; refreshed the token and retrying once.", request.RequestUri);
        response.Dispose();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", second.Value);
        return await base.SendAsync(request, ct);
    }
}
