using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppList = Il2CppSystem.Collections.Generic.List<int>;
using Il2CppReadOnlyList = Il2CppSystem.Collections.Generic.IReadOnlyList<int>;

namespace ProjectCook
{
    // The game's tag logic sees each rule without the cooking tag, or Food when the cooking tag is the only tag
    // (CookingTagLogic.RuleForGame). A match of the tag itself would change the robot rules: the options of one row
    // are OR'd (Chilled + Cooking would take each food), and the specificity counts the options (a storage with the
    // tag would rank above one without it).
    // Each path that matches a rule against an item goes through one of these three methods. Evaluate is a real call
    // from Matches, TryGetMatchLevel, FurnitureTagComponent.KeepsInPlace, and ItemManager.MatchesFurnitureTagRule.
    // The compiler inlined Evaluate into TryGetMatchRank and into FurnitureTagComponent.TryGetPutRank, which reads the
    // rule from its own TagIds.
    internal static class TagMatch
    {
        private static readonly HashSet<string> logged = new HashSet<string>();
        private static bool warned;

        // The rule for the game, or null when the rule has no cooking tag (the common case, with no allocation).
        public static Il2CppList ForGame(Il2CppObjectBase rule)
        {
            if (rule == null) return null;
            var list = rule.TryCast<Il2CppList>();
            if (list == null)
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning($"Project Cook: a tag rule of type {rule.GetType().Name} is not a list, the cooking tag can reach the game's tag logic.");
                }
                return null;
            }
            if (!list.Contains(CookingTagLogic.TagId)) return null;
            var ids = new int[list.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = list[i];
            var forGame = CookingTagLogic.RuleForGame(ids);
            var result = new Il2CppList(forGame.Count);
            foreach (var id in forGame) result.Add(id);
            if (Plugin.Verbose.Value)
            {
                var text = $"cooking tag rule {string.Join(",", ids)} -> {string.Join(",", forGame)}";
                if (logged.Add(text)) Plugin.Log.LogDebug(text);
            }
            return result;
        }

        public static void Warn(string where, Exception e)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning($"Project Cook: {where} failed, the cooking tag can reach the game's tag logic: {e.Message}");
        }
    }

    [HarmonyPatch(typeof(FurnitureTagRules), "Evaluate")]
    internal static class TagMatchOnEvaluate
    {
        private static void Prefix(ref Il2CppReadOnlyList rule)
        {
            try
            {
                var forGame = TagMatch.ForGame(rule);
                if (forGame != null) rule = forGame.Cast<Il2CppReadOnlyList>();
            }
            catch (Exception e) { TagMatch.Warn("Evaluate", e); }
        }
    }

    [HarmonyPatch(typeof(FurnitureTagRules), nameof(FurnitureTagRules.TryGetMatchRank))]
    internal static class TagMatchOnTryGetMatchRank
    {
        private static void Prefix(ref Il2CppReadOnlyList rule)
        {
            try
            {
                var forGame = TagMatch.ForGame(rule);
                if (forGame != null) rule = forGame.Cast<Il2CppReadOnlyList>();
            }
            catch (Exception e) { TagMatch.Warn("TryGetMatchRank", e); }
        }
    }

    // TryGetPutRank reads the rule from TagIds of the component, so the Prefix swaps the list for the call and the
    // Finalizer puts the saved list back, also when the call throws (a Postfix would not run then, and the storage
    // would keep the list without the cooking tag). It returns nothing, so the exception goes on to the game.
    [HarmonyPatch(typeof(FurnitureTagComponent), nameof(FurnitureTagComponent.TryGetPutRank))]
    internal static class TagMatchOnTryGetPutRank
    {
        private static void Prefix(FurnitureTagComponent __instance, out Il2CppList __state)
        {
            __state = null;
            try
            {
                var saved = __instance.TagIds;
                var forGame = TagMatch.ForGame(saved);
                if (forGame == null) return;
                __state = saved;
                __instance.TagIds = forGame;
            }
            catch (Exception e) { TagMatch.Warn("TryGetPutRank", e); }
        }

        private static void Finalizer(FurnitureTagComponent __instance, Il2CppList __state)
        {
            if (__state == null) return;
            try { __instance.TagIds = __state; }
            catch (Exception e) { Plugin.Log.LogError($"Project Cook: the tag rule of a storage could not be put back: {e}"); }
        }
    }
}
