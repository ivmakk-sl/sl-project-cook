using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // Reads the items of an open fridge or cooking storage into the numbers of the food sort (SortLogic), and hands
    // the JSON to the send schedule. The schedule sends it only when it differs from the last one.
    internal static class SortData
    {
        private static HashSet<int> perfectIds;
        private static object perfectIdsSource;
        private static bool warned;
        private static readonly HashSet<string> loggedEat = new HashSet<string>();
        // The last container tab of the cooking window and the last items of its workbench.
        private static long cookingTab;
        private static List<KeyValuePair<long, SortLogic.Numbers>> cookingTabNumbers, workbenchNumbers;

        // A fridge (the test of AE_OpenCookingPanel), a storage with the cooking tag or Food, or, with cookingBag (the
        // cooking window), the bag of the leading role.
        public static bool IsSortable(long ownerId, bool cookingBag)
        {
            var agents = BaseSingleton<BattleLogicWorld>.Instance?._AgentManager;
            if (agents == null) return false;
            var role = cookingBag ? agents.GetLeadingRole() : null;
            if (role != null && role.InstanceId == ownerId) return SortLogic.IsSortable(false, false, true);
            var furniture = agents.GetAgent(ownerId)?.TryCast<Furniture>();
            if (furniture == null) return false;
            var cfg = ConfigManager.Instance.Get_Config_Furniture(furniture.AgentConfigId);
            bool isFridge = cfg != null && cfg.IsElectrical && cfg.ElectricalType == 1 && cfg.ColdRate > 0;
            var tags = AgentTools.GetAgentComponent<FurnitureTagComponent>(furniture)?.TagIds;
            return SortLogic.IsSortable(isFridge, Plugin.CookingStorages.Value && CookingTabs.LinksToCooking(tags), false);
        }

        // The cooking window: reads the items of one grid and requests a send. The window has two grids: the
        // container tab (the bag, a fridge, or a cooking storage), and the workbench, the grid whose owner is not
        // sortable (the cooking furniture). The send holds the numbers of both, with the owner of the tab.
        public static void RequestCooking(long ownerId, Il2CppDict.List<Data_Item> items, float currentHours)
        {
            try
            {
                if (items == null) return;
                bool workbench = !IsSortable(ownerId, true);
                var numbers = Numbers(items, currentHours, out string eatLog);
                List<KeyValuePair<long, SortLogic.Numbers>> send;
                if (workbench)
                {
                    // Kept also with no tab numbers yet (an empty tab at the open), so the next tab send holds them.
                    workbenchNumbers = numbers;
                    if (cookingTabNumbers == null) return;
                    send = SortLogic.Merge(cookingTabNumbers, workbenchNumbers);
                }
                else
                {
                    cookingTab = ownerId;
                    cookingTabNumbers = numbers;
                    send = SortLogic.Merge(numbers, workbenchNumbers);
                }
                PageTick.RequestSort(PageJson.SortDataJson(cookingTab, send, Words()));
                Log($"cooking{(workbench ? " workbench" : "")} {ownerId} items={numbers.Count} sent={send.Count}", eatLog);
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        // The storage window: with a sortable storage open, the numbers of its items, and in a window of two grids
        // also of the items of the Backpack side, which shows the same sort. RA_OpenUI puts the first bag of the window
        // on side A: a window of two grids has the Backpack on A and the storage on B, and a storage opened alone (the
        // Use of a storage) is on A, while B keeps the storage of an earlier window. The send names the storage and,
        // with two grids, the Backpack.
        public static void RequestStorage(State_Web_BackpackUI state, float currentHours)
        {
            try
            {
                if (state == null) return;
                bool dual = state.IsDualBag;
                long owner = dual ? state.OwnerId_B : state.OwnerId_A;
                var items = dual ? state.ItemDataList_B : state.ItemDataList_A;
                if (items == null || !IsSortable(owner, false)) return;
                cookingTabNumbers = null;
                workbenchNumbers = null;
                var numbers = Numbers(items, currentHours, out string eatLog);
                long bag = dual ? state.OwnerId_A : 0;
                var send = numbers;
                if (bag != 0 && state.ItemDataList_A != null)
                    send = SortLogic.Merge(numbers, Numbers(state.ItemDataList_A, currentHours, out _));
                else bag = 0;
                PageTick.RequestSort(PageJson.SortDataJson(owner, send, Words(), bag));
                Log($"storage {owner}{(dual ? "" : " alone")} bag={bag} items={numbers.Count} sent={send.Count}", eatLog);
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static List<KeyValuePair<long, SortLogic.Numbers>> Numbers(Il2CppDict.List<Data_Item> items, float currentHours, out string eatLog)
        {
            var config = ConfigManager.Instance;
            var factors = EatInputs.Read(out eatLog);
            float appraisal = TalentInputs.ReadAppraisal();
            var perfect = PerfectIds(config);
            var numbers = new List<KeyValuePair<long, SortLogic.Numbers>>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var cfg = item == null ? null : config.Get_Config_Item(item.ItemConfigId);
                if (cfg == null) continue;
                var dish = new EatLogic.Dish
                {
                    Values = OneUse(item, cfg, config, out bool instance),
                    HasInstanceValues = instance,
                    SubCategory = cfg.SubCategory,
                    IsStaple = cfg.Category == 1 && cfg.SubCategory == 1,
                    IsPerfect = perfect.Contains(cfg.ID),
                    Portions = 1,
                };
                var n = SortLogic.ItemNumbers(dish, cfg.Category == 1, factors, cfg.TradeValue,
                    SortLogic.TradeUses(item.UseTimes, item.MaxUseTimes, cfg.UseTimes), item.ItemCount, appraisal,
                    Days(item, cfg, currentHours));
                n.ConfigId = cfg.ID;
                numbers.Add(new KeyValuePair<long, SortLogic.Numbers>(item.LogicId, n));
            }
            return numbers;
        }

        private static void Log(string text, string eatLog)
        {
            if (!Plugin.Verbose.Value) return;
            Plugin.Log.LogDebug($"sort data {text}");
            if (loggedEat.Add(eatLog)) Plugin.Log.LogDebug($"sort eat factors {eatLog}");
        }

        private static void Warn(Exception e)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning($"Project Cook: the numbers of the food sort could not be read: {e.Message}");
        }

        // The values of one use, as the eat path reads them (ItemValueDisplayHelper.GetEffectiveEffectEnd): the
        // instance values of a cooked item, else the Item<Stat>End values of the Effect of its use action.
        private static float[] OneUse(Data_Item item, Config_Item cfg, ConfigManager config, out bool instance)
        {
            var own = item.InstanceEffectEnd;
            instance = own != null && own.Length >= 5;
            if (instance) return new[] { own[0], own[1], own[2], own[3], own[4] };
            var action = cfg.UseAction > 0 ? config.Get_Config_Action(cfg.UseAction) : null;
            var effect = action == null ? null : config.Get_Config_Effect(action.EffectConfigID);
            if (effect == null) return new float[5];
            return new[] { effect.ItemSatietyEnd, effect.ItemMoraleEnd, effect.ItemStaminaEnd, effect.ItemHealthEnd, effect.ItemVitalityEnd };
        }

        // The days of the shelf life line of the storage window (GetShelfLifeText): the days left, or for an expired
        // item the days until it spoils.
        private static SortLogic.DaysLeft Days(Data_Item item, Config_Item cfg, float currentHours) =>
            SortLogic.DaysLeftOf(cfg.Life, cfg.IT_Rot_Threshold, item.StartTime, item.TimeLeft, item.OriginalTimeLeft,
                item.TimeScale, item.PresetShelfScale, currentHours, FoodPreservationRules.IsCompostRotFrozen(cfg, item.TimeScale));

        // The config ids that AE_ItemAddBase5Attr counts as a Perfect dish: the PerfectItemID of each recipe.
        private static HashSet<int> PerfectIds(ConfigManager config)
        {
            var source = config._Config_CookingRecipe_Dict;
            if (perfectIds != null && ReferenceEquals(source, perfectIdsSource)) return perfectIds;
            var ids = new HashSet<int>();
            var e = source.GetEnumerator();
            while (e.MoveNext())
                if (e.Current.Value != null && e.Current.Value.PerfectItemID > 0) ids.Add(e.Current.Value.PerfectItemID);
            perfectIds = ids;
            perfectIdsSource = source;
            return ids;
        }

        private static SortLogic.Words Words()
        {
            var config = ConfigManager.Instance;
            bool chinese = false;
            try { chinese = config?.customCache != null && config.customCache.LanguageType == LanguageType.Chinese; }
            catch (Exception) { }
            return SortLogic.WordsFrom(GameWords.Current(),
                ConstantTextTools.ToConstantTextOrEmpty("SR_Web_EventChoice_DefaultTag"),
                ConstantTextTools.ToConstantTextOrEmpty("UI_ItemTips_2"),
                ConstantTextTools.ToConstantTextOrEmpty("UI_BattleBag_6"), chinese,
                ConstantTextTools.ToConstantTextOrEmpty("SR_Web_Backpack_Rotten"));
        }
    }
}
