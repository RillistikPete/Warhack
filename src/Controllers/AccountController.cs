using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WarHack.Identity;
using WarHack.Infrastructure.Models;

namespace WarHack.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly SignInManager<WarHackUser> _signInManager;
    private readonly UserManager<WarHackUser> _userManager;

    public AccountController(
        ILogger<AccountController> logger,
        SignInManager<WarHackUser> signInManager,
        UserManager<WarHackUser> userManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
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
            _logger.LogInformation("User {User} signed in.", model.UserName);
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
            return View(model);
        }

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

        _logger.LogInformation("New account created: {User}", user.UserName);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("logout"), Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}