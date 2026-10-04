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
