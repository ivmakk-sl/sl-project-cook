using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace ProjectCook
{
    // Adds a container tab to the cooking window for each cooking storage: a home storage with the cooking tag or the
    // tag Food that is not a fridge. AE_OpenCookingPanel puts the fridges of the home (AgentManager.GetFurnituresWithBag(home, true,
    // true, false)) into Fridges of this action, and RA_GetCookingBagList turns that list into the tabs, their names,
    // their lock, and the switch. So a cooking storage that joins the list works as a fridge tab, with no frost.
    [HarmonyPatch(typeof(Reducer_Data_Item), "RA_GetCookingBagList")]
    internal static class CookingTabs
    {
        private static bool warned;

        private static void Prefix(Ac_Item_GetCookingBagList ac)
        {
            try
            {
                var fridges = ac?.Fridges;
                if (fridges == null) return;
                var agents = BaseSingleton<BattleLogicWorld>.Instance?._AgentManager;
                if (agents == null) return;

                var fridgeOwners = new List<long>();
                for (var i = 0; i < fridges.Count; i++) fridgeOwners.Add(fridges[i].OwnerId);

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
                foreach (var tab in CookingTagLogic.ExtraTabs(fridgeOwners, tagged, level))
                {
                    fridges.Add(new CookingFridgeInfo { OwnerId = tab.OwnerId, FurnitureConfigId = tab.ConfigId, Locked = tab.Locked });
                    if (Plugin.Verbose.Value)
                    {
                        var name = ConfigManager.Instance?.Get_Config_Furniture(tab.ConfigId)?.Name;
                        Plugin.Log.LogDebug($"cooking tab added {tab.OwnerId} {(name == null ? tab.ConfigId.ToString() : ConfigManager.Instance.GetLocalTxt(name))} locked={tab.Locked}");
                    }
                }
            }
            catch (Exception e)
            {
                if (warned) return;
                warned = true;
                Plugin.Log.LogWarning($"Project Cook: the cooking storage tabs could not be added, the cooking window shows only the game's tabs: {e.Message}");
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
}
