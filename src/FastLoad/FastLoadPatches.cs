using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // One refresh for each click on Load: the game's click (Reducer_Web_Cooking.RA_RecipeClick) moves the items of the
    // fill one at a time, and each move refreshes the whole cooking window (Reducer_Data_Item.UpdateBag sends
    // Ac_Cooking_RefreshBag). These patches hold each refresh of the cooking window while the click runs and send one
    // refresh at its end. No move of the click reads State_Web_Cooking (read with NativeDisasm in game 1.1.19611,
    // change 0105 design.md), so the held refreshes change nothing that a later move needs. The patches are one group
    // (Plugin.PatchGroup): all or none.
    internal static class FastLoadPatches
    {
        internal static readonly Type[] Types =
        {
            typeof(FastLoadOnRecipeClick),
            typeof(FastLoadOnRefreshBag),
        };

        internal static readonly FastLoadLogic Hold = new FastLoadLogic();

        // Ends a click and sends its one refresh when a refresh was held. The hold is off here, so the refresh runs.
        internal static void End(bool abort)
        {
            int held = Hold.Held;
            bool send = abort ? Hold.Abort() : Hold.End();
            if (Plugin.Verbose.Value)
                Plugin.Log.LogDebug($"load: click held refreshes={held} sent={(send ? "true" : "false")}{(abort ? " after an exception" : "")}");
            if (send) Ac_Cooking_RefreshBag.SendAction();
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_RecipeClick")]
    internal static class FastLoadOnRecipeClick
    {
        private static void Prefix() => FastLoadPatches.Hold.Begin();

        // First of the Postfixes, so the one refresh falls inside the timing line of the click (LoadAllTimingOnRecipeClick).
        [HarmonyPriority(Priority.First)]
        private static void Postfix() => FastLoadPatches.End(false);

        // The click threw after some moves: the hold ends, and the one refresh shows the items that moved. With no
        // exception the Postfix ended the click, and so it did when a later Postfix threw: then no click is open.
        private static void Finalizer(Exception __exception)
        {
            if (__exception != null && FastLoadPatches.Hold.InClick) FastLoadPatches.End(true);
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "Trigger_RA_RefreshBag")]
    internal static class FastLoadOnRefreshBag
    {
        private static bool Prefix() => !FastLoadPatches.Hold.TryHold();
    }
}
