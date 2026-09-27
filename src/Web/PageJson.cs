using System.Collections.Generic;
using System.Text;

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
                sb.Append('"').Append(kv.Key).Append("\":").Append(Str(kv.Value));
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

        // One pass with the data that the page script already has: sent at a prediction refresh when the
        // data did not change.
        public const string ApplyCommand = "window.__projectCook?window.__projectCook.apply():'" + NoScript + "'";

        // The push of the data when the page script is already in the root page.
        public static string SetDataCommand(string json) =>
            "window.__projectCook?window.__projectCook.setData(" + json + "):'" + NoScript + "'";

        // The page script, then the push of the data: sent only when a command gave NoScript.
        public static string SetDataWithScriptCommand(string script, string json) =>
            script + ";window.__projectCook.setData(" + json + ");";

        // A JSON string literal, with its quotes. Also a JavaScript string literal.
        internal static string Str(string s)
        {
            var sb = new StringBuilder((s ?? "").Length + 2);
            sb.Append('"');
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
