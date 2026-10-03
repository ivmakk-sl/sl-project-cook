using System;

namespace ProjectCook
{
    // Decides on each frame whether the plugin sends to the page script, and what. No game or BepInEx type
    // here: the caller gives the real time.
    //
    // - A prediction refresh sets a request. The next tick sends once: SetData when the data JSON differs
    //   from the one that the page confirmed, else Apply. So many refreshes in one frame make one send.
    // - An answer "no Cooking frame" (or no answer, after a browser crash) sets a retry one real second
    //   later, while the request is less than 3 real seconds old. It is the only retry: the page has none.
    // - An answer "no script" forgets the confirmed data, so the next send carries it.
    // - A build that throws (for example during a save load) counts as a send with no answer, and rethrows
    //   to the caller.
    // - The numbers of the food sort are a second kind of data. New sort data goes with the send of a request
    //   in the same frame, or alone as SetSortData, so one change of a window makes at most one send. The same
    //   sort data as the sent or confirmed one makes no send. A sort send with no answer goes again one real
    //   second later; an answer "no script" forgets the confirmed sort data, so the next sort data goes again.
    //   A sort send whose answer never comes (a dropped callback) counts as sent for 5 real seconds only, and
    //   the late answer of an older send does not free a newer one. A sort-only build that throws waits one
    //   real second.
    public sealed class PushSchedule
    {
        public enum Kind { None, Apply, SetData, SetSortData }

        public readonly struct Step
        {
            public readonly Kind Kind;
            // The data JSON of the send, also for Apply, so a send that finds no page script can send the
            // script with the data. Null for None.
            public readonly string Json;
            // True for a send of the retry, false for the first send of a request.
            public readonly bool Retry;
            // The sort data that the send carries, or null, and the number of that sort send.
            public readonly string SortJson;
            public readonly int SortSeq;

            public Step(Kind kind, string json, bool retry, string sortJson = null, int sortSeq = 0)
            {
                Kind = kind; Json = json; Retry = retry; SortJson = sortJson; SortSeq = sortSeq;
            }
        }

        private const float RetrySeconds = 1f;
        private const float RequestSeconds = 3f;
        private const float SortAnswerSeconds = 5f;

        private bool requested;
        private float requestAt;
        private float retryAt = float.NaN;
        // The data JSON that the page script has, as its answers confirm; null when it is not known.
        private string confirmedJson;
        // The last sort data, the one in a send with no answer yet, and the one that the page script has.
        private string pendingSort, sentSort, confirmedSort;
        private float sortNotBefore = float.NegativeInfinity;
        private float sentSortAt;
        private int sortSeq;

        // A prediction refresh.
        public void Request(float now)
        {
            requested = true;
            requestAt = now;
            retryAt = float.NaN;
        }

        // New sort data of the open window.
        public void RequestSort(string json)
        {
            pendingSort = json;
        }

        public Step Tick(float now, Func<string> build)
        {
            bool retry = false;
            bool dataDue = requested;
            if (!requested && !float.IsNaN(retryAt) && now >= retryAt)
            {
                retryAt = float.NaN;
                if (now - requestAt < RequestSeconds) retry = dataDue = true;
            }
            if (sentSort != null && now - sentSortAt >= SortAnswerSeconds) sentSort = null;
            bool sortDue = pendingSort != null && pendingSort != confirmedSort && pendingSort != sentSort && now >= sortNotBefore;
            if (!dataDue && !sortDue) return new Step(Kind.None, null, false);
            requested = false;
            string json;
            try { json = build(); }
            catch
            {
                if (dataDue) SetRetry(now);
                else sortNotBefore = now + RetrySeconds;
                throw;
            }
            string sort = sortDue ? pendingSort : null;
            int seq = 0;
            if (sortDue)
            {
                sentSort = pendingSort;
                sentSortAt = now;
                seq = ++sortSeq;
            }
            if (!dataDue) return new Step(Kind.SetSortData, json, false, sort, seq);
            return new Step(json == confirmedJson ? Kind.Apply : Kind.SetData, json, retry, sort, seq);
        }

        // The answer of the page script to a send, or null when the send did not run (a browser crash).
        public void OnResult(Step sent, string result, float now)
        {
            if (sent.SortJson != null)
            {
                bool latest = sent.SortSeq == sortSeq;
                if (latest && sent.SortJson == sentSort) sentSort = null;
                if (result == null) { if (latest) sortNotBefore = now + RetrySeconds; }
                else if (result == PageJson.NoScript) confirmedSort = null;
                else confirmedSort = sent.SortJson;
            }
            if (result == PageJson.NoScript) confirmedSort = null;
            if (sent.Kind == Kind.SetSortData) return;
            if (result == null || result == PageJson.NoScript)
            {
                // A root page with no page script has no data either.
                if (result == PageJson.NoScript) confirmedJson = null;
                else SetRetry(now);
                return;
            }
            // setData stores the data before its pass, also when no Cooking frame is there or the pass fails (a
            // rebuilt browser blocks the frame), so the next sends need not carry the data again.
            if (sent.Kind == Kind.SetData) confirmedJson = sent.Json;
            if (result == "no Cooking frame" || result.StartsWith("no Cooking frame;", StringComparison.Ordinal)) SetRetry(now);
        }

        // A later refresh makes its own send, so only the answer of the last send sets a retry.
        private void SetRetry(float now)
        {
            if (!requested) retryAt = now + RetrySeconds;
        }
    }
}
