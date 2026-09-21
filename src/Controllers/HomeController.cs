using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using WarHack.Logging;
using WarHack.Models;
using WarHack.Options;

namespace WarHack.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [Route("/")]
    [Route("Home")]
    [Route("Home/Index")]
    [ApiExplorerSettings(IgnoreApi=true)]
    public IActionResult Index()
    {
        _logger.LogInformation("Privacy page reached.");
        return View();
    }

    [HttpGet]
    //[RestrictAccess]
    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Home/Error/{code:int?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        var reExecute = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code ?? HttpContext.Response.StatusCode,
            OriginalPath = reExecute?.OriginalPath
        });
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}




