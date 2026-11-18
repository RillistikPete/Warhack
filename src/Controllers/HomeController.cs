using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WarHack.Models;

namespace WarHack.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
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
    public IActionResult Index()
    {
        _logger.LogInformation("Privacy page reached.");
        return View();
    }

    [HttpGet]
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




