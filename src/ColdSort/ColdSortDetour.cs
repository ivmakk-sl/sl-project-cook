using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppItemList = Il2CppSystem.Collections.Generic.List<GameCore.HotUpdate.Battle.Logic.ItemData>;

namespace ProjectCook
{
    // Adds the targets of ColdSortLogic to the game's [Store Chilled Food] button. The count of the button, the plan of
    // a run, each stop of a run, and the text of the leftovers all rank each storage for each item through the
    // private static ItemManager.TryRankQuickStoreTarget, so one hook serves them all. The game keeps each storage
    // that it ranks itself: the hook acts only when the original gives false for QuickStoreKind.Cold.
    // The count and each stop collect the food of the backpack (CollectQuickStoreItems). While the character stands in
    // the Cold Storage room, the food in the backpack rots slower there, so the game counts none of it as food that
    // needs cold: no button, and a stop in the room puts nothing. So in the room the food that needs cold at the normal
    // speed joins the list.
    // Native detours, not Harmony: a Harmony patch of a private ItemManager method that gives a value gave the caller
    // the default value, and TryRankQuickStoreTarget has two out arguments.
    internal static class ColdSortDetour
    {
        // The native signatures of il2cpp methods: the arguments (an instance method: this first), then the MethodInfo.
        // An enum is its int, an out int a pointer, an object a pointer. il2cpp gives a C# bool in the low byte only, so
        // a bool result is a byte.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte TryRankFn(int kind, IntPtr furniture, IntPtr cfg, float remainDays, IntPtr level, IntPtr specificity, IntPtr methodInfo);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CollectItemsFn(IntPtr self, int kind, long roleId, float nowHours, IntPtr methodInfo);

        // Static, so the garbage collector keeps the delegates while the detours run.
        private static TryRankFn rankHook, rankOriginal;
        private static CollectItemsFn itemsHook, itemsOriginal;
        private static readonly List<INativeDetour> detours = new List<INativeDetour>();

        // Config_Item.Category of food (the Cold items of CollectQuickStoreItems).
        private const int FoodCategory = 1;

        private static bool warned;
        // For the Verbose lines: each storage and group that ranked, and each item that joined the list in the room, so
        // a line comes once.
        private static readonly HashSet<(long, ColdSortLogic.StoreGroup)> logged = new HashSet<(long, ColdSortLogic.StoreGroup)>();
        private static readonly HashSet<long> loggedItems = new HashSet<long>();

        internal static void Apply()
        {
            var rank = Code("TryRankQuickStoreTarget", true, typeof(bool),
                typeof(QuickStoreKind), typeof(Furniture), typeof(Config_Item), typeof(float), typeof(int).MakeByRefType(), typeof(int).MakeByRefType());
            var items = Code("CollectQuickStoreItems", false, typeof(Il2CppItemList),
                typeof(QuickStoreKind), typeof(long), typeof(float));
            rankHook = Rank;
            itemsHook = CollectItems;
            detours.Add(INativeDetour.CreateAndApply(rank, rankHook, out rankOriginal));
            detours.Add(INativeDetour.CreateAndApply(items, itemsHook, out itemsOriginal));
        }

        internal static void Undo()
        {
            foreach (var d in detours) d?.Dispose();
            detours.Clear();
        }

        // The address of the native code of a method of ItemManager, after a check of its signature: the hooks read
        // and write through the pointers of the native signature, so a game update that changes the method must stop
        // the patch group instead of writing to the wrong place.
        private static IntPtr Code(string name, bool isStatic, Type result, params Type[] parameters)
        {
            var method = AccessTools.Method(typeof(ItemManager), name) ?? throw new MissingMethodException(nameof(ItemManager), name);
            var ps = method.GetParameters();
            var ok = method.IsStatic == isStatic && method.ReturnType == result && ps.Length == parameters.Length;
            for (var i = 0; ok && i < ps.Length; i++) ok = ps[i].ParameterType == parameters[i];
            if (!ok) throw new MissingMethodException(nameof(ItemManager), name + " (the signature changed)");
            var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method)
                ?? throw new MissingMethodException(nameof(ItemManager), name + " (no method info)");
            // The first field of an il2cpp MethodInfo is the address of the native code.
            var code = Marshal.ReadIntPtr((IntPtr)field.GetValue(null));
            if (code == IntPtr.Zero) throw new MissingMethodException(nameof(ItemManager), name + " (no code)");
            return code;
        }

