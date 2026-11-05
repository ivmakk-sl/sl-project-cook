using System;
using System.Collections.Generic;
using System.Text;
using SlShared.Json;

namespace ProjectCook
{
    // Game-free logic of the row split: each row of the cooking station is its own group of ingredients. This file
    // must not use any game or BepInEx type, because the unit tests compile it alone.
    public static class RowSplitLogic
    {
        // An item of the cooking station: its instance id, config id, sub category, and its cells (X, Y of the
        // top-left cell, W x H cells). Y = 0 is the top row.
        public readonly struct Item
        {
            public readonly long Id;
            public readonly int ConfigId, SubCategory, X, Y, W, H;
            public Item(long id, int configId, int subCategory, int x, int y, int w, int h)
            {
                Id = id; ConfigId = configId; SubCategory = subCategory; X = x; Y = y; W = w; H = h;
            }
        }

        public sealed class Row
        {
            public int Y;
            public List<Item> Items = new List<Item>();
        }

        // The rows with items, top row first. An item belongs to the row of its top cell. The order of the items
        // inside a row is the order of the list.
        public static List<Row> Rows(IEnumerable<Item> items)
        {
            var byY = new SortedDictionary<int, Row>();
            foreach (var item in items)
            {
                if (!byY.TryGetValue(item.Y, out var row)) byY[item.Y] = row = new Row { Y = item.Y };
                row.Items.Add(item);
            }
            return new List<Row>(byY.Values);
        }

        // Row split applies with the switch on, from the game's multi-dish level, not in hot pot mode, and with the
        // items in two or more rows. Else the game's own split applies.
        public static bool Applies(bool rowSplitOn, int cookingLevel, int unlockLevel, bool hotPot, int rowCount)
            => rowSplitOn && cookingLevel >= unlockLevel && !hotPot && rowCount >= 2;

        // CookingDishPriority.Exact, the cooking mode Separate (State_Web_Cooking.DishPriority, game 1.1.20719).
        public const int SeparateMode = 1;

        // Reducer_Web_Cooking.UseExactFirst: the match puts first the recipes with specific ingredients only in Separate,
        // from the unlock level of the cooking modes (GetPickDishUnlockLevel, 0 when the modes are off).
        public static bool ExactFirst(int cookingLevel, int unlockLevel, int dishPriority)
            => unlockLevel > 0 && cookingLevel >= unlockLevel && dishPriority == SeparateMode;

        // A matched recipe for the compare of the two cooking modes: its id, its tier (0 for an exact recipe), and its
        // tag combo (the food types of a tag recipe, null for none).
        public readonly struct RecipeKey
        {
            public readonly int Id, Tier;
            public readonly int[] TagCombo;

            public RecipeKey(int id, int tier, int[] tagCombo) { Id = id; Tier = tier; TagCombo = tagCombo; }
        }

        // Reducer_Web_Cooking.IsSameDish: the same recipe, or two tiered recipes with the same food types and counts.
        public static bool IsSameDish(RecipeKey a, RecipeKey b)
        {
            if (a.Id == b.Id) return true;
            return a.Tier > 0 && b.Tier > 0 && SameTagCombo(a.TagCombo, b.TagCombo);
        }

        private static bool SameTagCombo(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            var counts = new Dictionary<int, int>();
            foreach (int t in a) counts[t] = (counts.TryGetValue(t, out int n) ? n : 0) + 1;
            foreach (int t in b)
            {
                if (!counts.TryGetValue(t, out int n) || n == 0) return false;
                counts[t] = n - 1;
            }
            return true;
        }

