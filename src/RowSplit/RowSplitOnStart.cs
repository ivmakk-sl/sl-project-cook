using System;
using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The start of a cook: the authority gets the recipe ids of the row plan, and checks each row alone.
    [HarmonyPatch(typeof(Furniture), "OnCookingStart")]
    internal static class RowSplitOnStart
    {
        // The plan of this start, for the check of ValidateClientRecipeIds. Only set during OnCookingStart.
        internal static RowPlanner.Plan Pending;

        private static void Prefix(Furniture __instance, CookingStartData data)
        {
            Pending = null;
            // The plan stays for a second start after a refused one (no power at an electric stove), which comes with
            // no prediction refresh. The signature check below keeps a plan of other items from applying.
            var plan = RowPlanner.Current;
            if (plan == null || data == null) return;
            try
            {
                var items = BaseSingleton<BattleLogicWorld>.Instance._ItemManager.GetItemDataList(__instance.InstanceId);
                string signature = RowSplitLogic.Signature(__instance.InstanceId, RowSplitLogic.Rows(RowPlanner.Items(items)));
                if (signature != plan.Signature)
                {
                    Plugin.Log.LogWarning($"Row split: the items of the cooking station differ from the preview, so the game's own split applies. preview={plan.Signature} start={signature}");
                    return;
                }
                var ids = new Il2CppDict.List<int>();
                foreach (var dish in plan.Dishes) ids.Add(dish.Recipe.ID);
                data.RecipeIds = ids;
                Pending = plan;
                if (Plugin.Verbose.Value)
                {
                    var text = new StringBuilder();
                    foreach (var dish in plan.Dishes) text.Append(text.Length > 0 ? "," : "").Append(dish.Recipe.ID);
                    Plugin.Log.LogDebug($"row split start owner={__instance.InstanceId} ids={text}");
                }
            }
            catch (Exception e)
            {
                Pending = null;
                Plugin.Log.LogWarning($"Row split start failed, the game's own split applies: {e}");
            }
        }

        private static void Postfix() => Pending = null;
    }

    // Checks the recipe ids of each row against the items of that row only, with the game's own check, so the
    // recipe tier of a row does not depend on the items of other rows.
    [HarmonyPatch(typeof(Furniture), "ValidateClientRecipeIds")]
    internal static class RowSplitOnValidate
    {
        private static bool inner;

        private static bool Prefix(Furniture __instance, Il2CppDict.List<int> clientRecipeIds, Il2CppDict.List<ItemData> workbenchItems, Config_FurnitureCook cookConfig, ref Il2CppDict.List<int> __result)
        {
            var plan = RowSplitOnStart.Pending;
            if (inner || plan == null) return true;
            try
            {
                var byRow = new Dictionary<int, Il2CppDict.List<ItemData>>();
                for (int i = 0; i < workbenchItems.Count; i++)
                {
                    var item = workbenchItems[i];
                    int y = item.BagPos.y;
                    if (!byRow.TryGetValue(y, out var list)) byRow[y] = list = new Il2CppDict.List<ItemData>();
                    list.Add(item);
                }
                var accepted = new Il2CppDict.List<int>();
                var log = new StringBuilder();
                foreach (var row in plan.Rows)
                {
                    var ids = new Il2CppDict.List<int>();
                    foreach (var dish in plan.Dishes) if (dish.Row == row.Y) ids.Add(dish.Recipe.ID);
                    if (ids.Count == 0 || !byRow.TryGetValue(row.Y, out var rowItems)) continue;
                    Il2CppDict.List<int> rowAccepted;
                    inner = true;
                    try { rowAccepted = __instance.ValidateClientRecipeIds(ids, rowItems, cookConfig); }
                    finally { inner = false; }
                    log.Append(log.Length > 0 ? " " : "").Append(row.Y).Append(":[");
                    for (int i = 0; rowAccepted != null && i < rowAccepted.Count; i++)
                    {
                        accepted.Add(rowAccepted[i]);
                        log.Append(i > 0 ? "," : "").Append(rowAccepted[i]);
                    }
                    log.Append(']');
                }
                __result = accepted;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split validated rows={log}");
                return false;
            }
            catch (Exception e)
            {
                inner = false;
                Plugin.Log.LogWarning($"Row split check failed, the game checks the recipe ids of the whole cooking station: {e}");
                return true;
            }
        }
    }
}
