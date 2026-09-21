using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RecipeSite.Services
{
    /// <summary>
    /// Приводит тексты списка покупок к русскому языку («базовому» языку сайта).
    /// Сайт переводится на выбранный язык виджетом Google Translate с pageLanguage = 'ru',
    /// поэтому виджет корректно переводит только русский текст. Английские названия
    /// продуктов/мер (Ginger, 1 tablespoon…) он оставлял как есть — их и нужно сначала
    /// перевести на русский, а дальше виджет переведёт страницу на en / pl / uk.
    /// </summary>
    public class TextTranslationService
    {
        private static readonly Regex NeedsTranslation = new(@"[A-Za-zĄąĆćĘęŁłŃńÓóŚśŹźŻż]", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

        private readonly HttpClient _http;

        public TextTranslationService(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(6);
        }

        /// <summary>true, если в строке есть латиница (значит, её надо привести к русскому).</summary>
        public static bool IsForeign(string? text) =>
            !string.IsNullOrWhiteSpace(text) && NeedsTranslation.IsMatch(text);

        /// <summary>Название продукта: сначала словарь, потом онлайн-перевод.</summary>
        public async Task<string> IngredientToRussianAsync(string name)
        {
            if (!IsForeign(name)) return name;
            var fromDict = IngredientTranslator.ToRussian(name);
            if (!fromDict.Equals(name.Trim(), StringComparison.Ordinal)) return fromDict;
            return await ToRussianAsync(name);
        }

        /// <summary>Произвольный текст (блюдо, количество) → русский. При ошибке вернёт исходный.</summary>
        public async Task<string> ToRussianAsync(string text)
        {
            if (!IsForeign(text)) return text;
            var key = text.Trim();
            if (Cache.TryGetValue(key, out var cached)) return cached;

            try
            {
                var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=ru&dt=t&q="
                          + Uri.EscapeDataString(key);
                using var stream = await _http.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var sb = new StringBuilder();
                foreach (var segment in doc.RootElement[0].EnumerateArray())
                {
                    if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                        sb.Append(segment[0].GetString());
                }

                var result = sb.ToString().Trim();
                if (result.Length == 0) return text;

                // Приводим к виду остальных названий: с заглавной буквы, если оригинал был с заглавной
                if (char.IsUpper(key[0]) && char.IsLower(result[0]))
                    result = char.ToUpper(result[0]) + result[1..];

                Cache[key] = result;
                return result;
            }
            catch (Exception)
            {
                // сервис перевода недоступен — оставляем как есть, попробуем при следующем открытии списка
                return text;
            }
        }
    }
}