using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using UnityEngine;

namespace ProjectCook
{
    // The items of the places of the fill for Load from all tabs: the cooking station, the open container tab, and the
    // other container tabs that are not locked, in the order of LoadAllLogic.Owners. The items come from the bag mirror
    // of the web UI (State_Data_Item, one GetItemDataList for each owner), never from ItemManager.GetAllAvailableItems.
    // One build is kept for the frame and the places until the next refresh of the Recipe Book, so the evaluations of
    // one refresh share it, and the searches of a click share one build.
    internal static class TabIndex
    {
        internal sealed class Index
        {
            public long Station, OpenTab;
            public List<long> Owners;
            public List<LoadAllLogic.Item> Items;
            // The items grouped by config id and by tag for the searches, built once with the index.
            public LoadAllLogic.Pool Pool;
            public int Frame;
        }

        // The container tabs of the cooking window of the last refresh of its Recipe Book, and its cooking station.
        private static readonly List<LoadAllLogic.Tab> tabs = new List<LoadAllLogic.Tab>();
        private static long tabsStation;
        private static int tabsVersion, indexTabsVersion = -1;
        private static Index current;

        // The cookable rule, the tag, and the tier of each config id: config data, read once.
        private struct ConfigInfo
        {
            public bool Cookable;
            public int SubCategory, Tier;
        }

        private static readonly Dictionary<int, ConfigInfo> configs = new Dictionary<int, ConfigInfo>();

        // Keeps the container tabs of the open cooking window (state.BagTabs, in the order of the tab row).
        internal static void SetTabs(State_Web_Cooking state)
        {
            long station = state.WorkbenchOwnerId?.Value ?? 0;
            var list = state.BagTabs;
            var next = new List<LoadAllLogic.Tab>();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                var t = list[i];
                if (t?.OwnerId == null) continue;
                next.Add(new LoadAllLogic.Tab(t.OwnerId.Value, t.Locked != null && t.Locked.Value));
            }
            if (station == tabsStation && SameTabs(next)) return;
            tabs.Clear();
            tabs.AddRange(next);
            tabsStation = station;
            tabsVersion++;
        }

        private static bool SameTabs(List<LoadAllLogic.Tab> next)
        {
            if (next.Count != tabs.Count) return false;
            for (int i = 0; i < next.Count; i++)
                if (next[i].Owner != tabs[i].Owner || next[i].Locked != tabs[i].Locked) return false;
            return true;
        }

        // A container tab of the open cooking window that the fill can take from: not locked, not the station.
        internal static bool IsUsableTab(long station, long owner)
        {
            if (station != tabsStation || owner == station) return false;
            foreach (var t in tabs) if (t.Owner == owner) return !t.Locked;
            return false;
        }

        // The cooking station of the last refresh of the Recipe Book.
        internal static long Station => tabsStation;

        // The items of the last build, for the timing line of a refresh.
        internal static int ItemCount => current?.Items.Count ?? 0;

        // Drops the build at the start of each refresh of the Recipe Book: the moves of a click run their refreshes in
        // the same frame, and a build from before a move holds the items at their old owners.
        internal static void Invalidate() => current = null;

        // The index of these places in this frame, or null when the tabs of the window are not known (no refresh of
        // the Recipe Book of this cooking station yet).
        internal static Index Get(State_Data_Item itemState, long station, long openTab)
        {
            if (itemState == null || station == 0 || openTab == 0 || station != tabsStation) return null;
            int frame = Time.frameCount;
            var index = current;
            if (index != null && index.Frame == frame && index.Station == station && index.OpenTab == openTab && indexTabsVersion == tabsVersion)
                return index;
            long start = Timing.Start();
            index = new Index
            {
                Station = station, OpenTab = openTab, Frame = frame,
                Owners = LoadAllLogic.Owners(station, openTab, tabs), Items = new List<LoadAllLogic.Item>(),
            };
            var bagCache = itemState.BagCache;
            var cache = itemState.Cache;
            foreach (long owner in index.Owners)
            {
                // GetItemDataList of an owner that is not in the mirror: GetBagData of the game throws for it.
                if (bagCache == null || !bagCache.ContainsKey(owner)) continue;
                var list = State_Extensions_Item.GetItemDataList(itemState, owner);
                for (int i = 0; list != null && i < list.Count; i++)
                {
                    var d = list[i];
                    if (d == null || cache == null || !cache.ContainsKey(d.LogicId)) continue;
                    var info = Config(d.ItemConfigId);
                    index.Items.Add(new LoadAllLogic.Item(d.LogicId, owner, d.ItemConfigId, info.Cookable, info.SubCategory, info.Tier));
                }
            }
            index.Pool = new LoadAllLogic.Pool(index.Items);
            current = index;
            indexTabsVersion = tabsVersion;
            if (Plugin.Verbose.Value)
                Plugin.Log.LogDebug(FormattableString.Invariant($"timing: load index {Timing.Ms(start):0.00} ms, owners {index.Owners.Count}, items {index.Items.Count}"));
            return index;
        }

        private static ConfigInfo Config(int configId)
        {
            if (configs.TryGetValue(configId, out var info)) return info;
            var cfg = ConfigManager.Instance.Get_Config_Item(configId);
            info = cfg == null ? new ConfigInfo() : new ConfigInfo
            {
                Cookable = LoadAllLogic.IsCookable(cfg.Category, cfg.CanCook, cfg.CutProductId),
                SubCategory = cfg.SubCategory,
                Tier = Reducer_Web_Cooking.ResolveIngredientTier(configId),
            };
            configs[configId] = info;
            return info;
        }
    }
}
