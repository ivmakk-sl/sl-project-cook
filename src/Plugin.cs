using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ProjectCook
{
    [BepInPlugin(PluginGuid, "Project Cook", "1.2.0")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.projectcook";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> IgnoreCookingTalents;
        internal static ConfigEntry<bool> DishPreview, FoodSort, CookingStorages;

        public override void Load()
        {
            Log = base.Log;
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Log each preview (quality inputs, chances, stats, eat factors, portions), each cooked result (quality roll and stats), each eat of the character, the numbers of the food sort and the cooking tag, and each send to the page script. Use it to compare the preview with the real result. Keep off in normal play.");
            IgnoreCookingTalents = Config.Bind(
                "Debug", "IgnoreCookingTalents", false,
                "For tests only. The game and the preview read each cooking talent as 0, so dishes of lower quality can occur with a character that has the talents. It changes the real cooking results while it is on. The save does not change. Needs a game restart.");
            DishPreview = Config.Bind(
                "Features", "DishPreview", true,
                "The preview lines and the tooltip of the dish cards, and the added lines and tier marks of the ingredients, in the cooking window. Needs a game restart.");
            FoodSort = Config.Bind(
                "Features", "FoodSort", true,
                "The food sort of fridges, cooking storages, and the Backpack, in the storage window and the cooking window: the dropdown, the numbers on the items, and the sorted view. Needs a game restart.");
            CookingStorages = Config.Bind(
                "Features", "CookingStorages", true,
                "The Cooking tag, and the storages with the Cooking tag or the Food tag as tabs of the cooking window. Off works like an uninstall of this part: the game drops the Cooking tag from each storage on the next save, a storage with only the Cooking tag loses its rule, and turning it on again does not bring the tag back. Needs a game restart.");
            var harmony = new Harmony(PluginGuid);
            var patches = new System.Collections.Generic.List<Type>
            {
                typeof(ResultLogOnRollCookingQuality),
                typeof(ResultLogOnCalcProductVD),
                typeof(ResultLogOnAddCookExp),
                typeof(ResultLogOnEat),
                typeof(PageTick),
                typeof(PageTickOnCallShowAction),
                typeof(PageTickOnStorageShow),
            };
            if (DishPreview.Value) patches.Add(typeof(PreviewOnRefreshPrediction));
            if (CookingStorages.Value)
                patches.AddRange(new[] { typeof(TagRow), typeof(TagMatchOnEvaluate), typeof(TagMatchOnTryGetMatchRank), typeof(TagMatchOnTryGetPutRank), typeof(CookingTabs) });
            if (FoodSort.Value)
                patches.AddRange(new[]
                {
                    typeof(StorageSortOnRefresh), typeof(StorageBagSortOnRefresh), typeof(CookingSortOnRefresh),
                    typeof(CookingSortOnOpen), typeof(CookingSortOnSwitchBag), typeof(CookingSortOnRefreshBag),
                });
            // One patch that fails to attach must not stop the others.
            foreach (var type in patches)
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogError($"patch {type.Name} failed: {e.Message}"); }
            }
            if (IgnoreCookingTalents.Value)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(IgnoreTalentsOnGetTalentEffectRatio)).Patch();
                    Log.LogWarning("Debug option IgnoreCookingTalents is on: the cooking talents of the character have no effect.");
                }
                catch (Exception e) { Log.LogError($"patch {nameof(IgnoreTalentsOnGetTalentEffectRatio)} failed: {e.Message}"); }
            }
            if (!DishPreview.Value || !FoodSort.Value || !CookingStorages.Value)
                Log.LogInfo($"Project Cook features: DishPreview={DishPreview.Value} FoodSort={FoodSort.Value} CookingStorages={CookingStorages.Value}");
            Log.LogInfo("Project Cook loaded.");
        }
    }
}
