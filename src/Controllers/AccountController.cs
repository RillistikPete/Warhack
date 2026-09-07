using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WarHack.Identity;
using WarHack.Logging;
using WarHack.Models;

namespace WarHack.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly SignInManager<WarHackUser> _signInManager;
    private readonly UserManager<WarHackUser> _userManager;
    private readonly ILogShipHarbor _logBoat;

    public AccountController(
        SignInManager<WarHackUser> signInManager,
        UserManager<WarHackUser> userManager,
        ILogShipHarbor ship)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logBoat = ship;
    }

    [HttpGet("login"), AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("login"), AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logBoat.PrintToOut($"User {model.UserName} signed in.");
            _logBoat.Ship($"Sign-in succeeded for {model.UserName}.", logger: nameof(AccountController));
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

        if (result.IsLockedOut)
        {
            _logBoat.PrintToOut($"Account locked out: {model.UserName}.");
            _logBoat.Ship($"Account locked out: {model.UserName}.", "Warning", nameof(AccountController));
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
            return View(model);
        }

        _logBoat.PrintToOut($"Failed sign-in for {model.UserName}.");
        _logBoat.Ship($"Failed sign-in for {model.UserName}.", "Warning", nameof(AccountController));
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet("register"), AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost("register"), AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new WarHackUser
        {
            UserName  = model.UserName,
            Email     = model.Email,
            BattleTag = model.BattleTag
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, WarHackRoles.Member);
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logBoat.Ship("New account created: {User}", user.UserName, logger: nameof(AccountController));
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("logout"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}