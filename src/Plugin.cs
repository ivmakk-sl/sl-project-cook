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
    [BepInPlugin(PluginGuid, "Project Cook", "1.1.0")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.projectcook";

        internal static new ManualLogSource Log;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> IgnoreCookingTalents;

        public override void Load()
        {
            Log = base.Log;
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Log each preview (quality inputs, chances, stats, portions), each cooked result (quality roll and stats), and the page script state. Use it to compare the preview with the real result. Keep off in normal play.");
            IgnoreCookingTalents = Config.Bind(
                "Debug", "IgnoreCookingTalents", false,
                "For tests only. The game and the preview read each cooking talent as 0, so dishes of lower quality can occur with a character that has the talents. It changes the real cooking results while it is on. The save does not change. Needs a game restart.");
            var harmony = new Harmony(PluginGuid);
            // One patch that fails to attach must not stop the others.
            foreach (var type in new[] { typeof(RefreshPredictionPatch), typeof(RollCookingQualityLog), typeof(CalcProductVDLog), typeof(AddCookExpLog) })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogError($"patch {type.Name} failed: {e.Message}"); }
            }
            if (IgnoreCookingTalents.Value)
            {
                try
                {
                    harmony.CreateClassProcessor(typeof(IgnoreCookingTalentsPatch)).Patch();
                    Log.LogWarning("Debug option IgnoreCookingTalents is on: the cooking talents of the character have no effect.");
                }
                catch (Exception e) { Log.LogError($"patch {nameof(IgnoreCookingTalentsPatch)} failed: {e.Message}"); }
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

                var previews = Preview.Build(state, entries, workbenchIds, workbenchTags, words, out var tips);
                if (previews.Count == 0) return;

                state.PredictionListJson.Value = PreviewLogic.AddPreviews(json, previews, tips);
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

    // The talent and buff ratios that the quality and EXP math need, read once for each refresh: the game's own
    // method gives the sum of the owned talents and any temporary buff. A failed read counts as 0 and warns once.
    internal struct TalentInputs
    {
        public float PerfectQuality, TagQuality, RottenReduce, ExpRatio, DishNourish, EatPerfectMorale;

        private static bool warned;

        public static TalentInputs Read()
        {
            return new TalentInputs
            {
                PerfectQuality = Ratio("Buff/AE_PerfectQualityMultiplier"),
                TagQuality = Ratio("Buff/AE_CookTagQualityBonus"),
                RottenReduce = Ratio("Buff/AE_CookRottenPenaltyReduce"),
                ExpRatio = Ratio("Buff/AE_CookExpMultiplier"),
                DishNourish = Ratio("Buff/AE_CookDishNourish"),
                EatPerfectMorale = Ratio("Buff/AE_CookEatPerfectMorale"),
            };
        }

        private static float Ratio(string key)
        {
            try { return Furniture.GetTalentEffectRatio(key); }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning($"talent read of {key} failed, treated as 0: {e.Message}");
                }
                return 0f;
            }
        }
    }

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
            int perfectMorale = (int)Math.Round((double)talents.EatPerfectMorale);
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
                var portions = new int[4];
                var tradeValues = new int[4];
                var exps = new int[4];
                Calculating = true;
                try
                {
                    for (int level = 0; level < 4; level++)
                    {
                        var vd = CookingFormula.CalcProductVD(participated, (CookingFormula.CookingTier)entry.Tier, FormulaQualityTier[level], productIds[level], entry.IsExact);
                        stats[level] = new int[vd.Length];
                        for (int i = 0; i < vd.Length; i++) stats[level][i] = (int)Math.Round(vd[i]);
                        portions[level] = PreviewLogic.Portions(stats[level][0], recipe.SatietyStandard > 0 ? recipe.SatietyStandard : Setting(config, "CookingSatiety_SplitThreshold"));
                        tradeValues[level] = config.Get_Config_Item(productIds[level])?.TradeValue ?? 0;
                        exps[level] = PreviewLogic.CookExp(recipe.CookExp, talents.ExpRatio, level);
                    }
                }
                finally
                {
                    Calculating = false;
                }

                var lines = PreviewLogic.Lines(chances, stats, portions, words);
                result[recipeId] = string.Join("\n", lines);
                var tipLines = PreviewLogic.TipLines(chances, tradeValues, exps[PreviewLogic.Perfect], entry.Tier, talents.DishNourish, perfectMorale, words);
                tips[recipeId] = string.Join("\n", tipLines);
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"preview recipe={recipeId} tier={entry.Tier} exact={entry.IsExact} {inputs} bonus={bonus} talents perfect={talents.PerfectQuality} tag={talents.TagQuality} rotten={talents.RottenReduce} exp={talents.ExpRatio} nourish={talents.DishNourish} perfectMorale={talents.EatPerfectMorale} | {string.Join(" | ", lines)} | all levels F/N/G/P sat={stats[0][0]}/{stats[1][0]}/{stats[2][0]}/{stats[3][0]} portions={string.Join("/", portions)} trade={tradeValues[0]}/{tradeValues[1]}/{tradeValues[2]}/{tradeValues[3]} exp={exps[0]}/{exps[1]}/{exps[2]}/{exps[3]}");
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

    // Log of the cooking EXP that the game actually awards. Furniture.SettleCookingResult calls this to give the
    // cooking EXP for a cooked dish. It is not known if the game calls it once for each dish or once for the pot
    // with the sum, so the entry logs the raw argument.
    [HarmonyPatch(typeof(CookingRecordComponent), "AddCookExp")]
    internal static class AddCookExpLog
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

    // Attached only when the debug option IgnoreCookingTalents is on. The game reads each talent effect through the
    // overloads of this method, the quality roll and the EXP grant included, so the real results and the preview
    // stay equal. The last argument of each overload is the effect key.
    [HarmonyPatch]
    internal static class IgnoreCookingTalentsPatch
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

    // The cooking page draws each card with a fixed name line and hint line. The page ignores unknown entry fields,
    // so the mod sends the preview text in a "Preview" field and this script adds it to the card as extra lines.
    // The install script itself lives in page.js (an embedded resource), so it can be edited and tested as a file.
    internal static class PageScript
    {
        private static string script;
        private static readonly HashSet<string> loggedMissing = new HashSet<string>();
        private static readonly HashSet<string> loggedErrors = new HashSet<string>();

        private static string Script()
        {
            if (script != null) return script;
            var assembly = typeof(PageScript).Assembly;
            string name = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith("page.js", StringComparison.Ordinal));
            using (var stream = assembly.GetManifestResourceStream(name))
            using (var reader = new System.IO.StreamReader(stream))
                script = reader.ReadToEnd();
            return script;
        }

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
            webView.ExecuteJavaScript(Script(), (Il2CppSystem.Action<string>)(r =>
            {
                LogPageCheck(r);
                if (r.StartsWith("already installed", StringComparison.Ordinal) && !wordsChanged) return;
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"page script: {r}");
                // The tier table arrives after the install, so the grids draw their items again to get the tier rings.
                webView.ExecuteJavaScript(tips + ";" + RedrawTiers, null);
            }));
        }

        // The result of page.js has an optional "; missing: ..." part (one text for each distinct set of missing
        // parts) and an optional "; errors: text1 || text2" part (the errors that page.js has not reported before).
        // Each is logged one time, with Verbose off also, so a game update or a page bug is not silent.
        private static void LogPageCheck(string r)
        {
            if (r == null) return;
            const string missingMarker = "; missing: ";
            const string errorsMarker = "; errors: ";
            int missingAt = r.IndexOf(missingMarker, StringComparison.Ordinal);
            int errorsAt = r.IndexOf(errorsMarker, StringComparison.Ordinal);
            if (missingAt >= 0)
            {
                int end = errorsAt >= 0 ? errorsAt : r.Length;
                string missing = r.Substring(missingAt + missingMarker.Length, end - missingAt - missingMarker.Length);
                if (loggedMissing.Add(missing)) Plugin.Log.LogWarning($"page check: missing {missing}");
            }
            if (errorsAt >= 0)
            {
                string errors = r.Substring(errorsAt + errorsMarker.Length);
                foreach (var text in errors.Split(new[] { " || " }, StringSplitOptions.None))
                    if (loggedErrors.Add(text)) Plugin.Log.LogError($"page script error: {text}");
            }
        }

        private const string RedrawTiers =
            "(function(){var f=document.querySelectorAll('iframe');for(var i=0;i<f.length;i++){try{var w=f[i].contentWindow;" +
            "if(w&&typeof w.__cookingRedrawTiers==='function'&&!w.__cookingTiersDrawn)w.__cookingRedrawTiers();}catch(e){}}})()";

        private static string tipsScript;
        private static PreviewLogic.Words tipsWords;

        // Sets the tooltip line of each ingredient in the root page, by config ID, and the tier of each ingredient
        // that has one (for the tier ring). The script is kept until the words change. The names are game text,
        // so a backslash or a quote in them is escaped for the script string.
        private static string TipsScript(PreviewLogic.Words words)
        {
            if (tipsScript != null && words.SameAs(tipsWords)) return tipsScript;
            tipsWords = words;
            var sb = new StringBuilder("window.__cookingTips={");
            var tiers = new StringBuilder("window.__cookingTiers={");
            int count = 0;
            int tierCount = 0;
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
                    tier, item.TradeValue, words);
                if (tip != null)
                {
                    sb.Append(item.ID).Append(":'").Append(tip.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n")).Append("',");
                    count++;
                }
                if (tier >= 1 && tier <= 3)
                {
                    tiers.Append(item.ID).Append(':').Append(tier).Append(',');
                    tierCount++;
                }
            }
            sb.Append("}");
            tiers.Append("}");
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"ingredient tips: {count} items, {tierCount} with a tier");
            return tipsScript = sb.ToString() + ";" + tiers.ToString();
        }
    }
}
