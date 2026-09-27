using System;
using System.Collections.Generic;
using HarmonyLib;
using GameCore.HotUpdate.ReduxUI;
using UnityEngine;

namespace ProjectCook
{
    // The per-frame hook: runs the schedule of the sends to the page script. WebUILayer.OnUpdate is the per-frame
    // update of the web UI layer, called by ReduxUISystem.OnUpdateAction with no pause check. The cooking window
    // pauses the world, and the per-frame updates of the world (ActionManager.Update) stop while it is open, so the
    // tick must be a web UI one. It returns at once while no request and no retry is pending.
    [HarmonyPatch(typeof(WebUILayer), "OnUpdate")]
    internal static class PageTick
    {
        private static readonly PushSchedule schedule = new PushSchedule();
        private static readonly HashSet<string> warned = new HashSet<string>();

        // A prediction refresh: the next tick sends once, however many refreshes come in the frame.
        public static void Request() => schedule.Request(Time.realtimeSinceStartup);

        public static void OnResult(PushSchedule.Step step, string result) =>
            schedule.OnResult(step, result, Time.realtimeSinceStartup);

        private static void Postfix()
        {
            try
            {
                var step = schedule.Tick(Time.realtimeSinceStartup, () => IngredientData.Json(GameWords.Current()));
                if (step.Kind != PushSchedule.Kind.None) PageScript.Send(step);
            }
            catch (Exception e)
            {
                // A failure can repeat on each frame, so each distinct text logs once.
                if (warned.Add(e.Message)) Plugin.Log.LogWarning($"Project Cook page tick failed: {e}");
            }
        }
    }

    // The refresh of a window open comes one frame before the Cooking page is ready, so its send finds no Cooking
    // frame. When the page tells the game that it is ready, the game calls CallShowAction of the page's view model,
    // and a request then makes the next tick send while the frame is there, with no wait for the retry.
    // WebUILayer.OnPageReady is inlined into WebUILayer.OnMessageFromJS, so a patch of it never runs; this call is
    // virtual, so it cannot be inlined.
    [HarmonyPatch(typeof(WebUI_Cooking), "CallShowAction")]
    internal static class PageTickOnCallShowAction
    {
        private static void Postfix()
        {
            try
            {
                PageTick.Request();
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug("ProjectCook page ready: Cooking");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook page ready failed: {e}");
            }
        }
    }
}
