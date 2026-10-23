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
        // page marks (High 1 and Low 3), by config ID, the names of the page features that are on in the config, and the
        // labels of the portion switch.
        public static string DataJson(IEnumerable<KeyValuePair<int, string>> tips, IEnumerable<KeyValuePair<int, int>> tiers, IEnumerable<string> features = null, string[] portion = null)
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
            sb.Append('}');
            if (features != null)
            {
                sb.Append(",\"features\":[");
                first = true;
                foreach (var name in features)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.AppendStr(name);
                }
                sb.Append(']');
            }
            if (portion != null)
            {
                sb.Append(",\"portion\":[");
                for (int i = 0; i < portion.Length; i++) sb.Append(i == 0 ? "" : ",").AppendStr(portion[i]);
                sb.Append(']');
            }
            sb.Append('}');
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

        // The push of the icon tab list of an open (IconTabsJson): the page script keeps it for the frame of its window.
        public static string SetIconTabsCommand(string json) =>
            "window.__projectCook?window.__projectCook.setIconTabs(" + json + "):'" + NoScript + "'";

        // The icon tab list of the open of a food window: the page id of the window, the open id, and each tab in the
        // order of the page reducer with its name, the bag mark, its icon path, the short text of a tab with no icon or
        // an icon that does not load, and the game's lock text of a locked tab.
        public static string IconTabsJson(string page, int openId, IEnumerable<IconTabsLogic.Entry> entries)
        {
            var sb = new StringBuilder("{\"page\":").AppendStr(page).Append(",\"open\":").Append(openId).Append(",\"tabs\":[");
            bool first = true;
            foreach (var e in entries)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"name\":").AppendStr(e.Name ?? "");
                if (e.Bag) sb.Append(",\"bag\":true");
                if (!string.IsNullOrEmpty(e.Icon)) sb.Append(",\"icon\":").AppendStr(e.Icon);
                sb.Append(",\"text\":").AppendStr(e.Text ?? "");
                if (e.Lock != null) sb.Append(",\"lock\":").AppendStr(e.Lock);
                sb.Append('}');
            }
            return sb.Append("]}").ToString();
        }

        // The pages of the part "icontabs: mismatch <page>,<page>" of a pass result: the tab row of the page did not
        // match the icon tab list, so the window shows the name tabs.
        public static string[] IconTabMismatches(string result)
        {
            const string marker = "icontabs: mismatch ";
            int at = result == null ? -1 : result.IndexOf(marker, System.StringComparison.Ordinal);
            if (at < 0) return new string[0];
            int start = at + marker.Length;
            int end = result.IndexOf(';', start);
            return result.Substring(start, (end < 0 ? result.Length : end) - start).Split(',');
        }

        // The page script, then the push of the data: sent only when a command gave NoScript.
        public static string SetDataWithScriptCommand(string script, string json, string sortJson = null) =>
            script + ";window.__projectCook.setData(" + json + (sortJson == null ? "" : "," + sortJson) + ");";

        // The page script, then the icon tab list: sent only when SetIconTabsCommand gave NoScript (the first open of a
        // food window before the data push).
        public static string SetIconTabsWithScriptCommand(string script, string json) =>
            script + ";window.__projectCook.setIconTabs(" + json + ");";

        // The data of setSortData: the owner of the open storage, the words of the dropdown, and the numbers of each
        // item by its logic id. "n" is Satiety, Morale, Stamina, Life, the trade value, the sort key of the days
        // (SortLogic.DaysKey: below 0 for an expired item), and the cage satiety, with null for no number. "d" is the text of the days
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
                sb.Append(',').Append(JsonText.Num(n.Cage));
                sb.Append("],\"d\":");
                if (days.IsRotten) sb.AppendStr(words.Rotten);
                else if (days.HasNumber) sb.AppendStr(SortLogic.DaysText(days.Days, words.DaysLeft));
                else if (days.IsExpired) sb.AppendStr(words.Expired);
                else sb.Append("null");
                sb.Append(",\"c\":").Append(n.ConfigId.ToString(CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            return sb.Append("}}").ToString();
        }
    }
}
