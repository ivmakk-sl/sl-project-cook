using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ProjectCook
{
    [BepInPlugin(PluginGuid, "Project Cook", "1.3.0")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.projectcook";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> IgnoreCookingTalents;
        internal static ConfigEntry<bool> DishPreview, FoodSort, CookingStorages, RowSplit, SeparatePieces, DimUncookable;
        // The names of the page features that are on, for the page data (PageJson.DataJson).
        internal static string[] PageFeatures = new string[0];

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
                "The food sort of fridges, cooking storages, and the Backpack, in the storage window, the cooking window, and the Rat Cage window: the dropdown, the numbers on the items, and the sorted view. Needs a game restart.");
            CookingStorages = Config.Bind(
                "Features", "CookingStorages", true,
                "The Cooking tag, and the storages with the Cooking tag or the Food tag as tabs of the cooking window. Off works like an uninstall of this part: the game drops the Cooking tag from each storage on the next save, a storage with only the Cooking tag loses its rule, and turning it on again does not bring the tag back. Needs a game restart.");
            RowSplit = Config.Bind(
                "Features", "RowSplit", true,
                "From cooking level 3, each row of the cooking station is its own group of ingredients: the game matches the dishes of each row alone, and the seasonings still help every dish. With all ingredients in one row, the cook works as without the mod. Needs a game restart.");
            SeparatePieces = Config.Bind(
                "Features", "SeparatePieces", true,
                "A piece of an item with more than one use (for example Wild Rabbit 1/3) that you drop on a free cell of the cooking station stays its own item, so a recipe counts it as its own ingredient. A drop on an item of the same kind merges the piece into that item. Needs a game restart.");
            DimUncookable = Config.Bind(
                "Features", "DimUncookable", true,
                "In each tab of the cooking window, the items that the cooking station does not take are dimmed: an item that is not food, a product such as Beef Slices, or an item that needs cutting first. Fuel items stay bright at a stove that burns fuel. Needs a game restart.");
            var features = new System.Collections.Generic.List<string>();
            if (SeparatePieces.Value) features.Add("separatePieces");
            if (DimUncookable.Value) features.Add("dimUncookable");
            // The portion switch is part of the dish preview.
            if (DishPreview.Value) features.Add("portionSwitch");
            PageFeatures = features.ToArray();
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
            if (RowSplit.Value)
                patches.AddRange(new[] { typeof(RowSplitOnRefreshPrediction), typeof(RowSplitOnStart), typeof(RowSplitOnValidate), typeof(RowSplitOnSettle), typeof(RowSplitOnAdmission) });
            if (SeparatePieces.Value)
                patches.AddRange(new[] { typeof(PiecesOnCookingDragMove), typeof(PiecesOnTryMergeIntoOwner), typeof(PiecesOnMoveItem) });
            if (CookingStorages.Value)
                patches.AddRange(new[] { typeof(TagRow), typeof(TagMatchOnEvaluate), typeof(TagMatchOnTryGetMatchRank), typeof(TagMatchOnTryGetPutRank), typeof(CookingTabs), typeof(HeadBarIcon) });
            if (FoodSort.Value)
                patches.AddRange(new[]
                {
                    typeof(StorageSortOnRefresh), typeof(StorageBagSortOnRefresh), typeof(CookingSortOnRefresh),
                    typeof(CookingSortOnOpen), typeof(CookingSortOnSwitchBag), typeof(CookingSortOnRefreshBag),
                    typeof(RatCageSortOnRefresh), typeof(RatCageSortOnOpen), typeof(RatCageSortOnSwitchBag), typeof(RatCageSortOnRefreshBag),
                    typeof(PageTickOnRatCageShow),
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
            if (!DishPreview.Value || !FoodSort.Value || !CookingStorages.Value || !RowSplit.Value || !SeparatePieces.Value || !DimUncookable.Value)
                Log.LogInfo($"Project Cook features: DishPreview={DishPreview.Value} FoodSort={FoodSort.Value} CookingStorages={CookingStorages.Value} RowSplit={RowSplit.Value} SeparatePieces={SeparatePieces.Value} DimUncookable={DimUncookable.Value}");
            Log.LogInfo("Project Cook loaded.");
        }
    }
}
