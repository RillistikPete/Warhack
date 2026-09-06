using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarHack.Application;
using WarHack.Logging;
using WarHack.Domain;

namespace WarHack.Controllers;

[Route("character")]
public class CharacterController : Controller
{
    private readonly IAthenaeum _athenaeum;
    private readonly ILogShipHarbor _ship;

    public CharacterController(IAthenaeum athenaeum, ILogShipHarbor ship)
    {
        _athenaeum = athenaeum;
        _ship = ship;
    }

    [HttpGet(""), AllowAnonymous]
    public async Task<IActionResult> Index(string? realm, string? name, CancellationToken ct)
    {
        ViewData["Realm"] = realm;
        ViewData["Name"] = name;

        // Empty form on first load -- no lookup, no wasted API call.
        if (string.IsNullOrWhiteSpace(realm) && string.IsNullOrWhiteSpace(name))
            return View(model: null);

        // Value objects validate at creation, so bad input never reaches destination.
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
            // 404 is a normal miss; anything else is worth having in the log table.
            if (result.Status != 404)
                _ship.Ship($"Character lookup failed ({result.Status}) for {realm}/{name}: {result.Error}",
                           "Warning", nameof(CharacterController));

            ModelState.AddModelError(string.Empty, result.Error ?? "Lookup failed.");
            return View(model: null);
        }

        return View(result.Value);
    }
}
