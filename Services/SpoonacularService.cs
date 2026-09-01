using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecipeSite.Services
{
    public class SpoonacularRecipe
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("readyInMinutes")]
        public int? ReadyInMinutes { get; set; }

        [JsonPropertyName("servings")]
        public int? Servings { get; set; }

        [JsonPropertyName("sourceUrl")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("extendedIngredients")]
        public List<SpoonacularIngredient>? ExtendedIngredients { get; set; }

        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }
    }

    public class SpoonacularIngredient
    {
        [JsonPropertyName("original")]
        public string Original { get; set; } = "";
    }

    public class SpoonacularSearchResponse
    {
        [JsonPropertyName("results")]
        public List<SpoonacularRecipe>? Results { get; set; }
    }

    public class SpoonacularService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.spoonacular.com/recipes";

        public SpoonacularService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["RecipeApis:SpoonacularApiKey"] ?? "";
        }

        public async Task<List<CatalogRecipe>> GetEasyRecipesAsync(int maxReadyMinutes = 30, int number = 12)
        {
            try
            {
                var url = $"{BaseUrl}/complexSearch?apiKey={_apiKey}&maxReadyTime={maxReadyMinutes}&number={number}&sort=time";
                var response = await _httpClient.GetFromJsonAsync<SpoonacularSearchResponse>(url);

                return response?.Results?.Select(r => new CatalogRecipe
                {
                    Id = $"spoon_{r.Id}",
                    Source = "spoon",
                    Title = r.Title,
                    ImageUrl = r.Image,
                    Category = "Простое и быстрое"
                }).ToList() ?? new List<CatalogRecipe>();
            }
            catch
            {
                return new List<CatalogRecipe>();
            }
        }

        public async Task<List<CatalogRecipe>> SearchAsync(string query, int number = 12)
        {
            try
            {
                var url = $"{BaseUrl}/complexSearch?apiKey={_apiKey}&query={Uri.EscapeDataString(query)}&number={number}";
                var response = await _httpClient.GetFromJsonAsync<SpoonacularSearchResponse>(url);

                return response?.Results?.Select(r => new CatalogRecipe
                {
                    Id = $"spoon_{r.Id}",
                    Source = "spoon",
                    Title = r.Title,
                    ImageUrl = r.Image,
                    Category = "Spoonacular"
                }).ToList() ?? new List<CatalogRecipe>();
            }
            catch
            {
                return new List<CatalogRecipe>();
            }
        }

        public async Task<SpoonacularRecipe?> GetByIdAsync(string id)
        {
            try
            {
                var url = $"{BaseUrl}/{id}/information?apiKey={_apiKey}";
                return await _httpClient.GetFromJsonAsync<SpoonacularRecipe>(url);
            }
            catch
            {
                return null;
            }
        }
    }
}