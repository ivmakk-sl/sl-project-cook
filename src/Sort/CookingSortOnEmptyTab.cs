using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The cooking window with an empty container tab: RefreshBagItems has no item to give the owner, so these
    // Postfixes of the reducers that set the tab (the open, a tab switch, a change of the items) send the sort data
    // of the empty tab with its owner from the new state. The page then keeps the dropdown, and the workbench keeps
    // its badges.
    internal static class CookingEmptyTab
    {
        internal static void Send(State_Web_Cooking state)
        {
            try
            {
                if (state == null || state.BagItems == null || state.BagItems.Count > 0 || state.BagOwnerId == null) return;
                SortData.RequestCooking(state.BagOwnerId.Value, new Il2CppDict.List<Data_Item>(), 0f);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook empty tab sort failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_Open")]
    internal static class CookingSortOnOpen
    {
        private static void Postfix(State_Web_Cooking __result) => CookingEmptyTab.Send(__result);
    }

    // Trigger_RA_SwitchBag calls ApplySwitchBag directly (RA_SwitchBag is inlined into it).
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "ApplySwitchBag")]
    internal static class CookingSortOnSwitchBag
    {
        private static void Postfix(State_Web_Cooking __result) => CookingEmptyTab.Send(__result);
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_RefreshBag")]
    internal static class CookingSortOnRefreshBag
    {
        private static void Postfix(State_Web_Cooking __result) => CookingEmptyTab.Send(__result);
    }
}
