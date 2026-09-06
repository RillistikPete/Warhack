using System.Net;
using System.Net.Http.Headers;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Attaches the Blizzard bearer token to every outbound request. All token
/// acquisition and caching lives in <see cref="IBlizzardTokenProvider"/>; this
/// handler only decides when to use one and when to give up.
/// </summary>
public sealed class BlizzardAuthHandler : DelegatingHandler
{
    private readonly IBlizzardTokenProvider _tokens;
    private readonly ILogger<BlizzardAuthHandler> _log;

    public BlizzardAuthHandler(IBlizzardTokenProvider tokens, ILogger<BlizzardAuthHandler> log)
    {
        _tokens = tokens;
        _log = log;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var first = await _tokens.GetAccessTokenAsync(ct);
        if (!first.IsSuccess || first.Value is null)
        {
            // No token means no point sending the request at all.
            _log.LogError("Skipping {Uri}: {Error}", request.RequestUri, first.Error);
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "Blizzard authentication failed"
            };
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.Value);
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        // A 401 means the token we sent was stale or revoked. Re-authenticate and
        // retry once -- but only if we actually got a DIFFERENT token back. If the
        // same one comes back, the token is fine and the request itself is being
        // refused (wrong namespace, wrong region, missing scope), so a second
        // identical call would just waste a round trip and rate-limit budget.
        _tokens.Invalidate();
        var second = await _tokens.GetAccessTokenAsync(ct);

        if (!second.IsSuccess || second.Value is null ||
            string.Equals(second.Value, first.Value, StringComparison.Ordinal))
        {
            _log.LogError(
                "401 from {Uri} and re-authentication did not produce a new token. Not retrying.",
                request.RequestUri);
            return response;
        }

        _log.LogInformation("401 from {Uri}; refreshed the token and retrying once.", request.RequestUri);
        response.Dispose();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", second.Value);
        return await base.SendAsync(request, ct);
    }
}
