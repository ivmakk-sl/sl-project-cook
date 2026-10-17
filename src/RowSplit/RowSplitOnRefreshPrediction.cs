using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // Makes the row plan after each prediction refresh. It runs before the preview Postfix, which reads the plan.
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
                var plan = RowPlanner.Make(state, allRecipes);
                if (plan == null) return;
                string json = RowSplitLogic.PredictionJson(RowPlanner.Entries(plan, state, allRecipes));
                state.PredictionListJson.Value = json;
                RowPlanner.Current = plan;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split plan owner={plan.OwnerId} rows={RowPlanner.Describe(plan)} list={json}");
            }
            catch (Exception e)
            {
                RowPlanner.Current = null;
                Plugin.Log.LogWarning($"Row split plan failed, the game's own split applies: {e}");
            }
        }
    }
}
