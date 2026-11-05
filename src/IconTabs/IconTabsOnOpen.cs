using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using SlShared.IconTabs;

namespace ProjectCook
{
    // The tab list of a food window as its page reducer holds it after the open. Reducer_Data_Item.RA_Get<Window>BagList
    // drops each fridge whose bag is not in State_Data_Item.BagCache and then sends the open of the page reducer, so
    // this list is the list that the page shows. Each open gets an open id, and with IconTabs on, the list with the icon
    // of each tab goes to the page script (IconTabsSend), which applies it to the frame of the window.
    internal static class IconTabsOnOpen
    {
        private static int openId;

        // The text key of the lock pop text of a locked fridge tab in the Brewing Barrel and Ration Press windows
        // (GameKey.Text_Cooking_FridgeLockLevel in Reducer_Web_Brew.ApplySwitchBag and
        // Reducer_Web_Compressor.ApplySwitchTab); the cooking window keeps the same text on each locked tab.
        private const string FridgeLockKey = "Cooking_FridgeLockLevel";

        // The text key of the short bag title of the workbench window ("Inventory"; GameKey.Text_ToolTable_0, the
        // bagTitle of WebUI_ToolTable), the text of the bag tab. An empty text gives the briefcase.
        private const string BagTitleKey = "ToolTable_0";

        // The lock text of a window whose tabs keep none, or null when the game has no text for the key.
        internal static string WindowLockText()
        {
            var text = ConstantTextTools.ToConstantTextOrEmpty(FridgeLockKey);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        // window: the name of the Verbose lines; page: the page id of the frame. lockText: the lock text of the window
        // for a locked tab that keeps none itself.
        internal static void Open(string window, string page, List<IconTabsLogic.Tab> tabs, string lockText)
        {
            if (Plugin.Verbose.Value)
            {
                var parts = new List<string>();
                foreach (var t in tabs) parts.Add($"{t.OwnerId} {t.Name}{(t.Locked ? " locked" : "")}");
                Plugin.Log.LogDebug($"{window} tabs: {string.Join(", ", parts)}");
            }
            if (!Plugin.IconTabs.Value) return;

            var agents = BaseSingleton<BattleLogicWorld>.Instance?._AgentManager;
            int language = ModTexts.Language();
            var bagText = ConstantTextTools.ToConstantTextOrEmpty(BagTitleKey) ?? "";
            var entries = IconTabsLogic.Entries(tabs, lockText);
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Bag)
                {
                    entries[i] = e.With(null, bagText);
                    continue;
                }
                var configId = agents?.GetAgent(e.OwnerId)?.TryCast<Furniture>()?.AgentConfigId ?? 0;
                var icon = FurnitureIcons.For(configId, e.Name, language);
                entries[i] = e.With(icon.Icon, icon.Text);
            }
            var id = ++openId;
            IconTabsSending.Request(PageJson.IconTabsJson(page, id, entries));
            if (Plugin.Verbose.Value)
            {
                var parts = new List<string>();
                foreach (var e in entries)
                    parts.Add($"{e.Name}={(e.Bag ? "bag:" + e.Text : !string.IsNullOrEmpty(e.Icon) ? e.Icon.Substring(e.Icon.LastIndexOf('/') + 1) : "text:" + e.Text)}{(e.Lock != null ? " lock:" + e.Lock : "")}");
                Plugin.Log.LogDebug($"{window} icon tabs open={id}: {string.Join(", ", parts)}");
            }
        }

        internal static void Fail(string window, Exception e)
        {
            if (warned.Add(window)) Plugin.Log.LogWarning($"Project Cook: the icon tabs of the {window} window could not be made, the tabs show their names: {e.Message}");
        }

        private static readonly HashSet<string> warned = new HashSet<string>();
    }

    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RA_Open")]
    internal static class IconTabsOnCookingOpen
    {
        private static void Postfix(State_Web_Cooking __result)
        {
            if (!Plugin.IconTabs.Value && !Plugin.CookingStorages.Value) return;
            try
            {
                var list = __result?.BagTabs;
                if (list == null) return;
                var tabs = new List<IconTabsLogic.Tab>();
                for (var i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    tabs.Add(new IconTabsLogic.Tab(t.OwnerId.Value, t.Name.Value, t.Locked.Value, t.LockText?.Value));
                }
                IconTabsOnOpen.Open("cooking", "Cooking", tabs, null);
            }
            catch (Exception e) { IconTabsOnOpen.Fail("cooking", e); }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Brew), "RA_Open")]
    internal static class IconTabsOnBrewOpen
    {
        private static void Postfix(State_Web_Brew __result)
        {
            if (!Plugin.IconTabs.Value && !Plugin.CookingStorages.Value) return;
            try
            {
                var list = __result?.BagTabs;
                if (list == null) return;
                var tabs = new List<IconTabsLogic.Tab>();
                for (var i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    tabs.Add(new IconTabsLogic.Tab(t.OwnerId.Value, t.Name.Value, t.Locked.Value));
                }
                IconTabsOnOpen.Open("brew", "BrewPanel", tabs, IconTabsOnOpen.WindowLockText());
            }
            catch (Exception e) { IconTabsOnOpen.Fail("brew", e); }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_RatCage), "RA_Open")]
    internal static class IconTabsOnRatCageOpen
    {
        private static void Postfix(State_Web_RatCage __result)
        {
            if (!Plugin.IconTabs.Value && !Plugin.CookingStorages.Value) return;
            try
            {
                var list = __result?.BagTabs;
                if (list == null) return;
                var tabs = new List<IconTabsLogic.Tab>();
                for (var i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    tabs.Add(new IconTabsLogic.Tab(t.OwnerId.Value, t.Name.Value, t.Locked.Value));
                }
                IconTabsOnOpen.Open("ratcage", "RatCage", tabs, null);
            }
            catch (Exception e) { IconTabsOnOpen.Fail("ratcage", e); }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_Compressor), "RA_Open")]
    internal static class IconTabsOnCompressorOpen
    {
        private static void Postfix(State_Web_Compressor __result)
        {
            if (!Plugin.IconTabs.Value && !Plugin.CookingStorages.Value) return;
            try
            {
                var list = __result?.CartTabs;
                if (list == null) return;
                var tabs = new List<IconTabsLogic.Tab>();
                for (var i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    tabs.Add(new IconTabsLogic.Tab(t.OwnerId.Value, t.Name.Value, t.Locked.Value));
                }
                IconTabsOnOpen.Open("compressor", "Compressor", tabs, IconTabsOnOpen.WindowLockText());
            }
            catch (Exception e) { IconTabsOnOpen.Fail("compressor", e); }
        }
    }
}
