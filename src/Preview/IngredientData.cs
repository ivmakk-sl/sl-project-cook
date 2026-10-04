using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;

namespace ProjectCook
{
    // The data of setData: the tooltip lines and the tier of each ingredient, by config ID, read from the Item
    // table. The words and the trade value with the appraisal are part of the tooltip lines, so the data is read again
    // only when the words or the appraisal change.
    internal static class IngredientData
    {
        private static PreviewLogic.Words words;
        private static float appraisal;
        private static string json;

        // The tiers are the game's tiers 1 to 3; PageJson keeps only the tiers that the page marks.
        public static string Json(PreviewLogic.Words current, float currentAppraisal)
        {
            if (json == null || !current.SameAs(words) || currentAppraisal != appraisal)
            {
                var tips = new Dictionary<int, string>();
                var tiers = new Dictionary<int, int>();
                var e = ConfigManager.Instance._Config_Item_Dict.GetEnumerator();
                while (e.MoveNext())
                {
                    var item = e.Current.Value;
                    if (item == null || item.Category != 1) continue;
                    int tier = Reducer_Web_Cooking.ResolveIngredientTier(item.ID);
                    string tip = PreviewLogic.IngredientTip(
                        new[]
                        {
                            (int)Math.Round(item.ValueDisplay1), (int)Math.Round(item.ValueDisplay2), (int)Math.Round(item.ValueDisplay3),
                            (int)Math.Round(item.ValueDisplay4), (int)Math.Round(item.ValueDisplay5),
                        },
                        tier, PreviewLogic.TradeValue(item.TradeValue, currentAppraisal), current);
                    if (tip != null) tips[item.ID] = tip;
                    if (tier >= 1 && tier <= 3) tiers[item.ID] = tier;
                }
                json = PageJson.DataJson(tips, tiers, Plugin.PageFeatures, current.Portion);
                words = current;
                appraisal = currentAppraisal;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"ingredient tips: {tips.Count} items, {tiers.Count} with a tier, appraisal={currentAppraisal}");
            }
            return json;
        }
    }
}
