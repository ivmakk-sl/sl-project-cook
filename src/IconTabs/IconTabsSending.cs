using UnityEngine;

namespace ProjectCook
{
    // The send of the icon tab list of the newest open (IconTabsSend) on the per-frame tick of PageTick.
    internal static class IconTabsSending
    {
        private static readonly IconTabsSend send = new IconTabsSend();

        public static void Request(string json) => send.Request(json, Time.realtimeSinceStartup);

        // Called on each frame: returns at once while no list waits.
        public static void Tick()
        {
            var json = send.Tick(Time.realtimeSinceStartup);
            if (json != null) PageScript.SendIconTabs(json);
        }

        public static void OnResult(string json, string result) => send.OnResult(json, result, Time.realtimeSinceStartup);
    }
}
