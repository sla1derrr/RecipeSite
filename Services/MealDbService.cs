using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecipeSite.Services
{
    public class MealDbMeal
    {
        [JsonPropertyName("idMeal")]
        public string Id { get; set; } = "";

        [JsonPropertyName("strMeal")]
        public string Title { get; set; } = "";

        [JsonPropertyName("strCategory")]
        public string? Category { get; set; }

        [JsonPropertyName("strArea")]
        public string? Area { get; set; }

        [JsonPropertyName("strInstructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("strMealThumb")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("strTags")]
        public string? Tags { get; set; }

        [JsonPropertyName("strYoutube")]
        public string? YoutubeUrl { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

        public List<(string Ingredient, string Measure)> GetIngredients()
        {
            var list = new List<(string, string)>();
            if (Extra == null) return list;

            for (int i = 1; i <= 20; i++)
            {
                if (Extra.TryGetValue($"strIngredient{i}", out var ingEl) &&
                    Extra.TryGetValue($"strMeasure{i}", out var measEl))
                {
                    var ing = ingEl.GetString();
                    var meas = measEl.GetString();
                    if (!string.IsNullOrWhiteSpace(ing))
                    {
                        list.Add((ing!.Trim(), meas?.Trim() ?? ""));
                    }
                }
            }
            return list;
        }
    }

    public class MealDbResponse
    {
        [JsonPropertyName("meals")]
        public List<MealDbMeal>? Meals { get; set; }
    }

    public class MealDbCategory
    {
        [JsonPropertyName("strCategory")]
        public string Name { get; set; } = "";
    }

    public class MealDbCategoryResponse
    {
        [JsonPropertyName("meals")]
        public List<MealDbCategory>? Categories { get; set; }
    }

    public class MealDbService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://www.themealdb.com/api/json/v1/1";

        public MealDbService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<MealDbCategoryResponse>($"{BaseUrl}/list.php?c=list");
            return response?.Categories?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public async Task<List<MealDbMeal>> GetMealsByCategoryAsync(string category)
        {
            var response = await _httpClient.GetFromJsonAsync<MealDbResponse>($"{BaseUrl}/filter.php?c={Uri.EscapeDataString(category)}");
            return response?.Meals ?? new List<MealDbMeal>();
        }

        public async Task<List<MealDbMeal>> SearchMealsAsync(string query)
        {
            var response = await _httpClient.GetFromJsonAsync<MealDbResponse>($"{BaseUrl}/search.php?s={Uri.EscapeDataString(query)}");
            return response?.Meals ?? new List<MealDbMeal>();
        }

        public async Task<MealDbMeal?> GetMealByIdAsync(string id)
        {
            var response = await _httpClient.GetFromJsonAsync<MealDbResponse>($"{BaseUrl}/lookup.php?i={Uri.EscapeDataString(id)}");
            return response?.Meals?.FirstOrDefault();
        }

        public async Task<MealDbMeal?> GetRandomMealAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<MealDbResponse>($"{BaseUrl}/random.php");
            return response?.Meals?.FirstOrDefault();
        }
    }
}