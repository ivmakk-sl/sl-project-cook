using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ProjectCook
{
    [BepInPlugin(PluginGuid, "Project Cook", "1.5.0")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.projectcook";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> IgnoreCookingTalents;
        internal static ConfigEntry<bool> DishPreview, FoodSort, CookingStorages, RowSplit, SeparatePieces, DimUncookable, ColdSortFood, IconTabs, CodexMarks, LoadFromAllTabs, UnknownRecipesWithIngredients, UnknownRecipesWithoutIngredients;
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
                "The Cooking tag, and the storages with the Cooking tag or the Food tag as tabs of the cooking window, the Brewing Barrel, the Rat Cage, and the Ration Press. Off works like an uninstall of this part: the game drops the Cooking tag from each storage on the next save, a storage with only the Cooking tag loses its rule, and turning it on again does not bring the tag back. Needs a game restart.");
            RowSplit = Config.Bind(
                "Features", "RowSplit", true,
                "From cooking level 3, each row of the cooking station is its own group of ingredients: the game matches the dishes of each row alone, and the seasonings still help every dish. With all ingredients in one row, the cook works as without the mod. Needs a game restart.");
            SeparatePieces = Config.Bind(
                "Features", "SeparatePieces", true,
                "A piece of an item with more than one use (for example Wild Rabbit 1/3) that you drop on a free cell of the cooking station stays its own item, so a recipe counts it as its own ingredient. A drop on an item of the same kind merges the piece into that item. Needs a game restart.");
            DimUncookable = Config.Bind(
                "Features", "DimUncookable", true,
                "In each tab of the cooking window, the items that the cooking station does not take are dimmed: an item that is not food, a product such as Beef Slices, or an item that needs cutting first. Fuel items stay bright at a stove that burns fuel. Needs a game restart.");
            ColdSortFood = Config.Bind(
                "Features", "ColdSortFood", true,
                "The Store Chilled Food button of the backpack also fills a fridge with the Food tag or the Cooking tag, and last a storage with one of these tags in the Cold Storage room while the room keeps food cold. A storage with the Chilled tag still comes first, and a fridge with no tag comes before the Cold Storage room. In the Cold Storage room, the button counts the food in the backpack by its days at the normal speed. Needs a game restart.");
            IconTabs = Config.Bind(
                "Features", "IconTabs", true,
                "The storage tabs of the cooking window, the Brewing Barrel, the Rat Cage, and the Ration Press show the icon of each storage in place of its name, so more tabs fit in the row. Point at a tab to see its name. Off shows the names as without the mod. Needs a game restart.");
            CodexMarks = Config.Bind(
                "Features", "CodexMarks", true,
                "A trophy on each dish card and each Recipe Book row of the cooking window whose dish is not in the Codex yet, the Codex button in the Recipe Book title that shows only those recipes (a dish that a cooking station cooks now leaves them until its cook ends), and the Codex choice of the food sort: the foods that you have never tasted come first, each with a trophy. Off shows no trophy, no Codex button, and no Codex choice. Needs a game restart.");
            LoadFromAllTabs = Config.Bind(
                "Features", "LoadFromAllTabs", true,
                "Load in the Recipe Book of the cooking window also takes the ingredients from the other tabs of the window (the bag, each fridge, and each cooking storage that is not locked), when the open tab does not have them all. The Recipe Book then counts the ingredients of all these tabs. Off fills only from the open tab, as without the mod. Needs a game restart.");
            UnknownRecipesWithIngredients = Config.Bind(
                "Features", "UnknownRecipesWithIngredients", true,
                "With the Codex button of the Recipe Book on, the list also shows the recipes that you have not learned yet, whose dish is not in the Codex, and whose ingredients the cooking station and the open tab hold (with LoadFromAllTabs, all the storage tabs): each with a grey \"?\" icon, the trophy, the dish name, the ingredients, and Load. A first cook teaches the recipe. Only at a cooking station where you know at least one dish, and not for a recipe that needs a cooking level or an event first. Needs CodexMarks. Needs a game restart.");
            UnknownRecipesWithoutIngredients = Config.Bind(
                "Features", "UnknownRecipesWithoutIngredients", false,
                "With the Codex button of the Recipe Book on, the list also shows the recipes that you have not learned yet whose ingredients you do not have, after the recipes with Load: every recipe that the cooking station can cook. Same rules as UnknownRecipesWithIngredients. Needs CodexMarks. Needs a game restart.");
            bool unknownRecipes = UnknownRecipesWithIngredients.Value || UnknownRecipesWithoutIngredients.Value;
            var features = new System.Collections.Generic.List<string>();
            if (CodexMarks.Value) features.Add("codexMarks");
            if (CodexMarks.Value && unknownRecipes) features.Add("unknownCodexRecipes");
            if (SeparatePieces.Value) features.Add("separatePieces");
            if (DimUncookable.Value) features.Add("dimUncookable");
            // The portion switch is part of the dish preview.
            if (DishPreview.Value) features.Add("portionSwitch");
            PageFeatures = features.ToArray();
            SlShared.ModTags.ModTagIcon.Setup(CookingTagLogic.IconKey, typeof(Plugin).Assembly, "ProjectCook.cook-tag.alpha",
                text => Log.LogWarning("Project Cook: " + text), text => { if (Verbose.Value) Log.LogDebug(text); });
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
            // The prediction Postfix adds the preview lines and the codex flags of the dish cards.
            if (DishPreview.Value || CodexMarks.Value) patches.Add(typeof(PreviewOnRefreshPrediction));
            if (RowSplit.Value)
                patches.AddRange(new[] { typeof(RowSplitOnRefreshPrediction), typeof(RowSplitOnStart), typeof(RowSplitOnValidate), typeof(RowSplitOnSettle), typeof(RowSplitOnAdmission) });
            if (SeparatePieces.Value)
                patches.AddRange(new[] { typeof(PiecesOnCookingDragMove), typeof(PiecesOnTryMergeIntoOwner), typeof(PiecesOnMoveItem) });
            if (CookingStorages.Value)
                patches.AddRange(new[] { typeof(TagRow), typeof(TagMatchOnEvaluate), typeof(TagMatchOnTryGetMatchRank), typeof(TagMatchOnTryGetPutRank), typeof(CookingTabs), typeof(CookingTabsOnBrew), typeof(CookingTabsOnRatCage), typeof(CookingTabsOnCompressor), typeof(SlShared.ModTags.ModTagIconLoad), typeof(SlShared.ModTags.ModTagIconPreload) });
            // The tab list of each food window: the icon tabs, and the Verbose line of the cooking storage tabs.
            if (IconTabs.Value || CookingStorages.Value)
                patches.AddRange(new[] { typeof(IconTabsOnCookingOpen), typeof(IconTabsOnBrewOpen), typeof(IconTabsOnRatCageOpen), typeof(IconTabsOnCompressorOpen) });
            // The show of the Rat Cage window requests a pass for the food sort and for the icon tabs.
            if (FoodSort.Value || IconTabs.Value) patches.Add(typeof(PageTickOnRatCageShow));
            if (IconTabs.Value) patches.AddRange(new[] { typeof(PageTickOnBrewShow), typeof(PageTickOnCompressorShow) });
            if (FoodSort.Value)
                patches.AddRange(new[]
                {
                    typeof(StorageSortOnRefresh), typeof(StorageBagSortOnRefresh), typeof(CookingSortOnRefresh),
                    typeof(CookingSortOnOpen), typeof(CookingSortOnSwitchBag), typeof(CookingSortOnRefreshBag),
                    typeof(RatCageSortOnRefresh), typeof(RatCageSortOnOpen), typeof(RatCageSortOnSwitchBag), typeof(RatCageSortOnRefreshBag),
                });
            // One patch that fails to attach must not stop the others.
            foreach (var type in patches)
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogError($"patch {type.Name} failed: {e.Message}"); }
            }
            if (LoadFromAllTabs.Value) PatchGroup(harmony, LoadAllPatches.Types, "Load from all tabs is off, the fill takes only the open tab");
            if (unknownRecipes && !CodexMarks.Value)
                Log.LogInfo("UnknownRecipesWithIngredients or UnknownRecipesWithoutIngredients is on, but CodexMarks is off, so the Recipe Book shows no unknown recipe.");
            else if (unknownRecipes)
            {
                string missing = UnknownRecipes.MissingRowMethod();
                if (missing != null)
                    Log.LogWarning($"Reducer_Web_Cooking.{missing} is missing, so the Codex filter shows no unknown recipe.");
                else
                    PatchGroup(harmony, UnknownRecipes.Types, "The Codex filter shows no unknown recipe");
            }
            // The timing lines of the Recipe Book also run with LoadFromAllTabs off, for the time without the feature.
            if (Verbose.Value)
                foreach (var type in new[] { typeof(LoadAllTimingOnRefresh), typeof(LoadAllTimingOnRecipeClick) })
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
            if (ColdSortFood.Value)
            {
                try { ColdSortDetour.Apply(); }
                catch (Exception e)
                {
                    ColdSortDetour.Undo();
                    Log.LogWarning($"patch ItemManager.TryRankQuickStoreTarget failed, Store Chilled Food keeps the game's targets: {e.Message}");
                }
            }
            // Logged when a feature differs from its default.
            if (!DishPreview.Value || !FoodSort.Value || !CookingStorages.Value || !RowSplit.Value || !SeparatePieces.Value || !DimUncookable.Value || !ColdSortFood.Value || !IconTabs.Value || !CodexMarks.Value || !LoadFromAllTabs.Value || !UnknownRecipesWithIngredients.Value || UnknownRecipesWithoutIngredients.Value)
                Log.LogInfo($"Project Cook features: DishPreview={DishPreview.Value} FoodSort={FoodSort.Value} CookingStorages={CookingStorages.Value} RowSplit={RowSplit.Value} SeparatePieces={SeparatePieces.Value} DimUncookable={DimUncookable.Value} ColdSortFood={ColdSortFood.Value} IconTabs={IconTabs.Value} CodexMarks={CodexMarks.Value} LoadFromAllTabs={LoadFromAllTabs.Value} UnknownRecipesWithIngredients={UnknownRecipesWithIngredients.Value} UnknownRecipesWithoutIngredients={UnknownRecipesWithoutIngredients.Value}");
            Log.LogInfo("Project Cook loaded.");
        }

        // Patches all classes of a feature, or none: when one target is missing (a game update), the patches already
        // applied are taken off again, and one warning names the class that failed. Patch() gives the replacement
        // methods, not the originals, so the patches of the group are found by their declaring class instead; only these
        // patch methods come off, so another patch of the mod on the same original stays.
        private static void PatchGroup(Harmony harmony, Type[] types, string offText)
        {
            foreach (var type in types)
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e)
                {
                    var group = new System.Collections.Generic.HashSet<Type>(types);
                    foreach (var original in new System.Collections.Generic.List<System.Reflection.MethodBase>(harmony.GetPatchedMethods()))
                    {
                        var info = Harmony.GetPatchInfo(original);
                        var patches = new System.Collections.Generic.List<Patch>();
                        patches.AddRange(info.Prefixes);
                        patches.AddRange(info.Postfixes);
                        patches.AddRange(info.Transpilers);
                        patches.AddRange(info.Finalizers);
                        foreach (var patch in patches)
                            if (patch.owner == harmony.Id && group.Contains(patch.PatchMethod.DeclaringType)) harmony.Unpatch(original, patch.PatchMethod);
                    }
                    Log.LogWarning($"patch {type.Name} failed, its target method is missing. {offText}: {e.Message}");
                    return;
                }
            }
        }
    }
}
