namespace RecipeSite.Services
{
    /// <summary>
    /// Простой словарь для перевода распространённых названий продуктов
    /// с русского на английский, т.к. TheMealDB понимает только английские
    /// названия ингредиентов в фильтре filter.php?i=
    /// </summary>
    public static class IngredientTranslator
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            // Овощи
            { "помидор", "tomato" }, { "помидоры", "tomato" }, { "томат", "tomato" }, { "томаты", "tomato" },
            { "картошка", "potato" }, { "картофель", "potato" }, { "картофеля", "potato" },
            { "лук", "onion" }, { "лука", "onion" }, { "чеснок", "garlic" }, { "чеснока", "garlic" },
            { "морковь", "carrot" }, { "моркови", "carrot" }, { "огурец", "cucumber" }, { "огурцы", "cucumber" },
            { "перец", "pepper" }, { "перца", "pepper" }, { "болгарский перец", "bell pepper" },
            { "капуста", "cabbage" }, { "капусты", "cabbage" }, { "брокколи", "broccoli" },
            { "кабачок", "zucchini" }, { "кабачки", "zucchini" }, { "баклажан", "eggplant" }, { "баклажаны", "eggplant" },
            { "тыква", "pumpkin" }, { "тыквы", "pumpkin" }, { "шпинат", "spinach" }, { "салат", "lettuce" },
            { "грибы", "mushroom" }, { "гриб", "mushroom" }, { "кукуруза", "corn" }, { "горох", "peas" },
            { "фасоль", "beans" }, { "чечевица", "lentils" }, { "авокадо", "avocado" }, { "свекла", "beetroot" },

            // Мясо и рыба
            { "курица", "chicken" }, { "курицы", "chicken" }, { "куриное филе", "chicken breast" },
            { "говядина", "beef" }, { "говядины", "beef" }, { "свинина", "pork" }, { "свинины", "pork" },
            { "баранина", "lamb" }, { "индейка", "turkey" }, { "фарш", "mince" }, { "бекон", "bacon" },
            { "рыба", "fish" }, { "лосось", "salmon" }, { "тунец", "tuna" }, { "креветки", "shrimp" }, { "креветка", "shrimp" },

            // Молочные и яйца
            { "яйцо", "egg" }, { "яйца", "egg" }, { "молоко", "milk" }, { "сыр", "cheese" }, { "сыра", "cheese" },
            { "масло", "butter" }, { "сливочное масло", "butter" }, { "сливки", "cream" }, { "йогурт", "yogurt" },
            { "творог", "cottage cheese" }, { "сметана", "sour cream" },

            // Крупы, мука, выпечка
            { "рис", "rice" }, { "риса", "rice" }, { "мука", "flour" }, { "муки", "flour" },
            { "макароны", "pasta" }, { "спагетти", "spaghetti" }, { "хлеб", "bread" }, { "сахар", "sugar" },
            { "соль", "salt" }, { "гречка", "buckwheat" }, { "овсянка", "oats" },

            // Фрукты
            { "яблоко", "apple" }, { "яблоки", "apple" }, { "банан", "banana" }, { "бананы", "banana" },
            { "лимон", "lemon" }, { "лайм", "lime" }, { "апельсин", "orange" }, { "клубника", "strawberry" },
            { "мёд", "honey" }, { "мед", "honey" }, { "шоколад", "chocolate" },
        };

        /// <summary>
        /// Возвращает английский эквивалент, если слово найдено в словаре;
        /// иначе возвращает исходную строку (на случай, если пользователь уже ввёл английское название).
        /// </summary>
        public static string ToEnglish(string input)
        {
            var trimmed = input.Trim().ToLowerInvariant();
            return Map.TryGetValue(trimmed, out var eng) ? eng : trimmed;
        }
        private static readonly Dictionary<string, string> Reverse = BuildReverse();

        private static Dictionary<string, string> BuildReverse()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in Map)
            {
                if (!d.ContainsKey(pair.Value)) d[pair.Value] = pair.Key;
            }
            return d;
        }

        /// <summary>Русское название продукта, если оно есть в словаре, иначе исходное.</summary>
        public static string ToRussian(string english)
        {
            var e = english.Trim();
            if (Reverse.TryGetValue(e, out var ru) ||
                (e.Length > 1 && e.EndsWith("s", StringComparison.OrdinalIgnoreCase) && Reverse.TryGetValue(e[..^1], out ru)))
            {
                return char.ToUpper(ru[0]) + ru[1..];
            }
            return e;
        }
    }
}