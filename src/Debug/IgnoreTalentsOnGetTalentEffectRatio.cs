using System.Collections.Generic;
using HarmonyLib;
using GameCore.HotUpdate.Battle.Logic;

namespace ProjectCook
{
    // Attached only when the debug option IgnoreCookingTalents is on. The game reads each talent effect through the
    // overloads of this method, the quality roll and the EXP grant included, so the real results and the preview
    // stay equal. The last argument of each overload is the effect key.
    [HarmonyPatch]
    internal static class IgnoreTalentsOnGetTalentEffectRatio
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Furniture)))
                if (method.Name == "GetTalentEffectRatio") yield return method;
        }

        private static void Postfix(object[] __args, ref float __result)
        {
            string key = __args.Length > 0 ? __args[__args.Length - 1] as string : null;
            if (key != null && (key.StartsWith("Buff/AE_Cook") || key == "Buff/AE_PerfectQualityMultiplier")) __result = 0f;
        }
    }
}
