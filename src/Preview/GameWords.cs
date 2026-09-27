using System;
using GameCore.HotUpdate;

namespace ProjectCook
{
    // The words that the mod adds, as the game writes them in the current display language.
    internal static class GameWords
    {
        // Order of PreviewLogic.WordsOrEnglish: quality Fail..Perfect, tier High..Low, then the stat array order.
        // The cooking page takes its own quality and tier names from the same SR_Web_Cooking keys.
        private static readonly string[] Keys =
        {
            "SR_Web_Cooking_87", "SR_Web_Cooking_86", "SR_Web_Cooking_85", "SR_Web_Cooking_84",
            "SR_Web_Cooking_53", "SR_Web_Cooking_54", "SR_Web_Cooking_55",
            "GameKey_1", "GameKey_2", "GameKey_3", "GameKey_4", "GameKey_5",
        };

        private static bool fallBackWarned;
        private static PreviewLogic.Words logged;

        // The language can change while the game runs, so the words are read again at each refresh.
        public static PreviewLogic.Words Current()
        {
            var config = ConfigManager.Instance;
            var texts = new string[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                // The game's own key getters (GameKey.Text_*) use this call. ConfigManager.GetLocalTxt does not take these keys.
                try { texts[i] = ConstantTextTools.ToConstantTextOrEmpty(Keys[i]); }
                catch (Exception) { texts[i] = null; }
            }

            bool chinese = false;
            try { chinese = config?.customCache != null && config.customCache.LanguageType == LanguageType.Chinese; }
            catch (Exception) { }

            var words = PreviewLogic.WordsOrEnglish(Keys, texts, chinese, out var fellBack);
            if (fellBack.Count > 0 && !fallBackWarned)
            {
                fallBackWarned = true;
                Plugin.Log.LogWarning($"The game has no text for {string.Join(", ", fellBack)}, so these words show in English. The game texts probably changed.");
            }
            if (Plugin.Verbose.Value && !words.SameAs(logged))
            {
                logged = words;
                Plugin.Log.LogDebug($"words chinese={chinese} tierLabel={words.TierLabel} quality={string.Join("/", words.Quality)} tier={words.Tier[1]}/{words.Tier[2]}/{words.Tier[3]} stat={string.Join("/", words.Stat)}");
            }
            return words;
        }
    }
}
