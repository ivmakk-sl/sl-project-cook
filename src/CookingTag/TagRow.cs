using System;
using GameCore.HotUpdate;
using HarmonyLib;

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
    [HarmonyPatch(typeof(FurnitureTagRules), "EnsureCache")]
    internal static class TagRow
    {
        private static IntPtr lastCache;
        private static int lastLanguage = -1;
        private static bool failed;

        private static void Prefix()
        {
            if (failed) return;
            try
            {
                var config = ConfigManager.Instance;
                var cache = config?.customCache;
                if (cache == null) return;
                var language = (int)cache.LanguageType;
                if (cache.Pointer == lastCache && language == lastLanguage) return;
                lastCache = cache.Pointer;
                lastLanguage = language;

                var words = CookingTagLogic.WordsFor(language);
                SetText(cache.LanguageMap, words);
                SetText(cache.ChineseLanguageMap, CookingTagLogic.WordsFor(0));
                SetText(cache.EnglishLanguageMap, CookingTagLogic.WordsFor(1));

                var table = config._Config_FurnitureTag_Dict;
                if (table == null || table.ContainsKey(CookingTagLogic.TagId)) return;
                table[CookingTagLogic.TagId] = new Config_FurnitureTag
                {
                    ID = CookingTagLogic.TagId,
                    TagName = CookingTagLogic.NameKey,
                    TagName_Local = words.Name,
                    TagDesc = CookingTagLogic.DescKey,
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
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"cooking tag row added (cache cleared: {wasBuilt})");
            }
            catch (Exception e)
            {
                failed = true;
                Plugin.Log.LogWarning($"Project Cook: the cooking tag could not be added, the tag rule shows only the game's tags: {e.Message}");
            }
        }

        private static void SetText(Il2CppSystem.Collections.Generic.Dictionary<string, string> map, CookingTagLogic.Words words)
        {
            if (map == null) return;
            map[CookingTagLogic.NameKey] = words.Name;
            map[CookingTagLogic.DescKey] = words.Desc;
        }
    }
}
