using System;
using System.Collections.Generic;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // The settle of a cook gives each recipe id its items with the game's entry builder. The builder counts the items
    // of the whole list, and a tag takes the first fitting item, so an item of an upper row that no dish of its row
    // used could go to a dish of a lower row. With row split the builder runs once for each row, with the items of
    // that row and the recipe ids of that row, which the builder itself finds (RowSplitLogic.SplitIds). The mod reads
    // only the Count of the game's lists and joins them with AddRange: an entry is a struct with a reference field,
    // which the interop can read wrongly. It needs no plan, so it works the same after a load.
    [HarmonyPatch(typeof(Furniture), "BuildMatchedEntriesFromRecipeIds")]
    internal static class RowSplitOnSettle
    {
        private static bool inner;

        private static bool Prefix(Furniture __instance, ref Il2CppDict.List<ItemData> workbenchItems, Il2CppDict.List<int> recipeIds, ref Il2CppDict.List<Furniture.MatchedRecipeEntry> __result)
        {
            if (inner) return true;
            try
            {
                if (workbenchItems == null) return true;
                var items = RowPlanner.Items(workbenchItems);
                var role = BaseSingleton<BattleLogicWorld>.Instance._AgentManager.GetLeadingRole();
                var record = role == null ? null : AgentTools.GetAgentComponent<CookingRecordComponent>(role);
                if (record == null) return true;
                if (!RowSplitLogic.Applies(true, record.CookingLevel, Reducer_Web_Cooking.GetMultiPotUnlockLevel(), false, RowSplitLogic.Rows(items).Count)) return true;

                // The items of each row, top row first, in the order of the list.
                var byRow = new SortedDictionary<int, Il2CppDict.List<ItemData>>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (!byRow.TryGetValue(items[i].Y, out var list)) byRow[items[i].Y] = list = new Il2CppDict.List<ItemData>();
                    list.Add(workbenchItems[i]);
                }
                var ys = new List<int>(byRow.Keys);
                var rowItems = new List<Il2CppDict.List<ItemData>>(byRow.Values);
                int idCount = recipeIds == null ? 0 : recipeIds.Count;
                int[] split = idCount == 0 ? null : RowSplitLogic.SplitIds(rowItems.Count, idCount, (row, offset, k) => Build(__instance, rowItems[row], recipeIds, offset, k).Count);

                if (split == null)
                {
                    // A cook that started with the game's own split (or with no recipe id): the items in row order, as
                    // before this rule, and the game's builder on the whole list.
                    var ordered = new Il2CppDict.List<ItemData>();
                    foreach (int i in RowSplitLogic.RowOrder(items)) ordered.Add(workbenchItems[i]);
                    workbenchItems = ordered;
                    if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split settle fallback ids={Ids(recipeIds, 0, idCount)} rows={string.Join(",", ys)}, the items in row order");
                    return true;
                }

                Il2CppDict.List<Furniture.MatchedRecipeEntry> result = null;
                var log = new StringBuilder();
                int at = 0;
                for (int row = 0; row < split.Length; row++)
                {
                    log.Append(log.Length > 0 ? " " : "").Append(ys[row]).Append(":[").Append(Ids(recipeIds, at, split[row])).Append(']');
                    if (split[row] == 0) continue;
                    var entries = Build(__instance, rowItems[row], recipeIds, at, split[row]);
                    at += split[row];
                    if (result == null) result = entries;
                    else result.AddRange(entries.Cast<Il2CppDict.IEnumerable<Furniture.MatchedRecipeEntry>>());
                }
                __result = result;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split settle rows={log}");
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Row split settle failed, the game's own order applies: {e}");
                return true;
            }
        }

        // The game's builder for the items of one row and the k recipe ids from offset.
        private static Il2CppDict.List<Furniture.MatchedRecipeEntry> Build(Furniture furniture, Il2CppDict.List<ItemData> rowItems, Il2CppDict.List<int> recipeIds, int offset, int k)
        {
            var ids = new Il2CppDict.List<int>();
            for (int i = offset; i < offset + k; i++) ids.Add(recipeIds[i]);
            inner = true;
            try { return furniture.BuildMatchedEntriesFromRecipeIds(rowItems, ids); }
            finally { inner = false; }
        }

        private static string Ids(Il2CppDict.List<int> recipeIds, int offset, int k)
        {
            var text = new StringBuilder();
            for (int i = offset; i < offset + k; i++) text.Append(i > offset ? "," : "").Append(recipeIds[i]);
            return text.ToString();
        }
    }
}
