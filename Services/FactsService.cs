using System.Text.Json.Serialization;

namespace RecipeSite.Services
{
    public class FactResponse
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    public class FactsService
    {
        private readonly HttpClient _httpClient;
        private const string Url = "https://uselessfacts.jsph.pl/api/v2/facts/random?language=en";

        public FactsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetRandomFactAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<FactResponse>(Url);
                return response?.Text;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<string>> GetRandomFactsAsync(int count = 3)
        {
            var facts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var fact = await GetRandomFactAsync();
                if (!string.IsNullOrWhiteSpace(fact))
                {
                    facts.Add(fact);
                }
            }
            return facts;
        }
    }
}