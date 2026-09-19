namespace RecipeSite.Models
{
    // Локальный рецепт из "Книги лёгких рецептов" (категория "Простые домашние")
    public class SimpleRecipe
    {
        public string Id { get; set; } = "";               // напр. "local_1"
        public string Title { get; set; } = "";
        public string SubCategory { get; set; } = "";       // Завтраки / Обеды / Ужины / Закуски
        public string? TimeEstimate { get; set; }           // напр. "10 мин"
        public string Ingredients { get; set; } = "";        // одной строкой, через запятую
        public string Steps { get; set; } = "";              // текст приготовления
        public string? ImageUrl { get; set; }
    }
}
