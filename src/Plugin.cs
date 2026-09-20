using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    [BepInPlugin(PluginGuid, "Project Cook", "1.0.0")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.projectcook";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;

        public override void Load()
        {
            Log = base.Log;
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Log each preview (quality inputs, chances, stats, portions), each cooked result (quality roll and stats), and the page script state. Use it to compare the preview with the real result. Keep off in normal play.");
            var harmony = new Harmony(PluginGuid);
            // One patch that fails to attach must not stop the others.
            foreach (var type in new[] { typeof(RefreshPredictionPatch), typeof(RollCookingQualityLog), typeof(CalcProductVDLog) })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogError($"patch {type.Name} failed: {e.Message}"); }
            }
            Log.LogInfo("Project Cook loaded.");
        }
    }

    // RefreshPrediction runs after each workbench change and fills the "THIS POT" list of the cooking page.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshPrediction")]
    internal static class RefreshPredictionPatch
    {
        private static bool formatWarned;

        private static void Postfix(State_Web_Cooking state, Il2CppDict.Dictionary<int, int> workbenchIds, Il2CppDict.Dictionary<int, int> workbenchTags)
        {
            try
            {
                if (state == null || state.HotPotMode.Value) return;
                var words = GameWords.Current();
                // Also with an empty workbench, because the ingredient tooltips need the script.
                PageScript.Install(words);

                string json = state.PredictionListJson.Value;
                var entries = PreviewLogic.ReadEntries(json);
                if (entries.Count == 0)
                {
                    // A list with objects but no readable entry means that a game update changed the format.
                    if (!formatWarned && json != null && json.IndexOf('{') >= 0)
                    {
                        formatWarned = true;
                        Plugin.Log.LogWarning($"The prediction list has no readable entry, so no preview lines show. The game format probably changed: {json}");
                    }
                    return;
                }

                var previews = Preview.Build(state, entries, workbenchIds, workbenchTags, words);
                if (previews.Count == 0) return;

                state.PredictionListJson.Value = PreviewLogic.AddPreviews(json, previews);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook failed: {e}");
            }
        }
    }

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

    internal static class Preview
    {
        // True while the mod calls the game's formula methods, so the result log skips these calls.
        internal static bool Calculating;

        // Index is PreviewLogic.Fail..Perfect. The value is the qualityTier argument of CookingFormula.
        private static readonly int[] FormulaQualityTier = { 0, 1, 2, 3 };

        public static Dictionary<int, string> Build(State_Web_Cooking state, List<PreviewLogic.Entry> entries, Il2CppDict.Dictionary<int, int> workbenchIds, Il2CppDict.Dictionary<int, int> workbenchTags, PreviewLogic.Words words)
        {
            var result = new Dictionary<int, string>();
            var config = ConfigManager.Instance;


            int qualityBase = QualityBonusWithoutRecipe(state, config, out int floor, out string inputs);

            // The interop layer reads the value tuples of PreMatchRecipes and CalcSplit wrongly, so the mod takes
            // the recipes from the prediction list and builds the ingredient set with the game's own method.
            foreach (var entry in entries)
            {
                int recipeId = entry.RecipeId;
                var recipe = recipeId == 0 ? null : config.Get_Config_CookingRecipe(recipeId);
                if (recipe == null) continue;
                var participated = Reducer_Web_Cooking.BuildParticipatedIngredients(recipe, workbenchIds, workbenchTags);
                if (participated == null) continue;

                int bonus = qualityBase + (entry.IsExact ? Setting(config, "CookingQuality_ExactMatchBonus") : 0);
                double[] chances = PreviewLogic.QualityChances(floor, bonus, ToArray(recipe.QualityMap));

                int[] productIds = { recipe.FailItemID, recipe.NormalItemID, recipe.GoodItemID, recipe.PerfectItemID };
                var stats = new int[4][];
                var portions = new int[4];
                Calculating = true;
                try
                {
                    for (int level = 0; level < 4; level++)
                    {
                        var vd = CookingFormula.CalcProductVD(participated, (CookingFormula.CookingTier)entry.Tier, FormulaQualityTier[level], productIds[level], entry.IsExact);
                        stats[level] = new int[vd.Length];
                        for (int i = 0; i < vd.Length; i++) stats[level][i] = (int)Math.Round(vd[i]);
                        portions[level] = PreviewLogic.Portions(stats[level][0], recipe.SatietyStandard > 0 ? recipe.SatietyStandard : Setting(config, "CookingSatiety_SplitThreshold"));
                    }
                }
                finally
                {
                    Calculating = false;
                }

                var lines = PreviewLogic.Lines(chances, stats, portions, words);
                result[recipeId] = string.Join("\n", lines);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"preview recipe={recipeId} tier={entry.Tier} exact={entry.IsExact} {inputs} bonus={bonus} talents=not included (AE_PerfectQualityMultiplier, AE_CookTagQualityBonus, AE_CookRottenPenaltyReduce) | {string.Join(" | ", lines)} | all levels F/N/G/P sat={stats[0][0]}/{stats[1][0]}/{stats[2][0]}/{stats[3][0]} portions={string.Join("/", portions)}");
            }
            return result;
        }

        // Quality floor and all bonuses that do not depend on the recipe: fresh or rotten, seasonings, furniture.
        private static int QualityBonusWithoutRecipe(State_Web_Cooking state, ConfigManager config, out int floor, out string inputs)
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

            int freshness = Setting(config, rotten ? "CookingQuality_RottenPenalty" : "CookingQuality_FreshBonus");
            var seasoningSetting = config.Get_Config_GlobalSetting("CookingQuality_SeasoningBonus");
            int seasoning = seasoningSetting == null ? 0 : PreviewLogic.SeasoningBonus(seasonings, seasoningSetting.Params1, seasoningSetting.Params2);

            inputs = $"cookLv={state.CookingLevel} floor={floor} rotten={rotten} freshness={freshness} seasonings={seasonings} seasoningBonus={seasoning} furniture={state.CookFurnitureId} furnitureBonus={furniture}";
            return freshness + seasoning + furniture;
        }

        private const int SeasoningFoodType = 8;

        private static int Setting(ConfigManager config, string key) => config.Get_Config_GlobalSetting(key)?.Params1 ?? 0;

        private static int[] ToArray(Il2CppDict.List<int> list)
        {
            if (list == null) return null;
            var array = new int[list.Count];
            for (int i = 0; i < array.Length; i++) array[i] = list[i];
            return array;
        }
    }

    // Log of the real cooking result, to compare with the preview entries.
    [HarmonyPatch(typeof(Furniture), "RollCookingQuality")]
    internal static class RollCookingQualityLog
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
    internal static class CalcProductVDLog
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

    // The cooking page draws each card with a fixed name line and hint line. The page ignores unknown entry fields,
    // so the mod sends the preview text in a "Preview" field and this script adds it to the card as extra lines.
    internal static class PageScript
    {
        // Runs in the root page. Each UI page is an iframe, and a reopened panel is a new iframe, so the hook is
        // installed again when it is missing. The wrapper keeps the game's render function and only adds lines.
        // A panel that opens with ingredients has no frame yet at the first refresh, so the script tries again for 3 seconds.
        // The colors are the game's own: quality colors of the cooking page (--q-perfect, --q-good, --q-fail), tier colors
        // of its result window (tier-high, tier-low), and the value colors of the item window (pos, neg).
        private const string Script = @"(function attempt(retries){
  var qualityColors = { 3: '#FFC107', 2: '#B988E6', 0: '#707070' };
  var tierColors = { 1: '#a4b546', 3: '#c0c0c0' };
  var frames = document.querySelectorAll('iframe'), result = 'no cooking frame';
  for (var i = 0; i < frames.length; i++) {
    try {
      var w = frames[i].contentWindow;
      if (!w || typeof w.renderPredictionList !== 'function') continue;
      if (w.__cookingPreview) { result = 'already installed'; continue; }
      var original = w.renderPredictionList;
      w.renderPredictionList = function (entries) {
        original(entries);
        try {
          var cards = w.document.querySelectorAll('#predictionList .pot-card');
          for (var k = 0; k < cards.length && k < entries.length; k++) {
            if (!entries[k].Preview) continue;
            var hint = cards[k].querySelector('.pot-hint');
            if (hint) hint.style.display = 'none';
            var rows = entries[k].Preview.split('\n').map(function (text) { return text.split('|'); });
            var grid = w.document.createElement('div');
            grid.className = 'pot-hint';
            grid.style.cssText = 'opacity:1;color:#e8dcc8;display:grid;column-gap:8px;white-space:nowrap;' +
              'grid-template-columns:repeat(' + (rows[0].length - 1) + ',max-content)';
            rows.forEach(function (cells) {
              cells.forEach(function (text, column) {
                if (column === 0) return;
                var cell = w.document.createElement('span');
                if (column === 2) cell.style.textAlign = 'right';
                if (column === 1 && qualityColors[cells[0]]) cell.style.color = qualityColors[cells[0]];
                cell.textContent = text;
                grid.appendChild(cell);
              });
            });
            cards[k].querySelector('.pot-bd').appendChild(grid);
          }
        } catch (e) {}
      };
      if (typeof w.showItemTip === 'function') {
        var originalTip = w.showItemTip;
        w.showItemTip = function (item) {
          originalTip(item);
          try {
            var tip = item && item.name && item.canCook !== false && window.__cookingTips && window.__cookingTips[item.configId];
            if (tip) tip.split('\n').forEach(function (text) {
              var line = w.document.createElement('div'), tier = text.match(/^T(\d)\|(.*)\|(.*)$/), parts = text.match(/^(.*: )(.+)$/);
              if (tier) {
                var tierName = w.document.createElement('span');
                tierName.textContent = tier[3];
                tierName.style.color = tierColors[tier[1]] || '';
                line.textContent = tier[2] + ': ';
                line.appendChild(tierName);
              } else if (parts) {
                var value = w.document.createElement('span');
                value.textContent = parts[2];
                value.style.color = parts[2][0] === '-' ? '#EF5350' : parts[2][0] === '+' ? '#66BB6A' : '';
                line.textContent = parts[1];
                line.appendChild(value);
              } else line.textContent = text;
              w.document.getElementById('recipeTooltip').appendChild(line);
            });
          } catch (e) {}
        };
      }
      w.__cookingPreview = true;
      result = 'installed';
      try { w.renderPredictionList(w.eval('State.lastPredictionEntries')); } catch (e) { result = 'installed, no redraw: ' + e; }
    } catch (e) { result = 'error: ' + e; }
  }
  if (result === 'no cooking frame' && retries > 0) setTimeout(function () { attempt(retries - 1); }, 300);
  return result;
})(10)";

        public static void Install(PreviewLogic.Words words)
        {
            var webView = ReduxUISystem.Instance?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;
            if (webView == null)
            {
                Plugin.Log.LogWarning("web view not found");
                return;
            }
            // After a language change the tips in the root page have the old words, also with the hook in place.
            bool wordsChanged = !words.SameAs(tipsWords);
            string tips = TipsScript(words);
            webView.ExecuteJavaScript(Script, (Il2CppSystem.Action<string>)(r =>
            {
                if (r == "already installed" && !wordsChanged) return;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"page script: {r}");
                webView.ExecuteJavaScript(tips, null);
            }));
        }

        private static string tipsScript;
        private static PreviewLogic.Words tipsWords;

        // Sets the tooltip line of each ingredient in the root page, by config ID. The script is kept until the words
        // change. The names are game text, so a backslash or a quote in them is escaped for the script string.
        private static string TipsScript(PreviewLogic.Words words)
        {
            if (tipsScript != null && words.SameAs(tipsWords)) return tipsScript;
            tipsWords = words;
            var sb = new StringBuilder("window.__cookingTips={");
            int count = 0;
            var e = ConfigManager.Instance._Config_Item_Dict.GetEnumerator();
            while (e.MoveNext())
            {
                var item = e.Current.Value;
                if (item == null || item.Category != 1) continue;
                string tip = PreviewLogic.IngredientTip(
                    new[]
                    {
                        (int)Math.Round(item.ValueDisplay1), (int)Math.Round(item.ValueDisplay2), (int)Math.Round(item.ValueDisplay3),
                        (int)Math.Round(item.ValueDisplay4), (int)Math.Round(item.ValueDisplay5),
                    },
                    Reducer_Web_Cooking.ResolveIngredientTier(item.ID), words);
                if (tip == null) continue;
                sb.Append(item.ID).Append(":'").Append(tip.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n")).Append("',");
                count++;
            }
            sb.Append("}");
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"ingredient tips: {count} items");
            return tipsScript = sb.ToString();
        }
    }
}
