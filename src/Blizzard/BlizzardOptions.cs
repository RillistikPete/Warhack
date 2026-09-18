namespace WarHack.Blizzard;

public sealed class BlizzardOptions
{
    public const string SectionName = "Blizzard";
    public string BaseUrl { get; init; } = "https://us.api.blizzard.com/";

    /// <summary>
    /// OAuth2 token endpoint
    /// </summary>
    public string TokenUrl { get; init; } = "https://oauth.battle.net/token";
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string Namespace { get; init; } = "profile-us";
    public string Locale { get; init; } = "en_US";
    public TimeSpan TokenRefreshTime { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan TokenRefreshInterval { get; init; } = TimeSpan.FromHours(12);
    public TimeSpan CharacterCacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}
