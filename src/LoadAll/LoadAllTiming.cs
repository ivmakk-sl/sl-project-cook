using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // The timing lines of the Recipe Book for the budget of Load from all tabs: one refresh of its rows and one click
    // on a row, patched with Verbose alone and apart from the patch group of the feature, so they also give the time
    // with LoadFromAllTabs off.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshRecipeList")]
    internal static class LoadAllTimingOnRefresh
    {
        private static long start;

        private static void Prefix() => start = Timing.Start();

        private static void Postfix(State_Web_Cooking state)
        {
            try
            {
                int rows = state?.RecipeList?.Count ?? 0;
                Plugin.Log.LogDebug(FormattableString.Invariant($"timing: book refresh {Timing.Ms(start):0.00} ms, rows {rows}, items {TabIndex.ItemCount}"));
            }
            catch (Exception e) { Plugin.Log.LogWarning($"Project Cook timing failed: {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_RecipeClick")]
    internal static class LoadAllTimingOnRecipeClick
    {
        private static long start;

        private static void Prefix() => start = Timing.Start();

        private static void Postfix() =>
            Plugin.Log.LogDebug(FormattableString.Invariant($"timing: recipe click {Timing.Ms(start):0.00} ms"));
    }
}