        // Reducer_Web_Cooking.AlignTagDishTierToMix for the dishes of Separate: with as many dishes as Mixed Cooking,
        // each dish in order takes the recipe of its first same dish of Mixed Cooking, which no earlier dish took (so a
        // tag dish gets the tier of Mixed Cooking); the first dish with no same dish ends the compare. Gives the recipe
        // ids of the dishes in order.
        public static int[] AlignToMix(IReadOnlyList<RecipeKey> separate, IReadOnlyList<RecipeKey> mixed)
        {
            var ids = new int[separate.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = separate[i].Id;
            if (mixed == null || mixed.Count != separate.Count) return ids;
            var left = new List<RecipeKey>(mixed);
            for (int i = 0; i < ids.Length; i++)
            {
                var dish = separate[i];
                int at = left.FindIndex(m => IsSameDish(dish, m));
                if (at < 0) break;
                ids[i] = left[at].Id;
                left.RemoveAt(at);
            }
            return ids;
        }

        public sealed class ItemCounts
        {
            public Dictionary<int, int> Ids = new Dictionary<int, int>();
            public Dictionary<int, int> Tags = new Dictionary<int, int>();
        }

        // The counts of a row by config id and by sub category, as the client counts them: one for each item (no food
        // item can stack). The keys are in the order of the items.
        public static ItemCounts Counts(Row row)
        {
            var counts = new ItemCounts();
            foreach (var item in row.Items)
            {
                counts.Ids.TryGetValue(item.ConfigId, out int id);
                counts.Ids[item.ConfigId] = id + 1;
                counts.Tags.TryGetValue(item.SubCategory, out int tag);
                counts.Tags[item.SubCategory] = tag + 1;
            }
            return counts;
        }

        // The indexes of the items, top row first, with the list order kept inside a row. The game's entry builder
        // gives a tag the first fitting item in the order of the list, so this order gives the dish of the top row
        // the items of the top row first.
        public static List<int> RowOrder(IList<Item> items)
        {
            var order = new List<int>();
            foreach (var row in Rows(Indexed(items)))
                foreach (var item in row.Items) order.Add((int)item.Id);
            return order;
        }

        // The count of the recipe ids that each row takes, top row first, or null when the ids do not split by row.
        // built(row, offset, k) is the entry count of the game's entry builder for the items of that row and the k ids
        // from offset. The builder skips an id that does not match, so a row takes the longest run of the next ids
        // that the builder takes all of. Null when ids are left after the last row, or when no row takes an id.
        public static int[] SplitIds(int rowCount, int idCount, Func<int, int, int, int> built)
        {
            var counts = new int[rowCount];
            int offset = 0;
            for (int row = 0; row < rowCount; row++)
            {
                int k = 0;
                while (offset + k < idCount && built(row, offset, k + 1) == k + 1) k++;
                counts[row] = k;
                offset += k;
            }
            return offset == 0 || offset != idCount ? null : counts;
        }

        // The items with the list index in place of the id.
        private static IEnumerable<Item> Indexed(IList<Item> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                yield return new Item(i, it.ConfigId, it.SubCategory, it.X, it.Y, it.W, it.H);
            }
        }

        // The counts that give the dish tier of a dish, as the settle makes them: for a tag recipe a copy of the
        // participated counts with one added for each seasoning item of the whole cooking station, also when the
        // recipe uses that seasoning. The stats of the dish come from the participated counts alone.
        public static Dictionary<int, int> WithSeasonings(Dictionary<int, int> participated, IEnumerable<int> seasoningIds, bool isExact)
        {
            var counts = new Dictionary<int, int>(participated);
            if (isExact) return counts;
            foreach (int id in seasoningIds)
            {
                counts.TryGetValue(id, out int n);
                counts[id] = n + 1;
            }
            return counts;
        }

        // One entry of the game's prediction list (CookingPredictionInfo).
        public sealed class PredictionEntry
        {
            public int RecipeId, Tier, Level;
            public bool IsExact;
            public string Name, Icon;
        }

        // The prediction list with the fields of the game, in the game's field order.
        public static string PredictionJson(List<PredictionEntry> entries)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"RecipeId\":").Append(JsonText.Num(e.RecipeId))
                  .Append(",\"Name\":").AppendStr(e.Name)
                  .Append(",\"Tier\":").Append(JsonText.Num(e.Tier))
                  .Append(",\"Level\":").Append(JsonText.Num(e.Level))
                  .Append(",\"IsExact\":").Append(e.IsExact ? "true" : "false")
                  .Append(",\"Icon\":").AppendStr(e.Icon)
                  .Append('}');
            }
            return sb.Append(']').ToString();
        }

        // The cooking station and the items of each row, so the start can check that the plan of the preview is of
        // the same items in the same rows. The client and the authority list the items in their own order, so the
        // ids of a row are sorted.
        public static string Signature(long ownerId, List<Row> rows)
        {
            var sb = new StringBuilder().Append(ownerId);
            foreach (var row in rows)
            {
                var ids = new List<long>();
                foreach (var item in row.Items) ids.Add(item.Id);
                ids.Sort();
                sb.Append('|').Append(row.Y).Append(':').Append(string.Join(",", ids));
            }
            return sb.ToString();
        }
    }
}
