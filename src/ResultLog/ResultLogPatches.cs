using System;
using System.Text;
using HarmonyLib;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace ProjectCook
{
    // Log of the real cooking result, to compare with the preview entries.
    [HarmonyPatch(typeof(Furniture), "RollCookingQuality")]
    internal static class ResultLogOnRollCookingQuality
    {
        private static void Postfix(Config_CookingRecipe recipe, bool isExactMatch, int __result)
        {
            if (!Plugin.Verbose.Value) return;
            try
            {
                int level = recipe == null ? -1 : Furniture.GetQualityTier(__result, recipe.QualityMap);
                Plugin.Log.LogDebug($"result roll recipe={recipe?.ID} exact={isExactMatch} quality={__result} qualityTier={level}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook result log failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(CookingFormula), "CalcProductVD")]
    internal static class ResultLogOnCalcProductVD
    {
        private static void Postfix(CookingFormula.CookingTier tier, int qualityTier, int productItemId, bool isExact, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float> __result)
        {
            if (!Plugin.Verbose.Value || Preview.Calculating) return;
            try
            {
                var sb = new StringBuilder();
                for (int i = 0; __result != null && i < __result.Length; i++) sb.Append(i == 0 ? "" : "/").Append(__result[i].ToString("0.##"));
                Plugin.Log.LogDebug($"result vd tier={(int)tier} qualityTier={qualityTier} product={productItemId} exact={isExact} vd={sb}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook result log failed: {e}");
            }
        }
    }

    // Log of the cooking EXP that the game actually awards. Furniture.SettleCookingResult calls this to give the
    // cooking EXP for a cooked dish. It is not known if the game calls it once for each dish or once for the pot
    // with the sum, so the entry logs the raw argument.
    [HarmonyPatch(typeof(CookingRecordComponent), "AddCookExp")]
    internal static class ResultLogOnAddCookExp
    {
        private static void Postfix(int exp, int __result)
        {
            if (!Plugin.Verbose.Value) return;
            try
            {
                Plugin.Log.LogDebug($"result exp value={exp} returned={__result}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook result log failed: {e}");
            }
        }
    }
}
