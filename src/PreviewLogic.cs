using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectCook
{
    // Game-free logic. This file must not use any game or BepInEx type, because the unit tests compile it alone.
    public static class PreviewLogic
    {
        // Index of a quality level in the chance array and in the game's QualityMap.
        public const int Fail = 0, Normal = 1, Good = 2, Perfect = 3;

        // The words that the mod adds to the cooking window, in one display language.
        public sealed class Words
        {
            // Index is Fail..Perfect.
            public string[] Quality;
            // Index is the game's tier number: 1 High, 2 Mid, 3 Low. Index 0 has no name.
            public string[] Tier;
            // Order of the game's stat array: satiety, mood, energy, health, life.
            public string[] Stat;
            // The label before the tier name. The game has no text for it, so it is the one word that the mod translates.
            public string TierLabel;
            // The labels of the dish card tooltip. The game has no text for them either, so the mod translates them too.
            public string TradeLabel, ExpLabel, RecoveryLabel;

            public bool SameAs(Words other)
            {
                return other != null
                    && TierLabel == other.TierLabel
                    && TradeLabel == other.TradeLabel
                    && ExpLabel == other.ExpLabel
                    && RecoveryLabel == other.RecoveryLabel
                    && string.Join("|", Quality) == string.Join("|", other.Quality)
                    && string.Join("|", Tier) == string.Join("|", other.Tier)
                    && string.Join("|", Stat) == string.Join("|", other.Stat);
            }
        }

        // The game's English texts for these words. They show when the game gives no text for a word.
        public static readonly Words EnglishWords = new Words
        {
            Quality = new[] { "Failed", "Average", "Good", "Perfect" },
            Tier = new[] { null, "High-end", "Mid-tier", "Low-grade" },
            Stat = new[] { "Satiety", "Morale", "Stamina", "Fitness", "Life" },
            TierLabel = "Tier",
            TradeLabel = "Trade value",
            ExpLabel = "Cooking XP",
            RecoveryLabel = "Recovery when eaten",
        };

        // Builds the words from the game texts. keys and texts have the order: quality Fail..Perfect, tier High..Low,
        // then the stat array order. A text that is missing, empty, or equal to its key (the game returns the key for
        // an unknown key) becomes the English word, and its key goes into fellBack. The cell separator and the line
        // break of the line formats cannot be part of a word. chinese selects the tier label of the mod.
        public static Words WordsOrEnglish(string[] keys, string[] texts, bool chinese, out List<string> fellBack)
        {
            fellBack = new List<string>();
            var english = new List<string>(EnglishWords.Quality);
            english.AddRange(new[] { EnglishWords.Tier[1], EnglishWords.Tier[2], EnglishWords.Tier[3] });
            english.AddRange(EnglishWords.Stat);

            var words = new string[english.Count];
            for (int i = 0; i < words.Length; i++)
            {
                string text = texts != null && i < texts.Length ? texts[i] : null;
                if (string.IsNullOrEmpty(text) || text == keys[i])
                {
                    fellBack.Add(keys[i]);
                    text = english[i];
                }
                words[i] = text.Replace('|', ' ').Replace('\n', ' ').Replace('\r', ' ');
            }
            return new Words
            {
                Quality = new[] { words[0], words[1], words[2], words[3] },
                Tier = new[] { null, words[4], words[5], words[6] },
                Stat = new[] { words[7], words[8], words[9], words[10], words[11] },
                TierLabel = chinese ? "档次" : EnglishWords.TierLabel,
                TradeLabel = chinese ? "交易价值" : EnglishWords.TradeLabel,
                ExpLabel = chinese ? "烹饪熟练度" : EnglishWords.ExpLabel,
                RecoveryLabel = chinese ? "食用恢复" : EnglishWords.RecoveryLabel,
            };
        }

        // Chance in percent of each quality level. The game rolls an integer in floor..100 (100 included),
        // adds the bonus, clamps to 0..100, and takes the highest level whose threshold the number reaches.
        public static double[] QualityChances(int floor, int bonus, int[] thresholds)
        {
            var result = new double[4];
            if (thresholds == null || thresholds.Length < 4)
            {
                result[Normal] = 100;
                return result;
            }

            if (floor > 100) floor = 100;
            int total = 100 - floor + 1;
            for (int roll = floor; roll <= 100; roll++)
            {
                int quality = Math.Max(0, Math.Min(100, roll + bonus));
                int level = Fail;
                for (int i = 0; i < 4; i++)
                    if (quality >= thresholds[i]) level = i;
                result[level] += 100.0 / total;
            }
            return result;
        }

        // Quality bonus from the "Practice Makes Perfect" and "Tag Master" talents, each read as the game's own
        // ratio (the sum of the owned levels). The tag bonus applies only to a tag recipe, not an exact recipe.
        public static int TalentQualityBonus(float perfectRatio, float tagRatio, bool isExact)
        {
            int bonus = (int)Math.Round((double)(perfectRatio * 100f));
            if (!isExact) bonus += (int)Math.Round((double)(tagRatio * 100f));
            return bonus;
        }

        // The rotten penalty (a negative number) reduced by the "Mold Master" talent ratio. A ratio at or above 1
        // removes the whole penalty.
        public static int RottenPenalty(int penalty, float reduceRatio)
        {
            return (int)Math.Round((double)(penalty * Math.Max(0f, 1f - reduceRatio)));
        }

        // Cooking EXP for one dish: 0 for the Fail level, else the recipe EXP raised by the "Cooking XP" talent ratio.
        public static int CookExp(int recipeExp, float expRatio, int level)
        {
            if (level == Fail) return 0;
            return (int)Math.Round((double)(recipeExp * (1f + expRatio)));
        }

        // Each seasoning adds less than the one before: base, base x ratio, base x ratio^2, ...
        public static int SeasoningBonus(int count, int baseValue, float ratio)
        {
            float sum = 0, term = baseValue;
            for (int i = 0; i < count; i++)
            {
                sum += term;
                term *= ratio;
            }
            return (int)Math.Round(sum);
        }

        // The game splits a dish into portions when its satiety is above the threshold. Total satiety stays the same.
        public static int Portions(int satiety, int threshold)
        {
            if (threshold <= 0 || satiety <= threshold) return 1;
            return (satiety + threshold - 1) / threshold;
        }

        public sealed class Entry
        {
            public int RecipeId, Tier, Level;
            public bool IsExact;
        }

        // The entries of the game's prediction list. An object without the four fields is not an entry.
        // The field order and unknown fields do not matter.
        public static List<Entry> ReadEntries(string json)
        {
            var list = new List<Entry>();
            foreach (var pair in EntriesWithStart(json)) list.Add(pair.Value);
            return list;
        }

        // Key: the index of the '{' of the entry in the JSON text.
        private static List<KeyValuePair<int, Entry>> EntriesWithStart(string json)
        {
            var list = new List<KeyValuePair<int, Entry>>();
            var objects = FlatJson.ReadArray(json);
            if (objects == null) return list;
            foreach (var obj in objects)
            {
                if (obj.Fields.TryGetValue("RecipeId", out object id) && id is double
                    && obj.Fields.TryGetValue("Tier", out object tier) && tier is double
                    && obj.Fields.TryGetValue("Level", out object level) && level is double
                    && obj.Fields.TryGetValue("IsExact", out object exact) && exact is bool)
                    list.Add(new KeyValuePair<int, Entry>(obj.Start, new Entry
                    {
                        RecipeId = (int)(double)id, Tier = (int)(double)tier, Level = (int)(double)level, IsExact = (bool)exact,
                    }));
            }
            return list;
        }

        // Adds a "Preview" field, and a "PreviewTip" field when tipByRecipeId has a text for the entry's RecipeId.
        // The page ignores unknown fields. Returns the input unchanged when nothing matches or when anything fails.
        public static string AddPreviews(string json, Dictionary<int, string> previewByRecipeId, Dictionary<int, string> tipByRecipeId = null)
        {
            try
            {
                var entries = EntriesWithStart(json);
                var sb = new StringBuilder(json);
                // Last entry first, so an insert does not move the start of an entry that is still to do.
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    int recipeId = entries[i].Value.RecipeId;
                    var fields = new StringBuilder();
                    if (previewByRecipeId.TryGetValue(recipeId, out string preview))
                        fields.Append("\"Preview\":\"").Append(Escape(preview)).Append("\",");
                    if (tipByRecipeId != null && tipByRecipeId.TryGetValue(recipeId, out string tip))
                        fields.Append("\"PreviewTip\":\"").Append(Escape(tip)).Append("\",");
                    if (fields.Length > 0) sb.Insert(entries[i].Key + 1, fields.ToString());
                }
                return entries.Count == 0 ? json : sb.ToString();
            }
            catch (Exception)
            {
                return json;
            }
        }

        private static string Escape(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Same icons as the item tooltip of the game. Order of the game's stat array: satiety, mood, energy, health, life.
        private static readonly string[] StatIcons = { "🍖", "🧠", "⚡", "💚", "❤️" };

        // Tooltip lines of an ingredient: the ingredient tier (the game's tier number: 0 none, 1 High, 2 Mid, 3 Low),
        // then each stat of the raw item that is not 0, with its sign as in the item window of the game.
        // The tier line is "T<tier number>|<label>|<tier name>", so the page can pick the tier color without the word.
        // A trade value above 0 gets a line after the tier.
        public static string IngredientTip(int[] stats, int tier, int tradeValue, Words words)
        {
            var lines = new List<string>();
            if (tier >= 1 && tier <= 3) lines.Add(TierLine(tier, words));
            if (tradeValue > 0) lines.Add($"{words.TradeLabel}: {tradeValue}");
            for (int i = 0; i < words.Stat.Length && i < stats.Length; i++)
                if (stats[i] != 0) lines.Add($"{StatIcons[i]} {words.Stat[i]}: {(stats[i] > 0 ? "+" : "")}{stats[i]}");
            return lines.Count > 0 ? string.Join("\n", lines) : null;
        }

        private static string TierLine(int tier, Words words) => "T" + tier + "|" + words.TierLabel + "|" + words.Tier[tier];

        // Lines of the dish card tooltip: the tier line of the dish in the format of IngredientTip (none for a dish
        // with no tier), the cooking EXP, the trade value, then the lines for the eat-time talents. The EXP is the
        // same for each level but Fail, which gives 0, so it is one line: "+<exp>", with "(<Fail name> 0)" after it
        // when Fail and another level can occur, and "0" when only Fail can occur. With one level the trade value
        // is a plain line also, because the card already names the level. With more levels it is a grid: a header
        // row with an empty label cell and "<level>:<quality name>" cells (highest first), so the page can color
        // each name, then a row with the label and one value for each level. Only the grid rows have '|' cells.
        public static List<string> TipLines(double[] chances, int[] tradeValues, int exp, int tier, float nourishRatio, int perfectMorale, Words words)
        {
            var header = new StringBuilder();
            var trade = new StringBuilder(words.TradeLabel);
            int levels = 0, onlyLevel = Fail;
            for (int level = Perfect; level >= Fail; level--)
            {
                if (chances[level] <= 0) continue;
                levels++;
                onlyLevel = level;
                header.Append('|').Append(level).Append(':').Append(words.Quality[level]);
                trade.Append('|').Append(tradeValues[level]);
            }
            bool failOnly = levels == 1 && onlyLevel == Fail;
            string failNote = chances[Fail] > 0 && !failOnly ? $" ({words.Quality[Fail]} 0)" : "";
            var lines = new List<string>();
            if (tier >= 1 && tier <= 3) lines.Add(TierLine(tier, words));
            lines.Add($"{words.ExpLabel}: {(failOnly ? "0" : "+" + exp + failNote)}");
            if (levels > 1)
            {
                lines.Add(header.ToString());
                lines.Add(trade.ToString());
            }
            else lines.Add($"{words.TradeLabel}: {tradeValues[onlyLevel]}");
            if (nourishRatio > 0)
                lines.Add($"{words.RecoveryLabel}: +{(int)Math.Round(nourishRatio * 100)}%");
            if (perfectMorale > 0 && chances[Perfect] > 0)
                lines.Add($"{words.Quality[Perfect]} {StatIcons[1]} {words.Stat[1]}: +{perfectMorale}");
            return lines;
        }

        // One line for each quality level that can occur, highest level first. stats[level] is the stat array of that level.
        // A line is cells with '|' between them, and all lines have the same cells, so the page can align them as columns.
        // A stat or the portion count gets a cell when any shown level has a value for it.
        // The first cell is the quality level number, for the color of the name. The page does not show it.
        public static List<string> Lines(double[] chances, int[][] stats, int[] portions, Words words)
        {
            var statUsed = new bool[StatIcons.Length];
            bool portionsUsed = false;
            for (int level = Fail; level <= Perfect; level++)
            {
                if (chances[level] <= 0) continue;
                for (int i = 0; i < StatIcons.Length && i < stats[level].Length; i++)
                    if (stats[level][i] != 0) statUsed[i] = true;
                if (portions[level] > 1) portionsUsed = true;
            }

            var lines = new List<string>();
            for (int level = Perfect; level >= Fail; level--)
            {
                if (chances[level] <= 0) continue;
                int percent = Math.Max(1, (int)Math.Round(chances[level]));
                var sb = new StringBuilder($"{level}|{words.Quality[level]}|{percent}%");
                for (int i = 0; i < StatIcons.Length; i++)
                    if (statUsed[i]) sb.Append('|').Append(StatIcons[i]).Append(i < stats[level].Length ? stats[level][i] : 0);
                if (portionsUsed) sb.Append("|x").Append(portions[level]);
                lines.Add(sb.ToString());
            }
            return lines;
        }
    }
}