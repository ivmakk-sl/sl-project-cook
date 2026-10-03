using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    internal static class Preview
    {
        // True while the mod calls the game's formula methods, so the result log skips these calls.
        internal static bool Calculating;

        // Index is PreviewLogic.Fail..Perfect. The value is the qualityTier argument of CookingFormula.
        private static readonly int[] FormulaQualityTier = { 0, 1, 2, 3 };

        public static Dictionary<int, string> Build(State_Web_Cooking state, List<PreviewLogic.Entry> entries, Il2CppDict.Dictionary<int, int> workbenchIds, Il2CppDict.Dictionary<int, int> workbenchTags, PreviewLogic.Words words, out Dictionary<int, string> tips)
        {
            var result = new Dictionary<int, string>();
            tips = new Dictionary<int, string>();
            var config = ConfigManager.Instance;

            var talents = TalentInputs.Read();
            var eat = EatInputs.Read(out string eatLog);
            int qualityBase = QualityBonusWithoutRecipe(state, config, talents, out int floor, out string inputs);

            // The interop layer reads the value tuples of PreMatchRecipes and CalcSplit wrongly, so the mod takes
            // the recipes from the prediction list and builds the ingredient set with the game's own method.
            foreach (var entry in entries)
            {
                int recipeId = entry.RecipeId;
                var recipe = recipeId == 0 ? null : config.Get_Config_CookingRecipe(recipeId);
                if (recipe == null) continue;
                var participated = Reducer_Web_Cooking.BuildParticipatedIngredients(recipe, workbenchIds, workbenchTags);
                if (participated == null) continue;

                int bonus = qualityBase + (entry.IsExact ? Setting(config, "CookingQuality_ExactMatchBonus") : 0)
                    + PreviewLogic.TalentQualityBonus(talents.PerfectQuality, talents.TagQuality, entry.IsExact);
                double[] chances = PreviewLogic.QualityChances(floor, bonus, ToArray(recipe.QualityMap));

                int[] productIds = { recipe.FailItemID, recipe.NormalItemID, recipe.GoodItemID, recipe.PerfectItemID };
                var stats = new int[4][];
                var baseSatiety = new int[4];
                var rawSatiety = new float[4];
                var portions = new int[4];
                var tradeValues = new int[4];
                var exps = new int[4];
                Calculating = true;
                try
                {
                    for (int level = 0; level < 4; level++)
                    {
                        var vd = CookingFormula.CalcProductVD(participated, (CookingFormula.CookingTier)entry.Tier, FormulaQualityTier[level], productIds[level], entry.IsExact);
                        var values = new float[vd.Length];
                        for (int i = 0; i < vd.Length; i++) values[i] = vd[i];
                        // The game splits by the satiety before it is rounded for the display.
                        rawSatiety[level] = vd.Length > 0 ? vd[0] : 0f;
                        baseSatiety[level] = (int)Math.Round(rawSatiety[level]);
                        portions[level] = PreviewLogic.Portions(rawSatiety[level], recipe.SatietyStandard > 0 ? recipe.SatietyStandard : FloatSetting(config, "CookingSatiety_SplitThreshold"));
                        var product = config.Get_Config_Item(productIds[level]);
                        // A cooked dish has its own values (the vd), so the nourish factor applies. The eat values
                        // are those of the whole dish, all portions together.
                        var dish = new EatLogic.Dish
                        {
                            Values = values,
                            HasInstanceValues = true,
                            SubCategory = product?.SubCategory ?? 0,
                            IsStaple = product != null && product.Category == 1 && product.SubCategory == 1,
                            IsPerfect = level == PreviewLogic.Perfect,
                            Portions = portions[level],
                        };
                        var eatValues = EatLogic.EatValues(dish, eat);
                        stats[level] = Array.ConvertAll(eatValues, v => (int)Math.Round(v));
                        tradeValues[level] = PreviewLogic.TradeValue(product?.TradeValue ?? 0, talents.Appraisal);
                        exps[level] = PreviewLogic.CookExp(recipe.CookExp, talents.ExpRatio, level);
                    }
                }
                finally
                {
                    Calculating = false;
                }

                var lines = PreviewLogic.Lines(chances, stats, portions, words);
                result[recipeId] = string.Join("\n", lines);
                var tipLines = PreviewLogic.TipLines(chances, tradeValues, exps[PreviewLogic.Perfect], entry.Tier, words);
                tips[recipeId] = string.Join("\n", tipLines);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"preview recipe={recipeId} tier={entry.Tier} exact={entry.IsExact} {inputs} bonus={bonus} talents perfect={talents.PerfectQuality} tag={talents.TagQuality} rotten={talents.RottenReduce} exp={talents.ExpRatio} appraisal={talents.Appraisal} | eat {eatLog} | {string.Join(" | ", lines)} | all levels F/N/G/P sat={baseSatiety[0]}/{baseSatiety[1]}/{baseSatiety[2]}/{baseSatiety[3]} eat={string.Join("/", Array.ConvertAll(stats, l => string.Join(",", l)))} satRaw={string.Join("/", Array.ConvertAll(rawSatiety, v => v.ToString("0.###")))} portions={string.Join("/", portions)} trade={tradeValues[0]}/{tradeValues[1]}/{tradeValues[2]}/{tradeValues[3]} exp={exps[0]}/{exps[1]}/{exps[2]}/{exps[3]}");
            }
            return result;
        }

        // Quality floor and all bonuses that do not depend on the recipe: fresh or expired, seasonings, furniture.
        private static int QualityBonusWithoutRecipe(State_Web_Cooking state, ConfigManager config, TalentInputs talents, out int floor, out string inputs)
        {
            floor = config.Get_Config_CookingLv(state.CookingLevel)?.QualityFloor ?? 0;
            int furniture = config.Get_Config_FurnitureCook(state.CookFurnitureId)?.QualityBonus ?? 0;

            bool rotten = false;
            int seasonings = 0;
            var items = state.WorkbenchItems;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.IsExpired.Value) rotten = true;
                if (item.SubCategory.Value == SeasoningFoodType) seasonings++;
            }

            int freshness = rotten
                ? PreviewLogic.RottenPenalty(Setting(config, "CookingQuality_RottenPenalty"), talents.RottenReduce)
                : Setting(config, "CookingQuality_FreshBonus");
            var seasoningSetting = config.Get_Config_GlobalSetting("CookingQuality_SeasoningBonus");
            int seasoning = seasoningSetting == null ? 0 : PreviewLogic.SeasoningBonus(seasonings, seasoningSetting.Params1, seasoningSetting.Params2);

            inputs = $"cookLv={state.CookingLevel} floor={floor} rotten={rotten} freshness={freshness} seasonings={seasonings} seasoningBonus={seasoning} furniture={state.CookFurnitureId} furnitureBonus={furniture}";
            return freshness + seasoning + furniture;
        }

        private const int SeasoningFoodType = 8;

        private static int Setting(ConfigManager config, string key) => config.Get_Config_GlobalSetting(key)?.Params1 ?? 0;

        // For a setting that the game reads from its float column.
        private static float FloatSetting(ConfigManager config, string key) => config.Get_Config_GlobalSetting(key)?.Params2 ?? 0f;

        private static int[] ToArray(Il2CppDict.List<int> list)
        {
            if (list == null) return null;
            var array = new int[list.Count];
            for (int i = 0; i < array.Length; i++) array[i] = list[i];
            return array;
        }
    }
}
