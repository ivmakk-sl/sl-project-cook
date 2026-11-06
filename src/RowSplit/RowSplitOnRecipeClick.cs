using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // A click on Load names the dish that the player wants. The refresh of the click, which runs inside the click (the
    // one refresh of the fast load, or the moves of the click with the fast load off), decides auto off with the items
    // of the fill. A click that the game refuses has no refresh, so it decides nothing.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_RecipeClick")]
    internal static class RowSplitOnRecipeClick
    {
        private static void Prefix(Ac_Cooking_RecipeClick ac, State_Web_Cooking state)
        {
            RowPlanner.ClickedRecipe = 0;
            try
            {
                if (state?.IsCooking != null && state.IsCooking.Value) return;
                RowPlanner.ClickedRecipe = RowAutoOff.ClickedRecipe(ac?.JsonData);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split click recipe={RowPlanner.ClickedRecipe}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Row split: the read of a click on Load failed, auto off does not start: {e}");
            }
        }

        // After the Postfix of the fast load, which sends the one refresh of the click.
        [HarmonyPriority(Priority.Low)]
        private static void Postfix()
        {
            RowPlanner.ClickedRecipe = 0;
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug("row split click end");
        }

        private static void Finalizer(Exception __exception)
        {
            if (__exception != null) RowPlanner.ClickedRecipe = 0;
        }
    }
}
