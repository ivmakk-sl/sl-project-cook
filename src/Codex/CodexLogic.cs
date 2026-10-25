using System;
using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free rules of the codex marks (which dish card or food has no codex entry yet), the dishes in progress, and
    // the unknown recipe rows of the Codex filter. This file must not use any
    // game or BepInEx type, because the unit tests compile it alone.
    public static class CodexLogic
    {
        // CodexCategory of the game: Food (the tasted foods) and Dish (the cooked dishes).
        public const int FoodCategory = 1, DishCategory = 2;

        // Config_CookingRecipe id of Struggle Meal: Furniture.SettleCookingResult cooks it when no recipe matches.
        public const int StruggleMealRecipeId = 100;

        // The recipe that a cook unlocks in the codex for a prediction entry: a dark dish (RecipeId 0) is Struggle Meal.
        public static int DishSource(int recipeId) => recipeId == 0 ? StruggleMealRecipeId : recipeId;

        // Config_Item.Category of a food.
        public const int FoodItemCategory = 1;

        // CodexManager.IsInCodex(Food, id): the item config is a food and has InCodex.
        public static bool IsFoodEntry(int itemCategory, bool inCodex) => itemCategory == FoodItemCategory && inCodex;

        // No mark while the game keeps the codex locked (guide and explore mode), for a thing with no codex entry, or
        // for an entry that is already unlocked.
        public static bool ShowMark(bool locked, bool isEntry, bool unlocked) => !locked && isEntry && !unlocked;

        // The dishes in progress: the dish of each recipe that a cooking station of the home cooks now (an id of 0 or
        // less is no recipe). dishOf gives the dish of a recipe by the same-dish rule of the codex.
        public static HashSet<int> DishesInProgress(IEnumerable<int> recipeIds, Func<int, int> dishOf)
        {
            var dishes = new HashSet<int>();
            foreach (int id in recipeIds)
                if (id > 0) dishes.Add(dishOf(id));
            return dishes;
        }

        // The book ids less each recipe of a dish in progress, so every recipe of that dish leaves the Codex filter.
        // With no dish in progress, the same set.
        public static HashSet<int> WithoutInProgress(HashSet<int> ids, HashSet<int> dishesInProgress, Func<int, int> dishOf)
        {
            if (ids == null || dishesInProgress == null || dishesInProgress.Count == 0) return ids;
            var result = new HashSet<int>();
            foreach (int id in ids)
                if (!dishesInProgress.Contains(dishOf(id))) result.Add(id);
            return result;
        }

        // The unknown recipes of the Codex filter: each recipe that the station allows, that is in the book ids, that is
        // not known (IsRecipeKnownToPlayer, true for each recipe of a dish of which one recipe is known), that is
        // craft-unlocked, and whose dish has no row in the Recipe Book. The dish of a row is the dish of its groupId (a
        // single row has the recipe id). None in hot pot mode or when the refresh evaluated no recipe (no known dish).
        public static List<int> UnknownCandidates(IEnumerable<int> allowed, HashSet<int> bookIds, IEnumerable<int> rowGroupIds,
            Func<int, int> dishOf, Func<int, bool> known, Func<int, bool> craftUnlocked, bool evaluated, bool hotPot)
        {
            var candidates = new List<int>();
            if (!evaluated || hotPot || bookIds == null) return candidates;
            var rowDishes = new HashSet<int>();
            foreach (int groupId in rowGroupIds) rowDishes.Add(dishOf(groupId));
            foreach (int id in allowed)
                if (bookIds.Contains(id) && !rowDishes.Contains(dishOf(id)) && !known(id) && craftUnlocked(id)) candidates.Add(id);
            return candidates;
        }

        // The count status of Reducer_Web_Cooking.EvaluateRecipeForBook (HasSufficientMaterials over the counts of
        // SpecificItems, else HasSufficientTags over the counts of TagCombo): 2 when the station counts hold each
        // needed count, else 1 when the totals hold them, else 0. A recipe with no list is 0.
        public static int CountStatus(IList<KeyValuePair<int, int>> needed, Func<int, int> station, Func<int, int> total)
        {
            if (needed == null || needed.Count == 0) return 0;
            if (Holds(needed, station)) return 2;
            return Holds(needed, total) ? 1 : 0;
        }

        private static bool Holds(IList<KeyValuePair<int, int>> needed, Func<int, int> count)
        {
            foreach (var kv in needed)
                if (count(kv.Key) < kv.Value) return false;
            return true;
        }

        // The status rule of EvaluateRecipeForBook for a craft-unlocked recipe: Ready stays when the recipe is a dish of
        // the pot (potMatchedIds); Ready of another dish and Load take the result of the game's plan of the fill
        // (TryPlanRecipeFill) as Load or not enough; not enough stays.
        public static int GameStatus(int countStatus, bool inPot, Func<bool> plan)
        {
            if (countStatus == 0 || (countStatus == 2 && inPot)) return countStatus;
            return plan() ? 1 : 0;
        }

        // The unknown recipe rows in the order of the candidates, with the dish key as their group: a row with Ready or
        // Load (the places of the fill hold the ingredients) with UnknownRecipesWithIngredients, a row with status 0 with
        // UnknownRecipesWithoutIngredients.
        public static List<PageJson.UnknownRow> UnknownRows(IEnumerable<int> candidates, Func<int, int> dishOf, Func<int, int> status,
            Func<int, UnknownTexts> texts, bool withIngredients, bool withoutIngredients)
        {
            var rows = new List<PageJson.UnknownRow>();
            foreach (int id in candidates)
            {
                int s = status(id);
                if (s == 0 ? !withoutIngredients : !withIngredients) continue;
                var t = texts(id);
                rows.Add(new PageJson.UnknownRow(id, dishOf(id), t.Name, t.CostText, t.EffectText, s));
            }
            return rows;
        }
    }

    // The texts of an unknown recipe row: the dish name, the ingredient line, and the effect line of the game.
    public readonly struct UnknownTexts
    {
        public readonly string Name, CostText, EffectText;

        public UnknownTexts(string name, string costText, string effectText) { Name = name; CostText = costText; EffectText = effectText; }
    }

    // The texts of the unknown recipe rows depend only on the recipe and the display language, so they are built once
    // for each recipe and kept until the language changes.
    public sealed class TextCache
    {
        private readonly Dictionary<int, UnknownTexts> texts = new Dictionary<int, UnknownTexts>();
        private int language = int.MinValue;

        public UnknownTexts Get(int recipeId, int language, Func<int, UnknownTexts> build)
        {
            if (language != this.language)
            {
                texts.Clear();
                this.language = language;
            }
            if (!texts.TryGetValue(recipeId, out var t))
            {
                t = build(recipeId);
                texts[recipeId] = t;
            }
            return t;
        }
    }

    // The key of the cached book data of the Recipe Book: the set of the recipes with no codex entry changes only with
    // a new dish entry (the unlocked count of the dish category), a change of the lock (another game mode), or a new or
    // stopped cook (the dishes in progress).
    public readonly struct BookKey : IEquatable<BookKey>
    {
        public readonly bool Locked;
        public readonly int Unlocked;
        // The dishes in progress, sorted.
        private readonly int[] inProgress;

        public BookKey(bool locked, int unlocked, IEnumerable<int> inProgress = null)
        {
            Locked = locked;
            Unlocked = unlocked;
            var dishes = inProgress == null ? new List<int>() : new List<int>(inProgress);
            dishes.Sort();
            this.inProgress = dishes.ToArray();
        }

        public bool Equals(BookKey other)
        {
            if (Locked != other.Locked || Unlocked != other.Unlocked) return false;
            int[] a = inProgress ?? new int[0], b = other.inProgress ?? new int[0];
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public override bool Equals(object obj) => obj is BookKey other && Equals(other);
        public override int GetHashCode() => (Locked ? 1 : 0) ^ (Unlocked << 1) ^ ((inProgress?.Length ?? 0) << 20);
    }
}
