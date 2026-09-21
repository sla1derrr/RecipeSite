namespace RecipeSite.Models
{
    public class ShoppingList
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string DishName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ShoppingItem> Items { get; set; } = new();
    }

    public class ShoppingItem
    {
        public int Id { get; set; }
        public int ShoppingListId { get; set; }
        public ShoppingList? ShoppingList { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Amount { get; set; }
        public bool IsChecked { get; set; }
    }
}