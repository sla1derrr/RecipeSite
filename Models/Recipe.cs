using System.ComponentModel.DataAnnotations;

namespace RecipeSite.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название блюда")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        // Категория: Супы, Вторые, Сладкое, Закуски, Салаты
        [Required]
        public string Category { get; set; } = string.Empty;

        // Сложность: Просто, Быстро, Долго, Сложно
        public string? Difficulty { get; set; }

        // Ссылка на TikTok/Instagram (для личных рецептов)
        public string? SourceUrl { get; set; }

        // Путь к картинке
        public string? ImageUrl { get; set; }

        // Кто добавил рецепт (для раздела "Мои рецепты")
        public string? UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}