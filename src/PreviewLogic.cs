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

            public bool SameAs(Words other)
            {
                return other != null
                    && TierLabel == other.TierLabel
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

        // Adds a "Preview" field to each entry whose RecipeId has a text. The page ignores unknown fields.
        // Returns the input unchanged when nothing matches or when anything fails.
        public static string AddPreviews(string json, Dictionary<int, string> previewByRecipeId)
        {
            try
            {
                var entries = EntriesWithStart(json);
                var sb = new StringBuilder(json);
                // Last entry first, so an insert does not move the start of an entry that is still to do.
                for (int i = entries.Count - 1; i >= 0; i--)
                    if (previewByRecipeId.TryGetValue(entries[i].Value.RecipeId, out string text))
                        sb.Insert(entries[i].Key + 1, "\"Preview\":\"" + Escape(text) + "\",");
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
        public static string IngredientTip(int[] stats, int tier, Words words)
        {
            var lines = new List<string>();
            if (tier >= 1 && tier <= 3) lines.Add("T" + tier + "|" + words.TierLabel + "|" + words.Tier[tier]);
            for (int i = 0; i < words.Stat.Length && i < stats.Length; i++)
                if (stats[i] != 0) lines.Add($"{StatIcons[i]} {words.Stat[i]}: {(stats[i] > 0 ? "+" : "")}{stats[i]}");
            return lines.Count > 0 ? string.Join("\n", lines) : null;
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