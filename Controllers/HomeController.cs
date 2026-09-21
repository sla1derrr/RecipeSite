using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RecipeSite.Data;
using RecipeSite.Models;
using RecipeSite.Services;

namespace RecipeSite.Controllers;

public record HomeCategory(string Key, string Title, string Emoji);

public class HomeController : Controller
{
    // Названия категорий TheMealDB → по-русски (страницу дальше переводит виджет) + эмодзи
    private static readonly Dictionary<string, (string Title, string Emoji)> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Beef", ("Говядина", "🥩") },
        { "Breakfast", ("Завтраки", "🥞") },
        { "Chicken", ("Курица", "🍗") },
        { "Dessert", ("Десерты", "🍰") },
        { "Goat", ("Козлятина", "🐐") },
        { "Lamb", ("Баранина", "🍖") },
        { "Miscellaneous", ("Разное", "🍽️") },
        { "Pasta", ("Паста", "🍝") },
        { "Pork", ("Свинина", "🥓") },
        { "Seafood", ("Морепродукты", "🦐") },
        { "Side", ("Гарниры", "🍟") },
        { "Starter", ("Закуски", "🥟") },
        { "Vegan", ("Веганское", "🥬") },
        { "Vegetarian", ("Вегетарианское", "🥗") },
    };

    private readonly ILogger<HomeController> _logger;
    private readonly MealDbService _mealDbService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public HomeController(
        ILogger<HomeController> logger,
        MealDbService mealDbService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache)
    {
        _logger = logger;
        _mealDbService = mealDbService;
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        // ----- быстрые категории -----
        var categories = new List<string>();
        try { categories = await _mealDbService.GetCategoriesAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Не удалось получить категории"); }

        ViewBag.QuickCategories = categories
            .Take(8)
            .Select(c => CategoryMap.TryGetValue(c, out var m)
                ? new HomeCategory(c, m.Title, m.Emoji)
                : new HomeCategory(c, c, "🍽️"))
            .ToList();

        // ----- последний рецепт друзей -----
        await LoadFriendRecipeAsync();

        // ----- рецепт дня -----
        ViewBag.DailyMeal = await GetDailyMealAsync(categories);

        return View();
    }

    private async Task LoadFriendRecipeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            ViewBag.FriendState = "guest";
            return;
        }

        var myId = _userManager.GetUserId(User)!;

        var friendIds = await _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == myId || f.AddresseeId == myId))
            .Select(f => f.RequesterId == myId ? f.AddresseeId : f.RequesterId)
            .ToListAsync();

        if (friendIds.Count == 0)
        {
            ViewBag.FriendState = "nofriends";
            return;
        }

        var recipe = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.UserId != null && friendIds.Contains(r.UserId))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (recipe == null)
        {
            ViewBag.FriendState = "norecipes";
            return;
        }

        ViewBag.FriendState = "ok";
        ViewBag.FriendRecipe = recipe;
        ViewBag.FriendUser = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == recipe.UserId);
    }

    /// <summary>Одно и то же блюдо в течение суток для всех пользователей.</summary>
    private async Task<MealDbMeal?> GetDailyMealAsync(List<string> categories)
    {
        var today = DateTime.UtcNow.Date;
        var key = "daily-meal-" + today.ToString("yyyyMMdd");
        if (_cache.TryGetValue(key, out MealDbMeal? cached)) return cached;

        try
        {
            if (categories.Count == 0) return null;

            var seed = today.Year * 1000 + today.DayOfYear;
            var category = categories[seed % categories.Count];
            var meals = await _mealDbService.GetMealsByCategoryAsync(category);
            if (meals.Count == 0) return null;

            var meal = meals[(seed / 7) % meals.Count];
            _cache.Set(key, meal, TimeSpan.FromHours(24));
            return meal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить рецепт дня");
            return null;
        }
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