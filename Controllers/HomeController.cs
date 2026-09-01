using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RecipeSite.Models;
using RecipeSite.Services;

namespace RecipeSite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly FactsService _factsService;

    public HomeController(ILogger<HomeController> logger, FactsService factsService)
    {
        _logger = logger;
        _factsService = factsService;
    }

    public async Task<IActionResult> Index()
    {
        var facts = await _factsService.GetRandomFactsAsync(4);
        ViewBag.Facts = facts;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}