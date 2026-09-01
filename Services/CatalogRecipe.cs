namespace RecipeSite.Services
{
    // Единый формат рецепта для всех источников (TheMealDB, Spoonacular, Edamam)
    public class CatalogRecipe
    {
        public string Id { get; set; } = "";       // с префиксом источника, напр. "meal_52977"
        public string Source { get; set; } = "";     // "meal" | "spoon" | "edamam"
        public string Title { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
    }
}