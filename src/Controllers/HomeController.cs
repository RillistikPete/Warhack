using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WarHack.Models;

namespace WarHack.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<ILogShipHarbor> _logger;
    private HomeControllerSettings _settings;

    public HomeController(ILogger<ILogShipHarbor> logger, IOptions<HomeControllerSettings> sOptions)
    {
        _logger = logger;
        _settings = sOptions.Value;
    }

    [Route("/")]
    [Route("Home")]
    [Route("Home/Index")]
    [ApiExplorerSettings(IgnoreApi=true)]
    public IActionResult Index()
    {
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




