using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using HarmonyLib;
using SlShared.ModTags;

namespace ProjectCook
{
    // Adds the cooking tag to the game's tag table and its words to the game's text maps. FurnitureTagRules.EnsureCache
    // copies _Config_FurnitureTag_Dict into its own cache once for each ConfigManager.customCache, and each read of
    // the tag rule (the rule window, Sanitize at load, each match) calls it first. So the row is in the table before
    // the game's first use of it, and Sanitize keeps the saved id.
    // The names resolve with ConfigManager.GetLocalTxt, which reads customCache.LanguageMap (the tag chips through
    // ConstantTextTools.GetLocalText, the rule text directly), so the two keys go into that map. A Prefix of
    // GetLocalTxt would run for each text of the game.
    // EnsureCache runs for each match of the robot, so the Prefix does its work only when the customCache object or
    // the display language changes.
    // The row has the keys of the mod tags library, so a game row with the id 1900 differs from it. When the game took
    // the id, the mod adds no row and Disabled turns off the rule swap, the tabs, and the food sort link of the id.
    [HarmonyPatch(typeof(FurnitureTagRules), "EnsureCache")]
    internal static class TagRow
    {
        private static IntPtr lastCache;
        private static int lastLanguage = -1;
        private static bool failed, warnedTaken;

        // The ids of the range that the game took, read from the table before the row is added.
        internal static HashSet<int> Taken = new HashSet<int>();
        internal static bool Disabled;

        private static void Prefix()
        {
            if (failed) return;
            try
            {
                var config = ConfigManager.Instance;
                var cache = config?.customCache;
                if (cache == null) return;
                var language = ModTexts.Language();
                if (cache.Pointer == lastCache && language == lastLanguage) return;
                lastCache = cache.Pointer;
                lastLanguage = language;

                var table = config._Config_FurnitureTag_Dict;
                if (table == null) return;
                var rows = Rows(table);
                Taken = ModTagRule.TakenIds(rows);
                Disabled = !ModTagRule.CanAdd(ModTagRule.CookingTagId, rows);
                if (Disabled)
                {
                    if (!warnedTaken)
                    {
                        warnedTaken = true;
                        Plugin.Log.LogWarning($"Project Cook: the game uses the tag id {ModTagRule.CookingTagId} for a tag of its own, so the cooking tag is off.");
                    }
                    return;
                }

                // The texts come from the i18n files only: the Prefix can run early in a save load, before the game texts.
                var words = CookingTagLogic.WordsFrom(ModTexts.For(language));
                SetText(cache.LanguageMap, words);
                SetText(cache.ChineseLanguageMap, CookingTagLogic.WordsFrom(ModTexts.For(0)));
                SetText(cache.EnglishLanguageMap, CookingTagLogic.WordsFrom(ModTexts.For(1)));

                if (table.ContainsKey(ModTagRule.CookingTagId)) return;
                table[ModTagRule.CookingTagId] = new Config_FurnitureTag
                {
                    ID = ModTagRule.CookingTagId,
                    TagName = ModTagRule.NameKey(ModTagRule.CookingTagId),
                    TagName_Local = words.Name,
                    TagDesc = ModTagRule.DescKey(ModTagRule.CookingTagId),
                    TagDesc_Local = words.Desc,
                    IconKey = CookingTagLogic.IconKey,
                    Color = CookingTagLogic.Color,
                    SortPriority = CookingTagLogic.SortPriority,
                    Dim = CookingTagLogic.Dim,
                    Order = CookingTagLogic.Order,
                };
                // The cache of the tag rules was built without the row: a null token makes EnsureCache build it again.
                var wasBuilt = FurnitureTagRules._cacheToken != null;
                FurnitureTagRules._cacheToken = null;
                if (Plugin.Verbose.Value)
                    Plugin.Log.LogDebug($"cooking tag row added, taken ids: {(Taken.Count == 0 ? "none" : string.Join(",", Taken))} (cache cleared: {wasBuilt})");
            }
            catch (Exception e)
            {
                failed = true;
                Plugin.Log.LogWarning($"Project Cook: the cooking tag could not be added, the tag rule shows only the game's tags: {e.Message}");
            }
        }

        // The (id, name key) pairs of the game's tag table.
        private static List<ModTagRule.Row> Rows(Il2CppSystem.Collections.Generic.Dictionary<int, Config_FurnitureTag> table)
        {
            var rows = new List<ModTagRule.Row>(table.Count);
            var e = table.GetEnumerator();
            while (e.MoveNext())
            {
                var row = e.Current.Value;
                rows.Add(new ModTagRule.Row(e.Current.Key, row?.TagName));
            }
            return rows;
        }

        private static void SetText(Il2CppSystem.Collections.Generic.Dictionary<string, string> map, CookingTagLogic.Words words)
        {
            if (map == null) return;
            map[ModTagRule.NameKey(ModTagRule.CookingTagId)] = words.Name;
            map[ModTagRule.DescKey(ModTagRule.CookingTagId)] = words.Desc;
        }
    }
}
