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

        // New numbers of the food sort for the open window: the next tick sends them when they changed.
        public static void RequestSort(string json) => schedule.RequestSort(json);

        public static void OnResult(PushSchedule.Step step, string result) =>
            schedule.OnResult(step, result, Time.realtimeSinceStartup);

        // Built at the first use, after Load has set the page features, and again for a new book data.
        private static string noIngredientData;
        private static PageJson.Book noIngredientBook;
        private static string[] noIngredientFeatures;
        private static string NoIngredientData(PageJson.Book book)
        {
            if (noIngredientData == null || !ReferenceEquals(book, noIngredientBook) || !ReferenceEquals(Plugin.PageFeatures, noIngredientFeatures))
            {
                noIngredientData = PageJson.DataJson(new KeyValuePair<int, string>[0], new KeyValuePair<int, int>[0], Plugin.PageFeatures, null, book);
                noIngredientBook = book;
                noIngredientFeatures = Plugin.PageFeatures;
            }
            return noIngredientData;
        }

        // The time of the last data build, for the timing line.
        private static double buildMs;

        // With the dish preview off the page gets no ingredient data, and still its script for the food sort and the
        // book data of the codex marks. A static delegate, so the tick of each frame allocates no closure.
        private static readonly Func<string> Build = () =>
        {
            long buildStart = Timing.Start();
            var book = BookData.Current;
            string json = Plugin.DishPreview.Value
                ? IngredientData.Json(GameWords.Current(), TalentInputs.ReadAppraisal(), book)
                : NoIngredientData(book);
            buildMs = Timing.Ms(buildStart);
            return json;
        };

        private static void Postfix()
        {
            try
            {
                if (Plugin.IconTabs.Value) IconTabsSending.Tick();
                var step = schedule.Tick(Time.realtimeSinceStartup, Build);
                if (step.Kind == PushSchedule.Kind.None) return;
                long sendStart = Timing.Start();
                PageScript.Send(step);
                if (Plugin.Verbose.Value)
                {
                    // The text that the command carries: the data of setData, and the sort data.
                    double kb = (step.Kind == PushSchedule.Kind.SetData ? Timing.Kb(step.Json) : 0) + Timing.Kb(step.SortJson);
                    Plugin.Log.LogDebug(FormattableString.Invariant($"timing: build {buildMs:0.00} ms, send {Timing.Ms(sendStart):0.00} ms, json {kb:0.0} KB ({step.Kind})"));
                }
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

    // The page ready calls of the Brewing Barrel and Ration Press windows, so a pass applies the icon tabs in their frame.
    [HarmonyPatch(typeof(WebUI_Brew), "CallShowAction")]
    internal static class PageTickOnBrewShow
    {
        private static void Postfix()
        {
            if (!Plugin.IconTabs.Value) return;
            try
            {
                PageTick.Request();
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug("ProjectCook page ready: BrewPanel");
            }
            catch (Exception e) { Plugin.Log.LogError($"Project Cook page ready failed: {e}"); }
        }
    }

    [HarmonyPatch(typeof(WebUI_Compressor), "CallShowAction")]
    internal static class PageTickOnCompressorShow
    {
        private static void Postfix()
        {
            if (!Plugin.IconTabs.Value) return;
            try
            {
                PageTick.Request();
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug("ProjectCook page ready: Compressor");
            }
            catch (Exception e) { Plugin.Log.LogError($"Project Cook page ready failed: {e}"); }
        }
    }

    // The storage window: the same ready call, so a pass installs the parts of the script in its frame (the icon of
    // the cooking tag, the food sort). A pass with no Cooking frame and a storage window gets no retry.
    [HarmonyPatch(typeof(WebUI_BackpackUI), "CallShowAction")]
    internal static class PageTickOnStorageShow
    {
        private static void Postfix()
        {
            try
            {
                PageTick.Request();
                if (Plugin.Verbose.Value) Plugin.Log.LogDebug("storage window shown, pass requested");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook storage window ready failed: {e}");
            }
        }
    }
}
