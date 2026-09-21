using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSite.Data;
using RecipeSite.Models;
using RecipeSite.Services;

namespace RecipeSite.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class ShoppingListController : Controller
    {
        private const int MaxLists = 30;
        private const int MaxItemsPerList = 100;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MealDbService _mealDb;

        public ShoppingListController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            MealDbService mealDb)
        {
            _context = context;
            _userManager = userManager;
            _mealDb = mealDb;
        }

        public async Task<IActionResult> Index(string? query)
        {
            var userId = _userManager.GetUserId(User);
            var lists = await _context.ShoppingLists
                .Include(l => l.Items.OrderBy(i => i.Id))
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            query = query?.Trim();
            ViewBag.Query = query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Results = await SearchMealsAsync(query);
            }

            return View(lists);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromMeal(string? mealId)
        {
            var userId = _userManager.GetUserId(User)!;
            if (string.IsNullOrWhiteSpace(mealId)) return RedirectToAction(nameof(Index));
            if (await ListLimitReached(userId)) return RedirectToAction(nameof(Index));

            MealDbMeal? meal = null;
            try { meal = await _mealDb.GetMealByIdAsync(mealId); }
            catch (Exception) { /* внешний сервис недоступен */ }

            if (meal == null)
            {
                TempData["Msg"] = "Не удалось получить блюдо из каталога. Попробуйте ещё раз.";
                return RedirectToAction(nameof(Index));
            }

            var list = new ShoppingList { UserId = userId, DishName = Clip(meal.Title, 100) };
            foreach (var (ingredient, measure) in meal.GetIngredients().Take(MaxItemsPerList))
            {
                list.Items.Add(new ShoppingItem
                {
                    Name = Clip(IngredientTranslator.ToRussian(ingredient), 100),
                    Amount = string.IsNullOrWhiteSpace(measure) ? null : Clip(measure, 50)
                });
            }

            _context.ShoppingLists.Add(list);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmpty(string? dishName)
        {
            var userId = _userManager.GetUserId(User)!;
            dishName = dishName?.Trim();
            if (string.IsNullOrWhiteSpace(dishName)) return RedirectToAction(nameof(Index));
            if (await ListLimitReached(userId)) return RedirectToAction(nameof(Index));

            _context.ShoppingLists.Add(new ShoppingList { UserId = userId, DishName = Clip(dishName, 100) });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(int listId, string? name, string? amount)
        {
            var userId = _userManager.GetUserId(User);
            var list = await _context.ShoppingLists
                .Include(l => l.Items)
                .FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);
            if (list == null) return NotFound();

            name = name?.Trim();
            if (!string.IsNullOrWhiteSpace(name) && list.Items.Count < MaxItemsPerList)
            {
                list.Items.Add(new ShoppingItem
                {
                    Name = Clip(name, 100),
                    Amount = string.IsNullOrWhiteSpace(amount) ? null : Clip(amount.Trim(), 50)
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int id, bool isChecked)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList)
                .FirstOrDefaultAsync(i => i.Id == id && i.ShoppingList!.UserId == userId);
            if (item == null) return NotFound();

            item.IsChecked = isChecked;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList)
                .FirstOrDefaultAsync(i => i.Id == id && i.ShoppingList!.UserId == userId);
            if (item == null) return NotFound();

            _context.ShoppingItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteList(int id)
        {
            var userId = _userManager.GetUserId(User);
            var list = await _context.ShoppingLists.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
            if (list != null)
            {
                _context.ShoppingLists.Remove(list); // продукты удалятся каскадом
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- вспомогательные методы ----------

        private async Task<List<MealDbMeal>> SearchMealsAsync(string query)
        {
            var terms = new List<string> { query };
            var english = IngredientTranslator.ToEnglish(query);
            if (!english.Equals(query, StringComparison.OrdinalIgnoreCase)) terms.Add(english);

            var found = new List<MealDbMeal>();
            foreach (var term in terms)
            {
                try { found.AddRange(await _mealDb.SearchMealsAsync(term)); }
                catch (Exception) { /* внешний сервис недоступен */ }
            }

            return found.GroupBy(m => m.Id).Select(g => g.First()).Take(12).ToList();
        }

        private async Task<bool> ListLimitReached(string userId)
        {
            if (await _context.ShoppingLists.CountAsync(l => l.UserId == userId) < MaxLists) return false;
            TempData["Msg"] = $"Можно хранить не больше {MaxLists} списков. Удалите ненужные.";
            return true;
        }

        private static string Clip(string s, int max) => s.Length <= max ? s : s[..max];
    }
}