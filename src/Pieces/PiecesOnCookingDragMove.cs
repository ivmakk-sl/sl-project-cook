using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using UnityEngine;

namespace ProjectCook
{
    // Separate pieces: the game merges each piece that joins the cooking station into the first item of the same kind
    // (ItemManager.TryMergeIntoOwner), whatever cell the player dropped it on. During a drag to the cooking station
    // the mod merges a piece only into the item under the drop, and a piece dropped on a free cell stays its own item.
    [HarmonyPatch(typeof(ItemManager), "OnCookingDragMove")]
    internal static class PiecesOnCookingDragMove
    {
        // The drag that runs now, set only during OnCookingDragMove to a cooking station that is not in hot pot mode.
        internal sealed class Drag
        {
            public long ItemId, To;
            public int X, Y;
            public long? Target;
            public bool Merged;
        }

        internal static Drag Current;

        private static void Prefix(ItemManager __instance, long itemId, long toOwnerId, Vector2Int bagPos)
        {
            Current = null;
            try
            {
                if (!ItemManager.IsCookingFurniture(toOwnerId) || ItemManager.IsHotPotWorkbench(toOwnerId)) return;
                var dragged = __instance.GetItemData(itemId);
                if (dragged == null) return;
                var station = new List<PiecesLogic.Item>();
                var items = __instance.GetItemDataList(toOwnerId);
                for (int i = 0; items != null && i < items.Count; i++)
                {
                    var it = items[i];
                    station.Add(new PiecesLogic.Item(it.InstanceId, it.ItemConfigId, it.BagPos.x, it.BagPos.y, it.ItemSize.x, it.ItemSize.y));
                }
                var size = dragged.ItemSize;
                Current = new Drag
                {
                    ItemId = itemId, To = toOwnerId, X = bagPos.x, Y = bagPos.y,
                    Target = PiecesLogic.MergeTarget(bagPos.x, bagPos.y, size.x, size.y, dragged.ItemConfigId, itemId, station),
                };
            }
            catch (Exception e)
            {
                Current = null;
                Plugin.Log.LogWarning($"Separate pieces: the drag check failed, the game's rule applies: {e}");
            }
        }

        private static void Postfix() => Current = null;
    }

    // The merge of a drag to the cooking station: none on a free cell, and only into the item under the drop.
    [HarmonyPatch(typeof(ItemManager), "TryMergeIntoOwner")]
    internal static class PiecesOnTryMergeIntoOwner
    {
        private static bool inner;

        private static bool Prefix(ItemManager __instance, long srcInstanceId, long targetOwnerId, ref bool __result)
        {
            var drag = PiecesOnCookingDragMove.Current;
            if (inner || drag == null || targetOwnerId != drag.To) return true;
            try
            {
                if (drag.Target == null)
                {
                    __result = false;
                }
                else
                {
                    // Each other item of the same kind looks full to the game's merge for this one call.
                    var hidden = new List<(ItemData Item, int UseTimes)>();
                    var src = __instance.GetItemData(srcInstanceId);
                    var items = __instance.GetItemDataList(targetOwnerId);
                    for (int i = 0; src != null && items != null && i < items.Count; i++)
                    {
                        var it = items[i];
                        if (it.InstanceId == drag.Target.Value || it.InstanceId == srcInstanceId || it.ItemConfigId != src.ItemConfigId) continue;
                        hidden.Add((it, it.UseTimes));
                    }
                    inner = true;
                    try
                    {
                        foreach (var h in hidden) h.Item.UseTimes = int.MaxValue;
                        __result = __instance.TryMergeIntoOwner(srcInstanceId, targetOwnerId);
                    }
                    finally
                    {
                        foreach (var h in hidden) h.Item.UseTimes = h.UseTimes;
                        inner = false;
                    }
                    drag.Merged = __result;
                }
                if (Plugin.Verbose.Value)
                    Plugin.Log.LogDebug($"pieces drag item={drag.ItemId} to={drag.To} cell={drag.X},{drag.Y} target={(drag.Target.HasValue ? drag.Target.Value.ToString() : "-")} merge={(__result ? "true" : "false")}");
                return false;
            }
            catch (Exception e)
            {
                inner = false;
                PiecesOnCookingDragMove.Current = null;
                Plugin.Log.LogWarning($"Separate pieces: the merge failed, the game's rule applies: {e}");
                return true;
            }
        }
    }

    // A piece that the item under the drop did not take goes to a free cell of the cooking station, not onto that item.
    [HarmonyPatch(typeof(ItemManager), "OnMoveItem")]
    internal static class PiecesOnMoveItem
    {
        private static bool Prefix(ItemManager __instance, long fromOwnerId, long itemId, long toOwnerId)
        {
            var drag = PiecesOnCookingDragMove.Current;
            if (drag == null || drag.Target == null || drag.Merged || toOwnerId != drag.To) return true;
            try
            {
                bool moved = __instance.TryQuickMoveItem(itemId, fromOwnerId, toOwnerId, false, false);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"pieces quick move item={itemId} moved={(moved ? "true" : "false")}");
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Separate pieces: the move to a free cell failed: {e}");
                return true;
            }
        }
    }
}
