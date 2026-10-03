using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The cooking window: RefreshBagItems builds the items of a grid at the open (RA_Open), at each switch of the
    // container tab (ApplySwitchBag), at each change of the items (RA_RefreshBag), and after the game's Auto Organize
    // (SortLeftBag). The owner comes from the items themselves: RA_Open builds a new window state, so the state that
    // the store gives can still be the old one during this call. The bag is sortable here; the items of the
    // workbench go with the numbers of the container tab, so the workbench shows them too. An empty tab has no owner and sends nothing; it has no item to sort.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshBagItems")]
    internal static class CookingSortOnRefresh
    {
        private static void Postfix(Il2CppDict.List<Data_Item> itemList, float currentHours)
        {
            try
            {
                if (itemList == null || itemList.Count == 0) return;
                var first = itemList[0];
                if (first == null) return;
                SortData.RequestCooking(first.OwnerId, itemList, currentHours);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook cooking sort failed: {e}");
            }
        }
    }
}
