using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // Unknown recipes in the Codex filter (UnknownRecipesWithIngredients, UnknownRecipesWithoutIngredients): the Recipe Book has a row only for a dish of which the
    // character knows a recipe, so after each refresh these patches build a row for each recipe of the other dishes that
    // the station allows and whose dish has no codex entry, with the game's own texts and status. The rows go into the
    // book data, and the page adds them to the filtered list only; the game's rows (state.RecipeList) do not change.
    // The patches are one group (Plugin.PatchGroup): all or none.
    internal static class UnknownRecipes
    {
        internal static readonly Type[] Types = { typeof(UnknownOnEvaluateRecipeForBook), typeof(UnknownOnRefreshRecipeList) };

        // The private methods of Reducer_Web_Cooking that the rows call. A missing one (a game update) turns the group off.
        private static readonly string[] RowMethods = { "BuildCostText", "BuildRecipeEffectText", "TryPlanRecipeFill", "IsRecipeKnownToPlayer", "IsRecipeCraftUnlocked" };

        // The name of the first row method that is missing, or null.
        internal static string MissingRowMethod()
        {
            foreach (var name in RowMethods)
                if (AccessTools.Method(typeof(Reducer_Web_Cooking), name) == null) return name;
            return null;
        }

        // The refresh of the Recipe Book that runs now: its first evaluation keeps the references of the refresh.
        internal static readonly RefreshMark Refresh = new RefreshMark();

        // The references of the refresh that runs now, kept by the first EvaluateRecipeForBook and cleared at the mark
        // and after the rows: the counts of the station and of all places of the fill, the dishes of the pot, and the
        // recipes of the plan check. Evaluated is false when the refresh evaluated no recipe (hot pot mode, or no
        // known dish at this station).
        internal static bool Evaluated;
        internal static State_Data_Item ItemState;
        internal static Il2CppDict.IEnumerable<Config_CookingRecipe> AllRecipes;
        internal static Il2CppDict.HashSet<int> PotMatched;
        internal static Il2CppDict.Dictionary<int, int> StationIds, StationTags, TotalIds, TotalTags;

        internal static void Clear()
        {
            Evaluated = false;
            ItemState = null;
            AllRecipes = null;
            PotMatched = null;
            StationIds = StationTags = TotalIds = TotalTags = null;
        }

        private static readonly TextCache texts = new TextCache();
        // The needed counts of each recipe (CountMaterials of SpecificItems, else of TagCombo), and true for a tag
        // recipe. The recipe config does not change while the game runs.
        private static readonly Dictionary<int, KeyValuePair<bool, List<KeyValuePair<int, int>>>> needed =
            new Dictionary<int, KeyValuePair<bool, List<KeyValuePair<int, int>>>>();
        private static bool warned;

        // Builds the unknown recipe rows of the refresh that just ran and puts them into the book data.
        internal static void Build(State_Web_Cooking state)
        {
            long start = Timing.Start();
            try
            {
                if (state == null) return;
                bool hotPot = state.HotPotMode != null && state.HotPotMode.Value;
                var config = ConfigManager.Instance;
                var candidates = CodexLogic.UnknownCandidates(Allowed(state), BookData.Ids, RowGroupIds(state), CodexReader.DishOf,
                    id => Reducer_Web_Cooking.IsRecipeKnownToPlayer(state, config.Get_Config_CookingRecipe(id)),
                    id => Reducer_Web_Cooking.IsRecipeCraftUnlocked(state, config.Get_Config_CookingRecipe(id)),
                    Evaluated, hotPot);
                int language = ModTexts.Language();
                var rows = CodexLogic.UnknownRows(candidates, CodexReader.DishOf, id => Status(state, config.Get_Config_CookingRecipe(id)),
                    id => texts.Get(id, language, BuildTexts), Plugin.UnknownRecipesWithIngredients.Value, Plugin.UnknownRecipesWithoutIngredients.Value);
                BookData.SetUnknown(rows);
                if (Plugin.Verbose.Value)
                {
                    int load = 0;
                    foreach (var row in rows)
                        if (row.Status == 1) load++;
                    Plugin.Log.LogDebug(FormattableString.Invariant($"codex: unknown rows={rows.Count} load={load} {Timing.Ms(start):0.00} ms"));
                }
            }
            catch (Exception e)
            {
                // The rows of an earlier refresh can belong to another station or other ingredients: none stays.
                BookData.SetUnknown(new PageJson.UnknownRow[0]);
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning($"Project Cook cannot build the unknown recipe rows, so the Codex filter shows only the known recipes: {e}");
                }
            }
            finally
            {
                Clear();
                // A refresh with no evaluation leaves its mark: an evaluation outside a refresh must not take it.
                Refresh.TakeFirst();
            }
        }

        private static List<int> Allowed(State_Web_Cooking state)
        {
            var ids = new List<int>();
            var set = state.AllowedRecipeSet;
            if (set == null) return ids;
            var e = set.GetEnumerator();
            while (e.MoveNext()) ids.Add(e.Current);
            return ids;
        }

        private static List<int> RowGroupIds(State_Web_Cooking state)
        {
            var ids = new List<int>();
            var list = state.RecipeList;
            for (int i = 0; list != null && i < list.Count; i++)
            {
                var row = list[i];
                if (row?.GroupId != null) ids.Add(row.GroupId.Value);
            }
            return ids;
        }

        // The status of EvaluateRecipeForBook over the counts of the refresh, with the game's plan check of the fill.
        private static int Status(State_Web_Cooking state, Config_CookingRecipe recipe)
        {
            if (recipe == null) return 0;
            var need = Needed(recipe);
            var station = need.Key ? StationTags : StationIds;
            var total = need.Key ? TotalTags : TotalIds;
            int count = CodexLogic.CountStatus(need.Value, id => Count(station, id), id => Count(total, id));
            return CodexLogic.GameStatus(count, PotMatched != null && PotMatched.Contains(recipe.ID),
                () => Reducer_Web_Cooking.TryPlanRecipeFill(state, ItemState, recipe, AllRecipes, out _));
        }

        private static int Count(Il2CppDict.Dictionary<int, int> counts, int id) =>
            counts != null && counts.ContainsKey(id) ? counts[id] : 0;

        private static KeyValuePair<bool, List<KeyValuePair<int, int>>> Needed(Config_CookingRecipe recipe)
        {
            if (needed.TryGetValue(recipe.ID, out var need)) return need;
            var items = recipe.SpecificItems;
            var tags = recipe.TagCombo;
            if (items != null && items.Count > 0) need = new KeyValuePair<bool, List<KeyValuePair<int, int>>>(false, LoadAllLogic.Needed(ToList(items)));
            else if (tags != null && tags.Count > 0) need = new KeyValuePair<bool, List<KeyValuePair<int, int>>>(true, LoadAllLogic.Needed(ToList(tags)));
            else need = new KeyValuePair<bool, List<KeyValuePair<int, int>>>(false, null);
            needed[recipe.ID] = need;
            return need;
        }

        private static List<int> ToList(Il2CppDict.List<int> list)
        {
            var ids = new List<int>();
            for (int i = 0; i < list.Count; i++) ids.Add(list[i]);
            return ids;
        }

        private static UnknownTexts BuildTexts(int recipeId)
        {
            var config = ConfigManager.Instance;
            var recipe = config.Get_Config_CookingRecipe(recipeId);
            return new UnknownTexts(config.GetLocalTxt(recipe.RecipeName), Reducer_Web_Cooking.BuildCostText(recipe), Reducer_Web_Cooking.BuildRecipeEffectText(recipe));
        }
    }

    // The first evaluation of each refresh keeps the references of the refresh. The return value (the private struct
    // RecipeBookEval) is not touched.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "EvaluateRecipeForBook")]
    internal static class UnknownOnEvaluateRecipeForBook
    {
        private static void Prefix(State_Data_Item itemState, Il2CppDict.IEnumerable<Config_CookingRecipe> allRecipes, Il2CppDict.HashSet<int> potMatchedIds,
            Il2CppDict.Dictionary<int, int> workbenchCountById, Il2CppDict.Dictionary<int, int> workbenchTags,
            Il2CppDict.Dictionary<int, int> totalCountById, Il2CppDict.Dictionary<int, int> totalTags)
        {
            if (!UnknownRecipes.Refresh.TakeFirst()) return;
            UnknownRecipes.Evaluated = true;
            UnknownRecipes.ItemState = itemState;
            UnknownRecipes.AllRecipes = allRecipes;
            UnknownRecipes.PotMatched = potMatchedIds;
            UnknownRecipes.StationIds = workbenchCountById;
            UnknownRecipes.StationTags = workbenchTags;
            UnknownRecipes.TotalIds = totalCountById;
            UnknownRecipes.TotalTags = totalTags;
        }
    }

    // The mark of each refresh of the Recipe Book, and the rows after it. The Postfix runs before the Postfix of Load
    // from all tabs, so the searches of its plan checks count in the refresh summary of that feature.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshRecipeList")]
    internal static class UnknownOnRefreshRecipeList
    {
        private static void Prefix()
        {
            UnknownRecipes.Clear();
            UnknownRecipes.Refresh.Mark();
        }

        [HarmonyPriority(Priority.High)]
        private static void Postfix(State_Web_Cooking state) => UnknownRecipes.Build(state);
    }
}