        private static void Warn(Exception e)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning($"Project Cook: Store Chilled Food for a Food storage failed: {e.Message}");
        }

        private static byte Rank(int kind, IntPtr furniture, IntPtr cfg, float remainDays, IntPtr level, IntPtr specificity, IntPtr methodInfo)
        {
            // The switch first: off, the game's own rank as it is.
            if (!Plugin.ColdSortFood.Value) return rankOriginal(kind, furniture, cfg, remainDays, level, specificity, methodInfo);
            var result = rankOriginal(kind, furniture, cfg, remainDays, level, specificity, methodInfo);
            if (result != 0 || kind != (int)QuickStoreKind.Cold || furniture == IntPtr.Zero || cfg == IntPtr.Zero
                || level == IntPtr.Zero || specificity == IntPtr.Zero)
                return result;
            try
            {
                if (!TryRank(new Furniture(furniture), new Config_Item(cfg), remainDays, out var l, out var s)) return result;
                Marshal.WriteInt32(level, l);
                Marshal.WriteInt32(specificity, s);
                return 1;
            }
            catch (Exception e)
            {
                Warn(e);
                return result;
            }
        }

        private static IntPtr CollectItems(IntPtr self, int kind, long roleId, float nowHours, IntPtr methodInfo)
        {
            if (!Plugin.ColdSortFood.Value) return itemsOriginal(self, kind, roleId, nowHours, methodInfo);
            var result = itemsOriginal(self, kind, roleId, nowHours, methodInfo);
            if (kind != (int)QuickStoreKind.Cold || result == IntPtr.Zero) return result;
            try
            {
                if (EnvAreaHelper.GetPlayerAreaId(out _) != EnvAreaHelper.ColdStorageAreaId) return result;
                var list = new Il2CppItemList(result);
                var have = new HashSet<long>();
                for (var i = 0; i < list.Count; i++) if (list[i] != null) have.Add(list[i].InstanceId);
                // With no action manager (a load or a transition) the game's filter cannot run, so the list stays as it is.
                var actions = BaseSingleton<BattleLogicWorld>.Instance?._ActionManager;
                if (actions == null) return result;
                var bag = new ItemManager(self).GetItemDataList(roleId);
                var config = ConfigManager.Instance;
                // The filter of the game (CollectQuickStoreItems): food with a shelf life that needs cold and that no
                // action of the character targets; only the days count at the normal speed.
                for (var i = 0; bag != null && i < bag.Count; i++)
                {
                    var item = bag[i];
                    if (item == null || have.Contains(item.InstanceId)) continue;
                    var cfg = config?.Get_Config_Item(item.ItemConfigId);
                    if (cfg == null || cfg.Life < 1 || cfg.Category != FoodCategory) continue;
                    var days = FoodPreservationRules.GetRemainingDays(item.StartTime, item.TimeLeft, item.PresetShelfScale, nowHours);
                    if (!FurnitureTagRules.IsNeedsColdDays(cfg, ColdSortLogic.NormalSpeedDays(days, item.TimeScale))) continue;
                    if (actions.HasActionWithTarget(roleId, item.InstanceId)) continue;
                    list.Add(item);
                    if (Plugin.Verbose.Value && loggedItems.Add(item.InstanceId))
                        Plugin.Log.LogDebug($"cold sort in the Cold Storage room: {config.GetLocalTxt(cfg.ItemName)} {item.InstanceId} needs cold at the normal speed ({days:0.#} days here, scale {item.TimeScale:0.###})");
                }
            }
            catch (Exception e) { Warn(e); }
            return result;
        }

        // The mod check, cheapest first: the tag rule, then the fridge test or the room, then the game's mold block and
        // the game's tag match of the item (through the tag patches, so a rule with only the cooking tag is Food).
        private static bool TryRank(Furniture f, Config_Item item, float remainDays, out int level, out int specificity)
        {
            level = 0;
            specificity = 0;
            var tags = AgentTools.GetAgentComponent<FurnitureTagComponent>(f);
            var raw = tags?.TagIds;
            if (raw == null || raw.Count == 0) return false;
            var ids = new List<int>(raw.Count);
            for (var i = 0; i < raw.Count; i++) ids.Add(raw[i]);
            // With the cooking storages off, the tag patches do not run, so a leftover cooking tag id counts for nothing.
            var disabled = TagRow.Disabled || !Plugin.CookingStorages.Value;
            if (!CookingTagLogic.LinksToCooking(ids, disabled)) return false;

            var fc = ConfigManager.Instance?.Get_Config_Furniture(f.AgentConfigId);
            if (fc == null) return false;
            var fridge = ColdSortLogic.IsFridge(fc.IsElectrical, fc.ElectricalType, fc.ColdRate, fc.PowerCost);
            bool inRoom = false, roomCold = false;
            if (!fridge)
            {
                var area = EnvAreaHelper.GetFurnitureAreaId(f);
                inRoom = area == EnvAreaHelper.ColdStorageAreaId;
                roomCold = inRoom && (EnvAreaHelper.IsColdStorageSealed(area) || EnvAreaHelper.GetFoodDecayScale(area) < 1f);
            }
            var group = ColdSortLogic.GroupOf(new ColdSortLogic.Facts(fridge, inRoom, roomCold, ids, disabled));
            if (group == ColdSortLogic.StoreGroup.None) return false;

            if (RobotSortingManager.IsMoldBlockedTarget(f.InstanceId, item)) return false;
            if (!tags.TryGetPutRank(f.AgentConfigId, item, remainDays, out var tagLevel, out specificity)) return false;
            level = ColdSortLogic.Level(group, tagLevel);
            if (Plugin.Verbose.Value && logged.Add((f.InstanceId, group)))
                Plugin.Log.LogDebug($"cold sort {ConfigManager.Instance.GetLocalTxt(fc.Name)} {f.InstanceId}: group {group}, level {level}, specificity {specificity}");
            return true;
        }
    }
}
