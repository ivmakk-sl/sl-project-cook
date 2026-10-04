using System;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The settle of a cook gives each recipe id its items with the game's entry builder. The builder gives a tag the
    // first fitting item in the order of the list, so the items in row order, top row first, give the dish of each
    // row the items of its row. It needs no plan, so it works the same after a load.
    [HarmonyPatch(typeof(Furniture), "BuildMatchedEntriesFromRecipeIds")]
    internal static class RowSplitOnSettle
    {
        private static void Prefix(ref Il2CppDict.List<ItemData> workbenchItems)
        {
            try
            {
                if (workbenchItems == null) return;
                var items = RowPlanner.Items(workbenchItems);
                int rowCount = RowSplitLogic.Rows(items).Count;
                var role = BaseSingleton<BattleLogicWorld>.Instance._AgentManager.GetLeadingRole();
                var record = role == null ? null : AgentTools.GetAgentComponent<CookingRecordComponent>(role);
                if (record == null) return;
                if (!RowSplitLogic.Applies(true, record.CookingLevel, Reducer_Web_Cooking.GetMultiPotUnlockLevel(), false, rowCount)) return;

                var ordered = new Il2CppDict.List<ItemData>();
                var log = new StringBuilder();
                foreach (int i in RowSplitLogic.RowOrder(items))
                {
                    ordered.Add(workbenchItems[i]);
                    log.Append(log.Length > 0 ? "," : "").Append(items[i].Y).Append(':').Append(items[i].Id);
                }
                workbenchItems = ordered;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split settle order={log}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Row split settle order failed, the game's own order applies: {e}");
            }
        }
    }
}
