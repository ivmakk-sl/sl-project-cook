using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free logic of the icon tabs. This file must not use any game or BepInEx type, because the unit tests compile
    // it alone.
    public static class IconTabsLogic
    {
        // One tab of the tab list of a food window, as its page reducer holds it after the open. LockText is the lock
        // text that the tab keeps itself (only the cooking window has one), else null or empty.
        public readonly struct Tab
        {
            public readonly long OwnerId;
            public readonly string Name;
            public readonly bool Locked;
            public readonly string LockText;
            public Tab(long ownerId, string name, bool locked, string lockText = null) { OwnerId = ownerId; Name = name; Locked = locked; LockText = lockText; }
        }

        // One entry of the list for the page: the bag is the first tab of each food window; each other tab is a
        // furniture (a fridge, a cooking storage, or a tool cabinet of the Ration Press). Lock is the game's lock text
        // of a locked tab, else null. Icon and Text are the icon choice of the tab (SlShared.IconTabs.TabIcon), set by
        // the game code with With.
        public readonly struct Entry
        {
            public readonly long OwnerId;
            public readonly string Name;
            public readonly bool Bag;
            public readonly string Lock;
            public readonly string Icon, Text;
            public Entry(long ownerId, string name, bool bag, string lockText, string icon = null, string text = null)
            {
                OwnerId = ownerId; Name = name; Bag = bag; Lock = lockText; Icon = icon; Text = text;
            }
            public Entry With(string icon, string text) => new Entry(OwnerId, Name, Bag, Lock, icon, text);
        }

        // windowLockText is the lock text of the window, for a locked tab that keeps no lock text itself.
        public static List<Entry> Entries(IReadOnlyList<Tab> tabs, string windowLockText)
        {
            var entries = new List<Entry>(tabs.Count);
            for (var i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                var lockText = !t.Locked ? null : string.IsNullOrEmpty(t.LockText) ? windowLockText : t.LockText;
                entries.Add(new Entry(t.OwnerId, t.Name, i == 0, lockText));
            }
            return entries;
        }
    }

    // The send of the icon tab list of the newest open to the root page script, which keeps it for the frame of the
    // window (the frame of an open loads after the open). A tick sends the pending list once (with the script when the
    // root page has none); the answer "no script" or no answer (a browser crash) tries again one real second later, and
    // "script busy" (another full send in flight) a tenth of a second later, while the request is less than 3 real
    // seconds old. A newer list replaces the pending one, and the late
    // answer of an older send does not drop it. A send whose answer never comes counts as sent for 5 real seconds only.
    // No game type here: the caller gives the real time.
    public sealed class IconTabsSend
    {
        // The answer of a send that needed the script while another full send of the script was in flight: the list
        // tries again soon, when that script is there.
        public const string ScriptBusy = "script busy";
        private const float BusyRetrySeconds = 0.1f;
        private const float RetrySeconds = 1f;
        private const float RequestSeconds = 3f;
        private const float AnswerSeconds = 5f;

        private string pending, sent;
        private float requestAt, notBefore, sentAt;

        public void Request(string json, float now)
        {
            pending = json;
            requestAt = now;
            notBefore = now;
        }

        // The list to send now, or null.
        public string Tick(float now)
        {
            if (pending == null || now < notBefore) return null;
            if (sent != null && now - sentAt < AnswerSeconds) return null;
            sent = pending;
            sentAt = now;
            return pending;
        }

        public void OnResult(string json, string result, float now)
        {
            if (json == sent) sent = null;
            if (json != pending) return;
            if (result == ScriptBusy && now - requestAt < RequestSeconds) notBefore = now + BusyRetrySeconds;
            else if ((result == null || result == PageJson.NoScript) && now - requestAt < RequestSeconds) notBefore = now + RetrySeconds;
            else pending = null;
        }
    }
}

