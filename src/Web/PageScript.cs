using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using UnityEngine;

namespace ProjectCook
{
    // The cooking page draws each card with a fixed name line and hint line. The page ignores unknown entry fields,
    // so the mod sends the preview text in a "Preview" field and the page script adds it to the card as extra lines.
    // The page script (the Vite bundle of src/Web/page) goes to the root page only when the root page does not have
    // it (a new root page, or one that the game built again after a browser crash); each other send holds only the
    // apply call or the ingredient data.
    internal static class PageScript
    {
        private static string script;
        private static readonly HashSet<string> loggedMissing = new HashSet<string>();
        private static readonly HashSet<string> loggedErrors = new HashSet<string>();
        private static readonly HashSet<string> loggedOther = new HashSet<string>();
        private static bool webViewWarned;

        // The Vite bundle, embedded by ProjectCook.csproj under this name.
        private static string Script()
        {
            if (script != null) return script;
            using (var stream = typeof(PageScript).Assembly.GetManifestResourceStream("ProjectCook.page.js"))
            using (var reader = new System.IO.StreamReader(stream))
                script = reader.ReadToEnd();
            return script;
        }

        private static Vuplex.WebView.IWebView WebView() =>
            ReduxUISystem.Instance?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        // A full send whose result has not come back yet blocks a second one, for at most this long: a
        // browser crash drops the result.
        private const float FullSendWaitSeconds = 5f;
        private static float fullSendAt = float.NegativeInfinity;
        // The number of the last full send: only its result opens the gate, not a late one of an older send.
        private static int fullSendId;

        // Sends the apply call or the data alone, and the script with the data when the root page has no script.
        public static void Send(PushSchedule.Step step)
        {
            var webView = WebView();
            if (webView == null)
            {
                if (!webViewWarned)
                {
                    webViewWarned = true;
                    Plugin.Log.LogWarning("web view not found");
                }
                Done(step, null);
                return;
            }
            string command = step.Kind == PushSchedule.Kind.Apply ? PageJson.ApplyCommand : PageJson.SetDataCommand(step.Json);
            if (!Execute(webView, command, r =>
            {
                try
                {
                    if (r != PageJson.NoScript)
                    {
                        Done(step, r);
                        return;
                    }
                    PageTick.OnResult(step, r);
                    SendWithScript(new PushSchedule.Step(PushSchedule.Kind.SetData, step.Json, step.Retry));
                }
                catch (Exception e) { Plugin.Log.LogError($"Project Cook send failed: {e}"); }
            }))
                Done(step, null);
        }

        private static void SendWithScript(PushSchedule.Step step)
        {
            var webView = WebView();
            float now = Time.realtimeSinceStartup;
            if (webView == null || now - fullSendAt < FullSendWaitSeconds)
            {
                Done(step, null);
                return;
            }
            fullSendAt = now;
            int id = ++fullSendId;
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug("ProjectCook page script: sent");
            if (!Execute(webView, PageJson.SetDataWithScriptCommand(Script(), step.Json), r =>
            {
                try
                {
                    if (id == fullSendId) fullSendAt = float.NegativeInfinity;
                    Done(step, r);
                }
                catch (Exception e) { Plugin.Log.LogError($"Project Cook send failed: {e}"); }
            }))
            {
                fullSendAt = float.NegativeInfinity;
                Done(step, null);
            }
        }

        // A web view that is being built again or disposed can throw at the call itself. The caller then counts
        // the send as one with no result, so the schedule tries again. Each distinct text logs once.
        private static bool Execute(Vuplex.WebView.IWebView webView, string js, Action<string> callback)
        {
            try
            {
                webView.ExecuteJavaScript(js, (Il2CppSystem.Action<string>)callback);
                return true;
            }
            catch (Exception e)
            {
                if (loggedOther.Add(e.Message)) Plugin.Log.LogWarning($"Project Cook send failed: {e.Message}");
                return false;
            }
        }

        // No result means the call did not run (for example a browser crash).
        private static void Done(PushSchedule.Step step, string r)
        {
            PageTick.OnResult(step, r);
            LogPageCheck(r);
            if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"ProjectCook send: {step.Kind}{(step.Retry ? " retry" : "")} result={r}");
        }

        // The result of a pass has an optional "; missing: ..." part (one text for each distinct set of missing
        // parts) and an optional "; errors: text1 || text2" part (the errors that the page script has not reported
        // before). A result "error: ..." is a page error too. Each is logged one time, with Verbose off also, so a
        // game update or a page bug is not silent.
        private static void LogPageCheck(string r)
        {
            if (r == null) return;
            if (r.StartsWith("error: ", StringComparison.Ordinal))
            {
                if (loggedErrors.Add(r)) Plugin.Log.LogError($"page script error: {r.Substring("error: ".Length)}");
                return;
            }
            const string missingMarker = "; missing: ";
            const string errorsMarker = "; errors: ";
            int missingAt = r.IndexOf(missingMarker, StringComparison.Ordinal);
            int errorsAt = r.IndexOf(errorsMarker, StringComparison.Ordinal);
            if (missingAt >= 0)
            {
                int end = errorsAt >= 0 ? errorsAt : r.Length;
                string missing = r.Substring(missingAt + missingMarker.Length, end - missingAt - missingMarker.Length);
                if (loggedMissing.Add(missing)) Plugin.Log.LogWarning($"page check: missing {missing}");
            }
            if (errorsAt >= 0)
            {
                string errors = r.Substring(errorsAt + errorsMarker.Length);
                foreach (var text in errors.Split(new[] { " || " }, StringSplitOptions.None))
                    if (loggedErrors.Add(text)) Plugin.Log.LogError($"page script error: {text}");
            }
        }
    }
}
