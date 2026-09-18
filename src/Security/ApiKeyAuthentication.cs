using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace WarHack.Security;

/// <summary>
/// Validates the x-api-key header handling inbound requests
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var hdrNameOut))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var supplied = Encoding.UTF8.GetBytes(hdrNameOut.ToString());

        string? matchedName = null;
        ApiClient? matchedClient = null;

        foreach (var (name, client) in Options.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.Key))
            {
                continue;
            }

            // don't leak position
            // diff != 0 is false when the keys don't match, so only sets client name when they do
            if (CryptographicOperations.FixedTimeEquals(supplied, Encoding.UTF8.GetBytes(client.Key)))
            {
                matchedName = name;
                matchedClient = client;
            }
        }

        if (matchedName is null || matchedClient is null)
        {
            Logger.LogWarning("Rejected an invalid API key from {Ip}.", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, matchedName),
            new(ClaimTypes.Name, matchedName)
        };

        claims.AddRange(matchedClient.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, ApiKeyDefaults.Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), ApiKeyDefaults.Scheme);

        Logger.LogDebug("Authenticated API client {Client}.", matchedName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
public static class ApiKeyDefaults
{
    public const string Scheme = "ApiKey";
}

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "ApiKey";
    public string HeaderName { get; set; } = "x-api-key";
    public Dictionary<string, ApiClient> Clients { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ApiClient
{
    public string Key { get; set; } = "";
    public string[] Roles { get; set; } = [];
}