using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // Load from all tabs: the game's fill of a Recipe Book row (Reducer_Web_Cooking.TryPlanRecipeFill) searches an exact
    // recipe over all unlocked container tabs (game 1.1.20719: GetExactFillOwners), but a tag recipe only over the
    // cooking station and the open container tab. When the search of a tag recipe fails, these patches run the same
    // search over the other container tabs too, and add the items of the other tabs to the counts by tag of the Recipe
    // Book (the game counts them by config id itself). The game moves each found item from its own tab and returns it
    // there at a recipe switch (RA_RecipeClick, MoveFillItemToBench with the item's owner as the source, seen in game
    // 1.1.20719). The patches are one group (Plugin.PatchGroup): all or none.
    internal static class LoadAllPatches
    {
        internal static readonly Type[] Types =
        {
            typeof(LoadAllOnSelectByTagTiered),
            typeof(LoadAllOnRefreshRecipeList),
            typeof(LoadAllOnEvaluateRecipeForBook),
        };

        // The refresh of the Recipe Book that runs now: its first evaluation adds the counts.
        internal static readonly RefreshMark Refresh = new RefreshMark();

        // The searches of the refresh that runs now, for one summary line, and true while it runs; ModMs is the time of
        // the patches of the feature in the refresh, apart from the game's own work.
        internal static bool InRefresh;
        internal static int RefreshSearches, RefreshFound;
        internal static double ModMs;

        internal static void AddTime(long start)
        {
            if (InRefresh) ModMs += Timing.Ms(start);
        }

        private static readonly HashSet<string> warned = new HashSet<string>();

        // A failure can repeat at each refresh, so each distinct text logs once; the game's own result stays.
        internal static void Fail(string part, Exception e)
        {
            if (warned.Add(part + e.Message)) Plugin.Log.LogWarning($"Load from all tabs: {part} failed, the game's fill stays: {e}");
        }

        internal static Il2CppDict.List<long> ToGame(List<long> ids)
        {
            var list = new Il2CppDict.List<long>();
            foreach (long id in ids) list.Add(id);
            return list;
        }

        // The Verbose line of a search over all tabs: at a click each one, in a refresh one summary.
        internal static void LogSearch(string kind, bool found, TabIndex.Index index)
        {
            if (InRefresh)
            {
                RefreshSearches++;
                if (found) RefreshFound++;
                return;
            }
            if (Plugin.Verbose.Value)
                Plugin.Log.LogDebug($"load: search {kind} game=fail all={(found ? "ok" : "fail")} owners={index.Owners.Count} items={index.Items.Count}");
        }
    }

    // The search of a tag recipe, with its tier passes and the config ids that a dish clash excludes.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "SelectItemsByTagTiered")]
    internal static class LoadAllOnSelectByTagTiered
    {
        private static void Postfix(State_Data_Item itemState, long bagOwnerId, long workbenchOwnerId, Il2CppDict.List<int> tagCombo, int requiredTier, Il2CppDict.HashSet<int> excludeConfigIds, ref Il2CppDict.List<long> __result)
        {
            if (!Plugin.LoadFromAllTabs.Value) return;
            if (__result != null || tagCombo == null) return;
            long start = Timing.Start();
            try
            {
                var index = TabIndex.Get(itemState, workbenchOwnerId, bagOwnerId);
                if (index == null || index.Owners.Count <= 2) return;
                var tags = new List<int>();
                for (int i = 0; i < tagCombo.Count; i++) tags.Add(tagCombo[i]);
                HashSet<int> exclude = null;
                if (excludeConfigIds != null)
                {
                    exclude = new HashSet<int>();
                    var e = excludeConfigIds.GetEnumerator();
                    while (e.MoveNext()) exclude.Add(e.Current);
                }
                var ids = LoadAllLogic.SelectByTagTiered(index.Pool, LoadAllLogic.Needed(tags), requiredTier, exclude);
                if (ids != null) __result = LoadAllPatches.ToGame(ids);
                LoadAllPatches.LogSearch("tag", ids != null, index);
            }
            catch (Exception e) { LoadAllPatches.Fail("the search of a tag recipe", e); }
            finally { LoadAllPatches.AddTime(start); }
        }
    }

    // The start of each refresh of the Recipe Book: the tabs of the window, and the mark of the counts.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshRecipeList")]
    internal static class LoadAllOnRefreshRecipeList
    {
        private static void Prefix(State_Web_Cooking state)
        {
            if (!Plugin.LoadFromAllTabs.Value) return;
            try
            {
                if (state == null) return;
                TabIndex.SetTabs(state);
                TabIndex.Invalidate();
                LoadAllPatches.Refresh.Mark();
                LoadAllPatches.InRefresh = true;
                LoadAllPatches.RefreshSearches = LoadAllPatches.RefreshFound = 0;
                LoadAllPatches.ModMs = 0;
            }
            catch (Exception e) { LoadAllPatches.Fail("the tabs of the window", e); }
        }

        private static void Postfix()
        {
            if (!Plugin.LoadFromAllTabs.Value) return;
            if (!LoadAllPatches.InRefresh) return;
            LoadAllPatches.InRefresh = false;
            if (Plugin.Verbose.Value)
                Plugin.Log.LogDebug(FormattableString.Invariant($"load: refresh searches over all tabs={LoadAllPatches.RefreshSearches} found={LoadAllPatches.RefreshFound} mod={LoadAllPatches.ModMs:0.00} ms"));
        }
    }

    // The counts of the Recipe Book: the game counts the station and the open tab into totalCountById and totalTags,
    // and the other unlocked tabs into totalCountById only (AddOtherTabsToExactCounts, game 1.1.20719), before the
    // first evaluation; each evaluation of the refresh reads the same dictionaries, so the first one adds the other
    // tabs to totalTags. The return value (the private struct RecipeBookEval) is not touched.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "EvaluateRecipeForBook")]
    internal static class LoadAllOnEvaluateRecipeForBook
    {
        private static void Prefix(State_Web_Cooking state, State_Data_Item itemState, Il2CppDict.Dictionary<int, int> totalCountById, Il2CppDict.Dictionary<int, int> totalTags)
        {
            if (!Plugin.LoadFromAllTabs.Value) return;
            try
            {
                if (!LoadAllPatches.Refresh.TakeFirst() || state == null || totalTags == null) return;
                long start = Timing.Start();
                long station = state.WorkbenchOwnerId.Value, openTab = state.BagOwnerId.Value;
                var index = TabIndex.Get(itemState, station, openTab);
                if (index == null) return;
                var tags = new Dictionary<int, int>();
                LoadAllLogic.AddTagCounts(index.Items, station, openTab, tags);
                Add(totalTags, tags);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"load: counts added tags={tags.Count}");
                LoadAllPatches.AddTime(start);
            }
            catch (Exception e) { LoadAllPatches.Fail("the counts of the Recipe Book", e); }
        }

        private static void Add(Il2CppDict.Dictionary<int, int> total, Dictionary<int, int> more)
        {
            foreach (var kv in more) total[kv.Key] = (total.ContainsKey(kv.Key) ? total[kv.Key] : 0) + kv.Value;
        }
    }
}
