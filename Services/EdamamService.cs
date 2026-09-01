using System.Text.Json.Serialization;

namespace RecipeSite.Services
{
    public class EdamamHit
    {
        [JsonPropertyName("recipe")]
        public EdamamRecipe Recipe { get; set; } = new();
    }

    public class EdamamRecipe
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("ingredientLines")]
        public List<string>? IngredientLines { get; set; }

        [JsonPropertyName("totalTime")]
        public double? TotalTime { get; set; }

        [JsonPropertyName("mealType")]
        public List<string>? MealType { get; set; }
    }

    public class EdamamSearchResponse
    {
        [JsonPropertyName("hits")]
        public List<EdamamHit>? Hits { get; set; }
    }

    public class EdamamService
    {
        private readonly HttpClient _httpClient;
        private readonly string _appId;
        private readonly string _appKey;
        private const string BaseUrl = "https://api.edamam.com/api/recipes/v2";

        public EdamamService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _appId = configuration["RecipeApis:EdamamAppId"] ?? "";
            _appKey = configuration["RecipeApis:EdamamAppKey"] ?? "";
        }

        private static string ExtractId(string uri)
        {
            var idx = uri.LastIndexOf("_");
            return idx >= 0 ? uri[(idx + 1)..] : uri;
        }

        public async Task<List<CatalogRecipe>> SearchAsync(string query, int number = 12)
        {
            try
            {
                var url = $"{BaseUrl}?type=public&q={Uri.EscapeDataString(query)}&app_id={_appId}&app_key={_appKey}";
                var response = await _httpClient.GetFromJsonAsync<EdamamSearchResponse>(url);

                return response?.Hits?.Take(number).Select(h => new CatalogRecipe
                {
                    Id = $"edamam_{ExtractId(h.Recipe.Uri)}",
                    Source = "edamam",
                    Title = h.Recipe.Label,
                    ImageUrl = h.Recipe.Image,
                    Category = h.Recipe.MealType?.FirstOrDefault() ?? "Edamam"
                }).ToList() ?? new List<CatalogRecipe>();
            }
            catch
            {
                return new List<CatalogRecipe>();
            }
        }

        public async Task<List<CatalogRecipe>> GetHomeStyleRecipesAsync(int number = 12)
        {
            return await SearchAsync("homemade dinner", number);
        }

        public async Task<EdamamRecipe?> GetByIdAsync(string id)
        {
            try
            {
                var url = $"{BaseUrl}/{id}?type=public&app_id={_appId}&app_key={_appKey}";
                var response = await _httpClient.GetFromJsonAsync<EdamamHit>(url);
                return response?.Recipe;
            }
            catch
            {
                return null;
            }
        }
    }
}