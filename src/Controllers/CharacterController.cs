using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarHack.Application;
using WarHack.Domain;
using WarHack.Logging;
using WarHack.Security;

namespace WarHack.Controllers;

[Route("character")]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.Scheme, Roles = "Characters")]
public class CharacterController : Controller
{
    private readonly IAthenaeum _athenaeum;
    private readonly ILogShipHarbor _ship;
    private readonly ILogger<CharacterController> _logger;

    public CharacterController(IAthenaeum athenaeum, ILogShipHarbor ship, ILogger<CharacterController> logger)
    {
        _athenaeum = athenaeum;
        _ship = ship;
        _logger = logger;
    }

    [HttpGet(""), AllowAnonymous]
    public async Task<IActionResult> Index(string? realm, string? name, CancellationToken ct)
    {
        ViewData["Realm"] = realm;
        ViewData["Name"] = name;

        if (string.IsNullOrWhiteSpace(realm) && string.IsNullOrWhiteSpace(name))
        {
            return View(model: null);
        }

        // might remove realm here, search by name only
        if (!RealmSlug.TryFrom(realm, out var realmSlug))
        {
            ModelState.AddModelError(string.Empty, "That doesn't look like a realm name.");
            return View(model: null);
        }

        if (!CharacterName.TryFrom(name, out var characterName))
        {
            ModelState.AddModelError(string.Empty, "That doesn't look like a character name.");
            return View(model: null);
        }

        var result = await _athenaeum.GetCharacterAsync(realmSlug, characterName, ct);

        if (!result.IsSuccess)
        {
            // 404 is a normal misspelling
            if (result.Status != 404)
            {
                _logger.LogWarning("Character lookup failed ({Status}) for {Realm}/{Name}: {Error}",
                    result.Status, realm, name, result.Error);
                _ship.Ship($"Character lookup failed ({result.Status}) for {realm}/{name}: {result.Error}", "Warning", nameof(CharacterController));
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "Lookup failed.");
            return View(model: null);
        }

        return View(result.Value);
    }

    [Authorize]
    [HttpGet("toon")]
    public IActionResult Toon()
    {
        _logger.LogInformation("Privacy page reached.");
        return View();
    }
}
