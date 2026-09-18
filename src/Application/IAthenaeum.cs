using WarHack.Models;
using WarHack.Domain;

namespace WarHack.Application;

/// <summary>
/// Library for character data
/// </summary>
public interface IAthenaeum
{
    /// <summary>Profile, stats and gear by character realm and name</summary>
    Task<ApiResult<CharacterView>> GetCharacterAsync(RealmSlug realm, CharacterName name, CancellationToken ct = default);
}
