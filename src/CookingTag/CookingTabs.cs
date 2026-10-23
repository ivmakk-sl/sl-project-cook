using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // Adds a container tab to each food window for each cooking storage: a home storage with the cooking tag or the
    // tag Food that is not a fridge. AE_Open<Window>Panel puts the fridges of the home (AgentManager.GetFurnituresWithBag(
    // home, true, true, false)) into Fridges of the action, and RA_Get<Window>BagList turns that list into the tabs, their
    // names, their lock, and the switch. So a cooking storage that joins the list works as a fridge tab, with no frost.
    [HarmonyPatch(typeof(Reducer_Data_Item), "RA_GetCookingBagList")]
    internal static class CookingTabs
    {
        private static bool warned;

        private static void Prefix(Ac_Item_GetCookingBagList ac) =>
            Append("cooking", ac?.Fridges, null, CookingTagLogic.LockRule.LockBelowLevel2);

        // The one body of the food windows. cabinets are the tool cabinets that the Ration Press lists after the fridges.
        internal static void Append(string window, Il2CppSystem.Collections.Generic.List<CookingFridgeInfo> fridges,
            Il2CppSystem.Collections.Generic.List<ToolCabinetInfo> cabinets, CookingTagLogic.LockRule rule)
        {
            try
            {
                if (fridges == null) return;
                var agents = BaseSingleton<BattleLogicWorld>.Instance?._AgentManager;
                if (agents == null) return;

                var listed = new List<long>();
                for (var i = 0; i < fridges.Count; i++) listed.Add(fridges[i].OwnerId);
                for (var i = 0; cabinets != null && i < cabinets.Count; i++) listed.Add(cabinets[i].OwnerId);

                var tagged = new List<CookingTagLogic.Storage>();
                var storages = agents.GetFurnituresWithBag(agents.GetHomeMapId(), true, true, false);
                for (var i = 0; storages != null && i < storages.Count; i++)
                {
                    var f = storages[i];
                    var tags = f == null ? null : AgentTools.GetAgentComponent<FurnitureTagComponent>(f);
                    if (LinksToCooking(tags?.TagIds))
                        tagged.Add(new CookingTagLogic.Storage(f.InstanceId, f.AgentConfigId));
                }
                if (tagged.Count == 0) return;

                var record = AgentTools.GetAgentComponent<CookingRecordComponent>(agents.GetLeadingRole());
                var level = record?.CookingLevel ?? 0;
                foreach (var tab in CookingTagLogic.ExtraTabs(listed, tagged, level, rule))
                {
                    fridges.Add(new CookingFridgeInfo { OwnerId = tab.OwnerId, FurnitureConfigId = tab.ConfigId, Locked = tab.Locked });
                    if (Plugin.Verbose.Value)
                    {
                        var name = ConfigManager.Instance?.Get_Config_Furniture(tab.ConfigId)?.Name;
                        Plugin.Log.LogDebug($"{window} tab added {tab.OwnerId} {(name == null ? tab.ConfigId.ToString() : ConfigManager.Instance.GetLocalTxt(name))} locked={tab.Locked}");
                    }
                }
            }
            catch (Exception e)
            {
                if (warned) return;
                warned = true;
                Plugin.Log.LogWarning($"Project Cook: the cooking storage tabs could not be added, the food windows show only the game's tabs: {e.Message}");
            }
        }

        // The tags of a storage as plain ids for CookingTagLogic.LinksToCooking.
        internal static bool LinksToCooking(Il2CppSystem.Collections.Generic.List<int> tagIds)
        {
            if (tagIds == null) return false;
            var ids = new List<int>();
            for (var i = 0; i < tagIds.Count; i++) ids.Add(tagIds[i]);
            return CookingTagLogic.LinksToCooking(ids, TagRow.Disabled);
        }
    }

    // The Brewing Barrel window locks its fridge tabs below cooking level 2 (AE_OpenBrewPanel.CollectFridges).
    [HarmonyPatch(typeof(Reducer_Data_Item), "RA_GetBrewBagList")]
    internal static class CookingTabsOnBrew
    {
        private static void Prefix(Ac_Item_GetBrewBagList ac) =>
            CookingTabs.Append("brew", ac?.Fridges, null, CookingTagLogic.LockRule.LockBelowLevel2);
    }

    // The Rat Cage window (each cage size) never locks its fridge tabs (AE_OpenRatCagePanel.CollectFridges).
    [HarmonyPatch(typeof(Reducer_Data_Item), "RA_GetRatCageBagList")]
    internal static class CookingTabsOnRatCage
    {
        private static void Prefix(Ac_Item_GetRatCageBagList ac) =>
            CookingTabs.Append("ratcage", ac?.Fridges, null, CookingTagLogic.LockRule.NeverLocked);
    }

    // The Ration Press window locks its fridge tabs below cooking level 2 and lists the tool cabinets after them, so a
    // tool cabinet with the tag Food keeps its one tab.
    [HarmonyPatch(typeof(Reducer_Data_Item), "RA_GetCompressorBagList")]
    internal static class CookingTabsOnCompressor
    {
        private static void Prefix(Ac_Item_GetCompressorBagList ac) =>
            CookingTabs.Append("compressor", ac?.Fridges, ac?.Cabinets, CookingTagLogic.LockRule.LockBelowLevel2);
    }
}
