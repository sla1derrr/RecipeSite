namespace RecipeSite.Models
{
    public class RecipeImage
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "image/jpeg";
    }
}