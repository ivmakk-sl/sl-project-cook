using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // The game's admission of an item to the cooking station counts the item itself when it is already there, so
    // with a full cooking station each move inside it is refused with no message, while the page shows the item at
    // its new cell. With row split the cell decides the dish, so a move inside the cooking station passes: the item
    // passed the food checks when it went in.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "CheckCookingAdmission")]
    internal static class RowSplitOnAdmission
    {
        private static void Postfix(long itemId, State_Web_Cooking state, ref bool __result)
        {
            if (__result) return;
            try
            {
                var items = state?.WorkbenchItems;
                for (int i = 0; items != null && i < items.Count; i++)
                {
                    if (items[i].ItemId.Value != itemId) continue;
                    __result = true;
                    if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split move inside the cooking station item={itemId}");
                    return;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Row split: the admission check of a move failed, the game's rule applies: {e}");
            }
        }
    }
}
