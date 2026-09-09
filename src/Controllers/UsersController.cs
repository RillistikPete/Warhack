using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarHack.Identity;
using WarHack.Domain;
using WarHack.Logging;

namespace WarHack.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<WarHackUser> _users;
    private readonly ILogger<UsersController> _logger;
    private readonly ILogShipHarbor _ship;

    public UsersController(
        UserManager<WarHackUser> users,
        ILogger<UsersController> logger,
        ILogShipHarbor ship)
    {
        _users = users;
        _logger = logger;
        _ship = ship;
    }

    // GET /api/users/me — reads purely from the ClaimsPrincipal, no DB hit
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        UserName = User.Identity?.Name,
        Email = User.FindFirstValue(ClaimTypes.Email),
        BattleTag = User.FindFirstValue("battletag"),
        MainCharacter = User.FindFirstValue("character:name"),
        MainRealm = User.FindFirstValue("character:realm"),
        Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
    });

    [HttpGet("me/roles")]
    public IActionResult MyRoles() =>
        Ok(User.FindAll(ClaimTypes.Role).Select(c => c.Value));

    [HttpGet]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _users.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new UserSummary(
                u.Id, u.UserName, u.Email, u.BattleTag,
                u.CharacterName, u.CharacterRealm))
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.BattleTag,
            user.CharacterName,
            user.CharacterRealm,
            Roles = roles
        });
    }

    [HttpPut("update/{id}")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> PutUser(string id, WarHackUser user)
    {
        var usr = await _users.FindByIdAsync(id);
        if (usr is null) return NotFound();

        //_context.Entry(user).State = EntityState.Modified;

        //try
        //{
        //    await _context.SaveChangesAsync();
        //}
        //catch (DbUpdateConcurrencyException)
        //{
        //    if (!UserExists(id))
        //    {
        //        return NotFound();
        //    }
        //    else
        //    {
        //        throw;
        //    }
        //}

        return NoContent();
    }

    /// <summary>
    /// makes role changes take effect
    /// </summary>
    /// <param name="id"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    [HttpPost("{id}/roles/{role}")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> GrantRole(string id, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await _users.AddToRoleAsync(user, role);
        if (!result.Succeeded) return Problem(
            string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Role {Role} granted to {UserId} by {Actor}",
            role, id, User.Identity!.Name);

        // Privilege change: who did what to whom, kept where it can be queried later.
        _ship.Ship($"Role {role} granted to {user.UserName} ({id}) by {User.Identity!.Name}.",
                   "Warning", nameof(UsersController));

        return NoContent();
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Policy = "IsAdmin")]
    public async Task<IActionResult> RevokeRole(string id, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await _users.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded) return Problem(
            string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Role {Role} revoked from {UserId} by {Actor}",
            role, id, User.Identity!.Name);

        _ship.Ship($"Role {role} revoked from {user.UserName} ({id}) by {User.Identity!.Name}.",
                   "Warning", nameof(UsersController));

        return NoContent();
    }
}

public record UserSummary(
    string Id, string? UserName, string? Email,
    string? BattleTag, string? MainCharacter, string? MainRealm);