using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The Rat Cage window has the shape of the cooking window (design D12): the container tab (the bag or a fridge)
    // and the Food Storage grid of the cage, a furniture that is not sortable, in the role of the cooking station.
    // RefreshBagItems builds the items of each grid at the open, at each switch of the tab, at each change of the
    // items, and at each change of the action marks. The owner comes from the items themselves, as in the cooking
    // window.
    [HarmonyPatch(typeof(Reducer_Web_RatCage), "RefreshBagItems")]
    internal static class RatCageSortOnRefresh
    {
        private static void Postfix(Il2CppDict.List<Data_Item> itemList, float currentHours)
        {
            try
            {
                if (itemList == null || itemList.Count == 0) return;
                var first = itemList[0];
                if (first == null) return;
                SortData.RequestCooking(first.OwnerId, itemList, currentHours, SortData.RatCage);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook rat cage sort failed: {e}");
            }
        }
    }

    // The Rat Cage window with an empty container tab: the sort data of the empty tab with its owner from the new
    // state, so the page keeps the dropdown and the Food Storage grid keeps its badges.
    internal static class RatCageEmptyTab
    {
        internal static void Send(State_Web_RatCage state)
        {
            try
            {
                if (state == null || state.BagItems == null || state.BagItems.Count > 0 || state.BagOwnerId == null) return;
                SortData.RequestCooking(state.BagOwnerId.Value, new Il2CppDict.List<Data_Item>(), 0f, SortData.RatCage);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook rat cage empty tab sort failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_RatCage), "RA_Open")]
    internal static class RatCageSortOnOpen
    {
        private static void Postfix(State_Web_RatCage __result) => RatCageEmptyTab.Send(__result);
    }

    [HarmonyPatch(typeof(Reducer_Web_RatCage), "ApplySwitchBag")]
    internal static class RatCageSortOnSwitchBag
    {
        private static void Postfix(State_Web_RatCage __result) => RatCageEmptyTab.Send(__result);
    }

    [HarmonyPatch(typeof(Reducer_Web_RatCage), "RA_RefreshBag")]
    internal static class RatCageSortOnRefreshBag
    {
        private static void Postfix(State_Web_RatCage __result) => RatCageEmptyTab.Send(__result);
    }

    // The page ready call of the Rat Cage window, so a pass installs the food sort in its frame.
    [HarmonyPatch(typeof(WebUI_RatCage), "CallShowAction")]
    internal static class PageTickOnRatCageShow
    {
        private static void Postfix()
        {
            try
            {
                PageTick.Request();
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug("rat cage window shown, pass requested");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook rat cage window ready failed: {e}");
            }
        }
    }
}
