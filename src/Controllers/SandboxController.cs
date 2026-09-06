using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WarHack.Logging;
using WarHack.Models;

namespace WarHack.Controllers;

public class SandboxController : Controller
{
    private readonly ILogger<ILogShipHarbor> _logger;
    //private SandboxControllerSettings _settings;

    public SandboxController(ILogger<ILogShipHarbor> logger)
    {
        _logger = logger;
    }

    [Route("Sandbox")]
    [Route("Sandbox/Index")]
    [ApiExplorerSettings(IgnoreApi=true)]
    public IActionResult Index()
    {
        _logger.LogInformation("This is sandbox index");
        return View();
    }

    [HttpGet]
    //[RestrictAccess]
    public IActionResult Privacy()
    {
        return View();
    }
    
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}




