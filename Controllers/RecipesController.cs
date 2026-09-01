using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;

namespace RecipeSite.Controllers
{
    public class RecipesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly HttpClient _httpClient = new HttpClient();

        public RecipesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public static readonly string[] Categories = new[]
        {
            "Супы", "Вторые", "Сладкое", "Закуски", "Салаты"
        };

        [Authorize]
        public async Task<IActionResult> MyRecipes()
        {
            var userId = _userManager.GetUserId(User);
            var myRecipes = await _context.Recipes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Categories = Categories;
            return View(myRecipes);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddRecipe(string title, string category, string sourceUrl)
        {
            var userId = _userManager.GetUserId(User);
            var recipe = new Recipe
            {
                Title = title,
                Category = category,
                SourceUrl = sourceUrl,
                UserId = userId,
                ImageUrl = null // картинку не сохраняем — иконка подставляется по категории прямо на странице
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyRecipes");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userId = _userManager.GetUserId(User);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MyRecipes");
        }

        // Переводит текст с русского на английский через бесплатный сервис LibreTranslate
        private async Task<string> TranslateToEnglish(string text)
        {
            try
            {
                var requestBody = new
                {
                    q = text,
                    source = "ru",
                    target = "en",
                    format = "text"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.PostAsync("https://libretranslate.de/translate", content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                var translated = doc.RootElement.GetProperty("translatedText").GetString();

                return translated ?? string.Empty;
            }
            catch
            {
                // Если переводчик недоступен или произошла ошибка — просто возвращаем пустую строку
                return string.Empty;
            }
        }
    }
}