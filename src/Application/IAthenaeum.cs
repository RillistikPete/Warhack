using WarHack.Models;
using WarHack.Domain;

namespace WarHack.Application;

/// <summary>
/// The library: one place the app asks for domain data, whatever mix of clients
/// it takes to assemble it. Controllers depend on this, never on IBlizzardClient.
/// </summary>
public interface IAthenaeum
{
    /// <summary>Profile, stats and gear for one character, composed into a single view.</summary>
    Task<ApiResult<CharacterView>> GetCharacterAsync(RealmSlug realm, CharacterName name, CancellationToken ct = default);
}
