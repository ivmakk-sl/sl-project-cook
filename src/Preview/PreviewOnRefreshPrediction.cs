using System;
using HarmonyLib;
using GameCore.HotUpdate.ReduxUI;
using Il2CppDict = Il2CppSystem.Collections.Generic;

namespace ProjectCook
{
    // RefreshPrediction runs after each workbench change and fills the "THIS POT" list of the cooking page.
    [HarmonyPatch(typeof(Reducer_Web_Cooking), "RefreshPrediction")]
    internal static class PreviewOnRefreshPrediction
    {
        private static bool formatWarned;

        private static void Postfix(State_Web_Cooking state, Il2CppDict.Dictionary<int, int> workbenchIds, Il2CppDict.Dictionary<int, int> workbenchTags)
        {
            try
            {
                if (state == null || state.HotPotMode.Value) return;
                // Also with an empty workbench, because the ingredient tooltips need the page script. Each window
                // open refreshes the prediction, so the send also finds a new Cooking frame.
                PageTick.Request();

                long readStart = Timing.Start();
                string json = state.PredictionListJson.Value;
                var entries = PreviewLogic.ReadEntries(json);
                double readMs = Timing.Ms(readStart);
                if (entries.Count == 0)
                {
                    // A list with objects but no readable entry means that a game update changed the format.
                    if (!formatWarned && json != null && json.IndexOf('{') >= 0)
                    {
                        formatWarned = true;
                        Plugin.Log.LogWarning($"The prediction list has no readable entry, so no preview lines show. The game format probably changed: {json}");
                    }
                    return;
                }

                long buildStart = Timing.Start();
                string result = json;
                if (Plugin.DishPreview.Value)
                {
                    // The row split Postfix runs first and rebuilds the list from its plan, one entry for each dish.
                    var plan = RowPlanner.Current;
                    var dishes = plan != null && plan.Dishes.Count == entries.Count ? plan.Dishes : null;
                    var previews = Preview.Build(state, entries, workbenchIds, workbenchTags, dishes, GameWords.Current(), out var tips, out var portionTexts);
                    if (previews.Count > 0) result = PreviewLogic.AddPreviews(result, previews, tips, portionTexts);
                }
                // The preview fields do not change the entries, so the indexes of entries stay valid.
                if (Plugin.CodexMarks.Value) result = AddCodex(result, entries);
                if (ReferenceEquals(result, json)) return;
                double buildMs = Timing.Ms(buildStart);
                state.PredictionListJson.Value = result;
                if (Plugin.Verbose.Value)
                    Plugin.Log.LogDebug(FormattableString.Invariant($"timing: prediction read {readMs:0.00} ms, build {buildMs:0.00} ms, json {Timing.Kb(json):0.0} KB, entries {entries.Count}"));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Project Cook failed: {e}");
            }
        }

        // The codex flag of each dish card whose dish has no codex entry.
        private static string AddCodex(string json, System.Collections.Generic.List<PreviewLogic.Entry> entries)
        {
            var reader = CodexReader.Take();
            var marked = new System.Collections.Generic.HashSet<int>();
            if (reader != null)
                for (int i = 0; i < entries.Count; i++)
                    if (reader.DishMark(entries[i].RecipeId)) marked.Add(i);
            if (Plugin.Verbose.Value)
                Plugin.Log.LogDebug($"codex: locked={(reader == null ? "unread" : reader.Locked.ToString())} dishes={entries.Count} marked={marked.Count}");
            return PreviewLogic.AddCodex(json, marked);
        }
    }
}
