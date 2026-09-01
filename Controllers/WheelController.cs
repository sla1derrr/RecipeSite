using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;
using RecipeSite.Services;

namespace RecipeSite.Controllers
{
    public class WheelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MealDbService _mealDbService;
        private static readonly Random _random = new Random();

        public WheelController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            MealDbService mealDbService)
        {
            _context = context;
            _userManager = userManager;
            _mealDbService = mealDbService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _mealDbService.GetCategoriesAsync();
            ViewBag.Categories = categories;

            var isLoggedIn = User.Identity != null && User.Identity.IsAuthenticated;
            ViewBag.IsLoggedIn = isLoggedIn;

            if (isLoggedIn)
            {
                var userId = _userManager.GetUserId(User);
                var hasOwnRecipes = await _context.Recipes.AnyAsync(r => r.UserId == userId);
                ViewBag.HasOwnRecipes = hasOwnRecipes;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Spin(List<string> sources)
        {
            var pool = new List<WheelItem>();

            if (sources == null || sources.Count == 0)
            {
                TempData["WheelError"] = "Выбери хотя бы один источник блюд";
                return RedirectToAction("Index");
            }

            if (sources.Contains("own") && User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var myRecipes = await _context.Recipes.Where(r => r.UserId == userId).ToListAsync();

                foreach (var r in myRecipes)
                {
                    pool.Add(new WheelItem
                    {
                        Title = r.Title,
                        ImageUrl = r.ImageUrl,
                        LinkUrl = r.SourceUrl,
                        IsInternal = false
                    });
                }
            }

            var categorySources = sources.Where(s => s != "own").ToList();
            foreach (var category in categorySources)
            {
                var meals = await _mealDbService.GetMealsByCategoryAsync(category);
                var randomPicks = meals.OrderBy(_ => _random.Next()).Take(10);

                foreach (var m in randomPicks)
                {
                    pool.Add(new WheelItem
                    {
                        Title = m.Title,
                        ImageUrl = m.ImageUrl,
                        LinkUrl = Url.Action("Details", "RecipeBook", new { id = $"meal_{m.Id}" }),
                        IsInternal = true
                    });
                }
            }

            if (pool.Count == 0)
            {
                TempData["WheelError"] = "Не удалось найти блюда по выбранным источникам, попробуй другие";
                return RedirectToAction("Index");
            }

            var finalPool = pool.OrderBy(_ => _random.Next()).Take(50).ToList();
            var winnerIndex = _random.Next(finalPool.Count);

            ViewBag.Items = finalPool;
            ViewBag.WinnerIndex = winnerIndex;

            return View("Result");
        }
    }
}