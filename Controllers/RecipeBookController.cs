using Microsoft.AspNetCore.Mvc;
using RecipeSite.Services;

namespace RecipeSite.Controllers
{
    public class RecipeBookController : Controller
    {
        private readonly MealDbService _mealDbService;
        private readonly SpoonacularService _spoonacularService;
        private readonly EdamamService _edamamService;

        public RecipeBookController(
            MealDbService mealDbService,
            SpoonacularService spoonacularService,
            EdamamService edamamService)
        {
            _mealDbService = mealDbService;
            _spoonacularService = spoonacularService;
            _edamamService = edamamService;
        }

        public async Task<IActionResult> Index(string? category, string? search)
        {
            var categories = await _mealDbService.GetCategoriesAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SearchQuery = search;

            var allRecipes = new List<CatalogRecipe>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var mealTask = _mealDbService.SearchMealsAsync(search);
                var spoonTask = _spoonacularService.SearchAsync(search);
                var edamamTask = _edamamService.SearchAsync(search);

                await Task.WhenAll(mealTask, spoonTask, edamamTask);

                allRecipes.AddRange(mealTask.Result.Select(m => new CatalogRecipe
                {
                    Id = $"meal_{m.Id}",
                    Source = "meal",
                    Title = m.Title,
                    ImageUrl = m.ImageUrl,
                    Category = m.Category
                }));
                allRecipes.AddRange(spoonTask.Result);
                allRecipes.AddRange(edamamTask.Result);
            }
            else if (!string.IsNullOrWhiteSpace(category) && category != "Простые домашние")
            {
                var meals = await _mealDbService.GetMealsByCategoryAsync(category);
                allRecipes.AddRange(meals.Select(m => new CatalogRecipe
                {
                    Id = $"meal_{m.Id}",
                    Source = "meal",
                    Title = m.Title,
                    ImageUrl = m.ImageUrl,
                    Category = m.Category
                }));
                ViewBag.SelectedCategory = category;
            }
            else if (category == "Простые домашние")
            {
                var spoonTask = _spoonacularService.GetEasyRecipesAsync();
                var edamamTask = _edamamService.GetHomeStyleRecipesAsync();
                await Task.WhenAll(spoonTask, edamamTask);

                allRecipes.AddRange(spoonTask.Result);
                allRecipes.AddRange(edamamTask.Result);
                ViewBag.SelectedCategory = "Простые домашние";
            }
            else
            {
                var defaultCategory = categories.FirstOrDefault() ?? "Beef";
                var meals = await _mealDbService.GetMealsByCategoryAsync(defaultCategory);
                allRecipes.AddRange(meals.Select(m => new CatalogRecipe
                {
                    Id = $"meal_{m.Id}",
                    Source = "meal",
                    Title = m.Title,
                    ImageUrl = m.ImageUrl,
                    Category = m.Category
                }));
                ViewBag.SelectedCategory = defaultCategory;
            }

            return View(allRecipes);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (id.StartsWith("meal_"))
            {
                var realId = id["meal_".Length..];
                var meal = await _mealDbService.GetMealByIdAsync(realId);
                if (meal == null) return NotFound();

                ViewBag.Source = "meal";
                return View("Details_Meal", meal);
            }

            if (id.StartsWith("spoon_"))
            {
                var realId = id["spoon_".Length..];
                var recipe = await _spoonacularService.GetByIdAsync(realId);
                if (recipe == null) return NotFound();

                ViewBag.Source = "spoon";
                return View("Details_Spoonacular", recipe);
            }

            if (id.StartsWith("edamam_"))
            {
                var realId = id["edamam_".Length..];
                var recipe = await _edamamService.GetByIdAsync(realId);
                if (recipe == null) return NotFound();

                ViewBag.Source = "edamam";
                return View("Details_Edamam", recipe);
            }

            return NotFound();
        }
    }
}