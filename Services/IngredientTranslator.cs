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
    }
}