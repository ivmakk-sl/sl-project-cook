using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // The storage window: RefreshItems_B builds the items of the opened storage (side B) at each open and each
    // change of its items, with the game time of the shelf life. Her Dishes also has a Postfix here; the two are
    // independent. The send holds the items of the Backpack side (A) too, which RefreshItems_A builds.
    [HarmonyPatch(typeof(Reducer_Web_BackpackUI), "RefreshItems_B")]
    internal static class StorageSortOnRefresh
    {
        private static void Postfix(State_Web_BackpackUI state, float currentHours)
        {
            try
            {
                if (state == null) return;
                SortData.RequestStorage(state, currentHours);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook storage sort failed: {e}");
            }
        }
    }

    // Side A of the storage window: the Backpack of a window of two grids, or a storage opened alone. A change of its
    // items alone (for example its Auto Organize) builds only side A, so it sends the numbers again.
    [HarmonyPatch(typeof(Reducer_Web_BackpackUI), "RefreshItems_A")]
    internal static class StorageBagSortOnRefresh
    {
        private static void Postfix(State_Web_BackpackUI state, float currentHours)
        {
            try
            {
                SortData.RequestStorage(state, currentHours);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook storage bag sort failed: {e}");
            }
        }
    }
}
