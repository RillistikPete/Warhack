using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarHack.Application;
using WarHack.Domain;
using WarHack.Logging;
using WarHack.Models;
using WarHack.Security;

namespace WarHack.Controllers;

/// <summary>
/// Requires api key
/// </summary>
[ApiController]
[Route("api/characters")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.Scheme, Roles = "Characters")]
[Produces("application/json")]
public class CharactersApiController : ControllerBase
{
    private readonly IAthenaeum _athenaeum;
    private readonly ILogShipHarbor _logBoat;
    private readonly ILogger<CharactersApiController> _logger;

    public CharactersApiController(IAthenaeum athenaeum, ILogShipHarbor logBoat, ILogger<CharactersApiController> logger)
    {
        _athenaeum = athenaeum;
        _logBoat = logBoat;
        _logger = logger;
    }

    /// <summary>Profile, stats and gear for one character</summary>
    /// <param name="realm">Realm slug</param>
    /// <param name="name">Character name</param>
    [HttpGet("{realm}/{name}")]
    [ProducesResponseType(typeof(CharacterView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string realm, string name, CancellationToken ct)
    {
        if (!RealmSlug.TryFrom(realm, out var realmSlug))
        {
            return BadRequest(new { message = $"'{realm}' is not a usable realm name." });
        }

        if (!CharacterName.TryFrom(name, out var characterName))
        {
            return BadRequest(new { message = $"'{name}' is not a usable character name." });
        }

        var result = await _athenaeum.GetCharacterAsync(realmSlug, characterName, ct);

        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(result.Value);
        }

        if (result.Status == StatusCodes.Status404NotFound)
        {
            return NotFound(new { message = result.Error });
        }

        _logger.LogWarning("API character lookup failed ({Status}) for {Realm}/{Name}: {Error}",
            result.Status, realmSlug.Value, characterName.Value, result.Error);

        _logBoat.Ship($"API character lookup failed ({result.Status}) for {realmSlug.Value}/{characterName.Value}: {result.Error}",
                      "Warning", nameof(CharactersApiController));

        return Problem(detail: result.Error, statusCode: result.Status == 0 ? StatusCodes.Status502BadGateway : result.Status);
    }
}
