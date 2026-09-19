using System.Text.Json;
using System.Text.Json.Serialization;
using RecipeSite.Models;

namespace RecipeSite.Services
{
    // Читает локальные рецепты "Книги лёгких рецептов" из wwwroot/data/simple-recipes.json
    public class SimpleRecipeService
    {
        private readonly List<SimpleRecipe> _recipes;

        public SimpleRecipeService(IWebHostEnvironment env)
        {
            var path = Path.Combine(env.WebRootPath ?? "wwwroot", "data", "simple-recipes.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _recipes = JsonSerializer.Deserialize<List<SimpleRecipe>>(json, options) ?? new List<SimpleRecipe>();
            }
            else
            {
                _recipes = new List<SimpleRecipe>();
            }
        }

        public List<SimpleRecipe> GetAll() => _recipes;

        public SimpleRecipe? GetById(string id) => _recipes.FirstOrDefault(r => r.Id == id);

        public List<CatalogRecipe> ToCatalogRecipes()
        {
            return _recipes.Select(r => new CatalogRecipe
            {
                Id = r.Id,
                Source = "local",
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                Category = "Простые домашние"
            }).ToList();
        }
    }
}
