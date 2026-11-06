using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Vuplex.WebView.Internal;

namespace ProjectCook
{
    // The messages of the page script (window.vuplex.postMessage('slmod|projectcook|<text>')). The Prefix takes only the
    // messages with the prefix of this mod, so the game's handler and the other mods never get them.
    [HarmonyPatch(typeof(BaseWebView), "HandleMessageEmitted")]
    internal static class PageMessages
    {
        private static bool Prefix(string serializedMessage)
        {
            if (serializedMessage == null || !serializedMessage.StartsWith(PageJson.MessagePrefix, StringComparison.Ordinal))
                return true;
            try
            {
                if (serializedMessage.Substring(PageJson.MessagePrefix.Length) == PageJson.RowSplitMessage) ToggleRowSplit();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Project Cook page message failed: {e}");
            }
            return false;
        }

        // A click on the toggle: with auto off it ends auto off and leaves the config entry, else it flips the entry. The
        // overlay of a cook covers the toggle, so the check of a cook is a second guard.
        private static void ToggleRowSplit()
        {
            var state = RowPlanner.WindowState();
            if (state == null || (state.IsCooking != null && state.IsCooking.Value)) return;
            bool endedAuto = Plugin.RowSplit.Value && RowPlanner.AutoOff.Click(state.CurrentFurnitureId);
            // A flip reaches the window through Plugin.OnSettingChanged.
            if (endedAuto) Ac_Cooking_RefreshBag.SendAction();
            else Plugin.RowSplit.Value = !Plugin.RowSplit.Value;
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"row split toggle on={Plugin.RowSplit.Value} auto={endedAuto}");
        }
    }
}
