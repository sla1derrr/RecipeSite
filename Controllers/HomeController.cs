using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RecipeSite.Models;
using RecipeSite.Services;

namespace RecipeSite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly MealDbService _mealDbService;

    public HomeController(ILogger<HomeController> logger, MealDbService mealDbService)
    {
        _logger = logger;
        _mealDbService = mealDbService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _mealDbService.GetCategoriesAsync();
        ViewBag.QuickCategories = categories.Take(6).ToList();
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