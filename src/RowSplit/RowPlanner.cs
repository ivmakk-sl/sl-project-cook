using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The row plan of the cooking window: for each row of the cooking station, the dishes that the game's own match
    // gives for the items of that row alone. The preview shows it, and the start sends its recipe ids.
    internal static class RowPlanner
    {
        internal sealed class Dish
        {
            public int Row;
            public Config_CookingRecipe Recipe;
            // The items that the dish uses, by config id, without the seasonings that only change the tier.
            public Il2CppDict.Dictionary<int, int> Participated;
        }

        internal sealed class Plan
        {
            public long OwnerId;
            public string Signature;
            public List<RowSplitLogic.Row> Rows;
            public List<Dish> Dishes = new List<Dish>();
            // The config id of each seasoning item of the cooking station, one for each item.
            public List<int> SeasoningIds = new List<int>();
        }

        // The plan of the last prediction refresh, or null when row split does not apply. The client and the
        // authority run in one process, so the start reads it from here.
        internal static Plan Current;

        private const int SeasoningFoodType = 8;
        // The game's loop ends when no recipe matches. The limit only guards against a recipe that uses no item.
        private const int MaxDishesPerRow = 32;

        // Null when row split does not apply to the cooking station of the state.
        internal static Plan Make(State_Web_Cooking state, Il2CppSystem.Collections.Generic.IEnumerable<Config_CookingRecipe> allRecipes)
        {
            var items = state.WorkbenchItems;
            if (items == null) return null;
            var list = new List<RowSplitLogic.Item>();
            var seasonings = new List<int>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                list.Add(new RowSplitLogic.Item(it.ItemId.Value, it.ConfigId.Value, it.SubCategory.Value, it.X.Value, it.Y.Value, it.W.Value, it.H.Value));
                if (it.SubCategory.Value == SeasoningFoodType) seasonings.Add(it.ConfigId.Value);
            }
            var rows = RowSplitLogic.Rows(list);
            if (!RowSplitLogic.Applies(true, state.CookingLevel, Reducer_Web_Cooking.GetMultiPotUnlockLevel(), state.HotPotMode.Value, rows.Count))
                return null;
            var allowed = state.AllowedRecipeSet;
            if (allowed == null) return null;

            // The furniture that gets the start (RA_StartCook sends it to CurrentFurnitureId).
            long ownerId = state.CurrentFurnitureId;
            var plan = new Plan { OwnerId = ownerId, Signature = RowSplitLogic.Signature(ownerId, rows), Rows = rows, SeasoningIds = seasonings };
            foreach (var row in rows)
            {
                // The loop of PreMatchRecipes on the items of this row only. BuildParticipatedIngredients takes the
                // used items out of ids and tags.
                var counts = RowSplitLogic.Counts(row);
                var ids = ToIl2Cpp(counts.Ids);
                var tags = ToIl2Cpp(counts.Tags);
                for (int n = 0; n < MaxDishesPerRow; n++)
                {
                    int tier = Reducer_Web_Cooking.ResolvePredictionTier(ids, state.TagTierFloorRank);
                    var recipe = Reducer_Web_Cooking.SelectBestMatchedRecipe(state, allowed, ids, tags, allRecipes, tier, int.MaxValue);
                    if (recipe == null) break;
                    var participated = Reducer_Web_Cooking.BuildParticipatedIngredients(recipe, ids, tags);
                    plan.Dishes.Add(new Dish { Row = row.Y, Recipe = recipe, Participated = participated });
                }
            }
            return plan;
        }

        // The prediction list of the plan, one entry for each dish, with the game's rules of RefreshPrediction for
        // the name, the level, the icon, and the tier. A plan with no dish gives the game's entry for no match.
        internal static List<RowSplitLogic.PredictionEntry> Entries(Plan plan, State_Web_Cooking state, Il2CppSystem.Collections.Generic.IEnumerable<Config_CookingRecipe> allRecipes)
        {
            var entries = new List<RowSplitLogic.PredictionEntry>();
            if (plan.Dishes.Count == 0)
            {
                entries.Add(new RowSplitLogic.PredictionEntry { RecipeId = 0, Name = ConstantTextTools.ToConstantText("SR_Web_Cooking_59"), Icon = "" });
                return entries;
            }
            int level = state.CookingLevel;
            foreach (var dish in plan.Dishes)
            {
                var recipe = dish.Recipe;
                var entry = new RowSplitLogic.PredictionEntry { RecipeId = recipe.ID };
                bool known = state.UnlockedRecipeIds.Contains(recipe.ID) || (recipe.show_level > 0 && recipe.show_level <= level);
                if (known)
                {
                    entry.Name = ConstantTextTools.GetLocalText(recipe.RecipeName);
                    entry.Level = 2;
                    entry.Icon = Reducer_Web_Cooking.GetRecipeIcon(recipe);
                }
                else
                {
                    var best = Reducer_Web_Cooking.FindBestKnownReadyRecipe(state, level, recipe, Copy(dish.Participated), allRecipes);
                    entry.Level = 1;
                    if (best == null)
                    {
                        entry.Name = ConstantTextTools.ToConstantText("SR_Web_Cooking_57");
                        entry.Icon = "";
                    }
                    else
                    {
                        entry.Name = string.Format(ConstantTextTools.ToConstantText("SR_Web_Cooking_64"), ConstantTextTools.GetLocalText(best.RecipeName));
                        entry.Icon = Reducer_Web_Cooking.GetRecipeIcon(best);
                    }
                }
                entry.IsExact = recipe.SpecificItems != null && recipe.SpecificItems.Count > 0;
                if (!entry.IsExact)
                {
                    var tierCounts = RowSplitLogic.WithSeasonings(ToManaged(dish.Participated), plan.SeasoningIds, false);
                    entry.Tier = Reducer_Web_Cooking.ResolvePredictionTier(ToIl2Cpp(tierCounts), state.TagTierFloorRank);
                }
                entries.Add(entry);
            }
            return entries;
        }

        // "row:[configIds]->[recipeIds]" for each row, for the log.
        internal static string Describe(Plan plan)
        {
            var sb = new StringBuilder();
            foreach (var row in plan.Rows)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(row.Y).Append(":[");
                for (int i = 0; i < row.Items.Count; i++) sb.Append(i > 0 ? "," : "").Append(row.Items[i].ConfigId);
                sb.Append("]->[");
                bool first = true;
                foreach (var dish in plan.Dishes)
                {
                    if (dish.Row != row.Y) continue;
                    sb.Append(first ? "" : ",").Append(dish.Recipe.ID);
                    first = false;
                }
                sb.Append(']');
            }
            return sb.ToString();
        }

        // The items of the cooking station on the authority side, with their bag cells.
        internal static List<RowSplitLogic.Item> Items(Il2CppDict.List<ItemData> items)
        {
            var list = new List<RowSplitLogic.Item>();
            if (items == null) return list;
            var config = ConfigManager.Instance;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                int sub = config.Get_Config_Item(it.ItemConfigId)?.SubCategory ?? 0;
                list.Add(new RowSplitLogic.Item(it.InstanceId, it.ItemConfigId, sub, it.BagPos.x, it.BagPos.y, it.ItemSize.x, it.ItemSize.y));
            }
            return list;
        }

        private static Il2CppDict.Dictionary<int, int> Copy(Il2CppDict.Dictionary<int, int> source) => ToIl2Cpp(ToManaged(source));

        internal static Dictionary<int, int> ToManaged(Il2CppDict.Dictionary<int, int> source)
        {
            var d = new Dictionary<int, int>();
            var e = source.GetEnumerator();
            while (e.MoveNext()) d[e.Current.Key] = e.Current.Value;
            return d;
        }

        private static Il2CppDict.Dictionary<int, int> ToIl2Cpp(Dictionary<int, int> source)
        {
            var d = new Il2CppDict.Dictionary<int, int>();
            foreach (var pair in source) d[pair.Key] = pair.Value;
            return d;
        }
    }
}
