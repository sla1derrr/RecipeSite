namespace RecipeSite.Services
{
    // Один вариант блюда на колесе фортуны
    public class WheelItem
    {
        public string Title { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? LinkUrl { get; set; }       // куда вести при клике "Готовить" (внешняя ссылка или страница рецепта)
        public bool IsInternal { get; set; }        // true = ссылка внутри сайта (RecipeBook/Details), false = внешняя (TikTok/Instagram)
    }
}