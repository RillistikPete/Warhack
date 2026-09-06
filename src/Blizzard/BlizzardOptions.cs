namespace WarHack.Blizzard;

public sealed class BlizzardOptions
{
    public const string SectionName = "Blizzard";

    /// <summary>Regional API host, e.g. https://us.api.blizzard.com/</summary>
    public string BaseUrl { get; init; } = "https://us.api.blizzard.com/";

    /// <summary>OAuth2 token endpoint (region-agnostic).</summary>
    public string TokenUrl { get; init; } = "https://oauth.battle.net/token";

    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";

    /// <summary>Blizzard data namespace, e.g. profile-us / static-us / dynamic-us.</summary>
    public string Namespace { get; init; } = "profile-us";

    public string Locale { get; init; } = "en_US";

    /// <summary>Refresh the token this long before it actually expires.</summary>
    public TimeSpan TokenRefreshSkew { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How long a composed character view stays cached. Character data only changes
    /// when the player logs out, so minutes are fine and it collapses the refresh
    /// spam that a reference site attracts.
    /// </summary>
    public TimeSpan CharacterCacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}
