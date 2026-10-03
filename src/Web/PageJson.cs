using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SlShared.Json;

namespace ProjectCook
{
    // The JSON text that the plugin sends to the page script, built by hand: netstandard2.1 has no
    // System.Text.Json, and the game's own Newtonsoft.Json is not usable from mod code. No game or
    // BepInEx type here.
    public static class PageJson
    {
        // The data of setData: the tooltip lines of each ingredient and the tier of each ingredient that the
        // page marks (High 1 and Low 3), by config ID.
        public static string DataJson(IEnumerable<KeyValuePair<int, string>> tips, IEnumerable<KeyValuePair<int, int>> tiers)
        {
            var sb = new StringBuilder("{\"tips\":{");
            bool first = true;
            foreach (var kv in tips)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(kv.Key).Append("\":").AppendStr(kv.Value);
            }
            sb.Append("},\"tiers\":{");
            first = true;
            foreach (var kv in tiers)
            {
                if (kv.Value != 1 && kv.Value != 3) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(kv.Key).Append("\":").Append(kv.Value);
            }
            sb.Append("}}");
            return sb.ToString();
        }

        // The result of a command when the root page has no page script: a new root page, or one that
        // the game built again after a browser crash.
        public const string NoScript = "no script";

        // The start of the result of a pass that found a storage window and no Cooking frame (install.ts). The
        // storage window shows by itself, so such a pass needs no retry.
        public const string StorageResultPrefix = "storage: ";

        // One pass with the data that the page script already has: sent at a prediction refresh when the
        // data did not change.
        public const string ApplyCommand = "window.__projectCook?window.__projectCook.apply():'" + NoScript + "'";

        // The push of the data when the page script is already in the root page.
        // With sort data, setData stores it too, before its one pass.
        public static string SetDataCommand(string json, string sortJson = null) =>
            "window.__projectCook?window.__projectCook.setData(" + json + (sortJson == null ? "" : "," + sortJson) + "):'" + NoScript + "'";

        // The push of the numbers of the food sort alone: no page script and no ingredient data.
        public static string SetSortDataCommand(string sortJson) =>
            "window.__projectCook?window.__projectCook.setSortData(" + sortJson + "):'" + NoScript + "'";

        // The page script, then the push of the data: sent only when a command gave NoScript.
        public static string SetDataWithScriptCommand(string script, string json, string sortJson = null) =>
            script + ";window.__projectCook.setData(" + json + (sortJson == null ? "" : "," + sortJson) + ");";

        // The data of setSortData: the owner of the open storage, the words of the dropdown, and the numbers of each
        // item by its logic id. "n" is Satiety, Morale, Stamina, Life, the trade value, and the sort key of the days
        // (SortLogic.DaysKey: below 0 for an expired item), with null for no number. "d" is the text of the days
        // badge: the days left, or for an expired item the days until it spoils, the expired word, or the rotten word.
        // "c" is the config id: a tie of the numbers groups the same items. "bag" is the owner of the Backpack side of
        // the storage window, whose items are in "items" too (none in the cooking window).
        public static string SortDataJson(long owner, IEnumerable<KeyValuePair<long, SortLogic.Numbers>> items, SortLogic.Words words, long bag = 0)
        {
            var sb = new StringBuilder("{\"owner\":").AppendStr(owner.ToString(CultureInfo.InvariantCulture));
            if (bag != 0) sb.Append(",\"bag\":").AppendStr(bag.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"words\":{\"choices\":[");
            for (int i = 0; i < words.Choices.Length; i++) sb.Append(i == 0 ? "" : ",").AppendStr(words.Choices[i]);
            sb.Append("],\"expired\":").AppendStr(words.Expired).Append(",\"sort\":").AppendStr(words.Sort).Append("},\"items\":{");
            bool first = true;
            foreach (var kv in items)
            {
                if (!first) sb.Append(',');
                first = false;
                var n = kv.Value;
                sb.Append('"').Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append("\":{\"n\":[");
                foreach (var stat in n.Stats) sb.Append(JsonText.Num(stat)).Append(',');
                sb.Append(JsonText.Num(n.Trade)).Append(',');
                var days = n.Days;
                var key = SortLogic.DaysKey(days);
                sb.Append(key.HasValue ? key.Value.ToString("0.###", CultureInfo.InvariantCulture) : "null");
                sb.Append("],\"d\":");
                if (days.IsRotten) sb.AppendStr(words.Rotten);
                else if (days.HasNumber) sb.AppendStr(SortLogic.DaysText(days.Days, words.DayUnit));
                else if (days.IsExpired) sb.AppendStr(words.Expired);
                else sb.Append("null");
                sb.Append(",\"c\":").Append(n.ConfigId.ToString(CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            return sb.Append("}}").ToString();
        }
    }
}
