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

    // Log of what the leading role gets when it eats: one line for each stat change from an item, with the value
    // that the eat path passes in (after nourish), the change of the stat, and the stat before, after, and its
    // maximum. The sum of the changes of one eat compares with the eat values of the preview and the food sort.
    [HarmonyPatch(typeof(AttributeComponent), "CostAttrValue",
        new[] { typeof(AttrName), typeof(float), typeof(long), typeof(EffectArgsType), typeof(bool), typeof(bool), typeof(VitalityChangeSourceType), typeof(int) })]
    internal static class ResultLogOnEat
    {
        private static void Prefix(AttributeComponent __instance, AttrName attrName, VitalityChangeSourceType sourceType, out float __state)
        {
            __state = float.NaN;
            if (!Plugin.Verbose.Value || sourceType != VitalityChangeSourceType.Item) return;
            try
            {
                if (__instance.ownerInstanceId != BaseSingleton<BattleLogicWorld>.Instance._AgentManager.GetLeadingRoleId()) return;
                __state = __instance.GetBaseValue_Float(attrName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook eat log failed: {e}");
            }
        }

        private static void Postfix(AttributeComponent __instance, AttrName attrName, float value, int configId, float __state)
        {
            if (float.IsNaN(__state)) return;
            try
            {
                float after = __instance.GetBaseValue_Float(attrName);
                // The maximum of a stat is the attribute 100 + the stat (MaxSatiety = 101 ...).
                int max = __instance.GetTotalValue_Int((AttrName)(100 + (int)attrName));
                Plugin.Log.LogDebug($"eat {configId} {attrName} in={value:0.##} change={after - __state:0.##} before={__state:0.##} after={after:0.##} max={max}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook eat log failed: {e}");
            }
        }
    }
}
