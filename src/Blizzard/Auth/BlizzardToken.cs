using System.Text.Json.Serialization;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// The client-credentials token response. Blizzard uses the standard OAuth2
/// snake_case field names; these are explicit rather than relying on a naming
/// policy, because this client is separate from the API client.
/// </summary>
public sealed record BlizzardToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
