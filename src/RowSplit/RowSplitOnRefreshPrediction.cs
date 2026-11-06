using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // Makes the row plan after each prediction refresh. It runs before the preview Postfix, which reads the plan. Each
    // refresh first sets the plan back, also with RowSplit off, so the preview never reads the plan of an earlier refresh.
    // While auto off is active the game's own list stays.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshPrediction")]
    internal static class RowSplitOnRefreshPrediction
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(State_Web_Cooking state, Il2CppSystem.Collections.Generic.IEnumerable<Config_CookingRecipe> allRecipes)
        {
            RowPlanner.Current = null;
            try
            {
                if (state == null) return;
                long owner = state.CurrentFurnitureId;
                var autoOff = RowPlanner.AutoOff;
                if (autoOff.IsActive(owner))
                {
                    var ids = ItemIds(state);
                    if (autoOff.Keep(owner, ids) && Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split auto off end owner={owner} recipe=0 ids={string.Join(",", ids)}");
                }
                if (!Plugin.RowSplit.Value) return;
                int clicked = RowPlanner.ClickedRecipe;
                if (clicked == 0 && autoOff.IsActive(owner)) return;
                // The game's own list, before the plan writes over it.
                string wholeJson = state.PredictionListJson.Value;
                var plan = RowPlanner.Make(state, allRecipes);
                if (plan == null) return;
                if (clicked != 0) Decide(plan, clicked, wholeJson);
                if (autoOff.IsActive(owner)) return;
                string json = RowSplitLogic.PredictionJson(RowPlanner.Entries(plan, state, allRecipes));
                state.PredictionListJson.Value = json;
                RowPlanner.Current = plan;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split plan owner={plan.OwnerId} mode={(plan.Separate ? "separate" : "mixed")} rows={RowPlanner.Describe(plan)} list={json}");
            }
            catch (Exception e)
            {
                RowPlanner.Current = null;
                Plugin.Log.LogWarning($"Row split plan failed, the game's own split applies: {e}");
            }
            finally
            {
                try { if (state != null) RowPlanner.WriteToggle(state); }
                catch (Exception e) { Plugin.Log.LogWarning($"Row split: the toggle state failed: {e}"); }
            }
        }

        // The refresh of a click on Load: auto off starts when only the game's match of all items gives the clicked recipe.
        private static void Decide(RowPlanner.Plan plan, int clicked, string wholeJson)
        {
            var clickedKey = RowPlanner.Key(clicked);
            if (clickedKey == null) return;
            var whole = new List<RowSplitLogic.RecipeKey>();
            foreach (var entry in PreviewLogic.ReadEntries(wholeJson))
            {
                var key = RowPlanner.Key(entry.RecipeId);
                if (key != null) whole.Add(key.Value);
            }
            if (!RowAutoOff.Starts(clickedKey.Value, whole, RowPlanner.Keys(plan.Dishes))) return;
            var ids = new List<long>();
            foreach (var row in plan.Rows)
                foreach (var item in row.Items) ids.Add(item.Id);
            RowPlanner.AutoOff.Begin(plan.OwnerId, ids);
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split auto off start owner={plan.OwnerId} recipe={clicked} ids={string.Join(",", ids)}");
        }

        private static List<long> ItemIds(State_Web_Cooking state)
        {
            var ids = new List<long>();
            var items = state.WorkbenchItems;
            for (int i = 0; items != null && i < items.Count; i++) ids.Add(items[i].ItemId.Value);
            return ids;
        }
    }

    // Auto off holds instance ids of one game session: they can repeat after a load.
    [HarmonyPatch(typeof(AgentManager), "StartBattle")]
    internal static class RowSplitOnStartBattle
    {
        private static void Prefix() => RowPlanner.AutoOff.Clear();
    }
}
