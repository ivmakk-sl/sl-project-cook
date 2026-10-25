using System;
using System.Collections.Generic;

namespace ProjectCook
{
    // The book data of the Recipe Book (the codex marks of its rows and the Codex filter): the set of the recipes is
    // cached by the lock of the codex and the unlocked count of the dish entries, so the walk of the recipe config runs
    // only for a new dish entry or another game mode, not for each refresh. A language change builds the book again
    // with the new words and the same set. Null when the codex cannot be read: the page then shows no mark and no
    // Codex filter.
    internal static class BookData
    {
        private static BookKey? key;
        private static HashSet<int> ids;

        internal static PageJson.Book Current { get; private set; }

        // Called at each prediction refresh while CodexMarks is on. A new book asks the page tick for a send.
        internal static void Update()
        {
            long start = Timing.Start();
            var reader = CodexReader.Take();
            BookKey? next = null;
            int unlocked = 0, total = 0;
            if (reader != null && (reader.Locked || reader.DishCounts(out unlocked, out total)))
                next = new BookKey(reader.Locked, reader.Locked ? 0 : unlocked);
            bool built = false;
            if (!Nullable.Equals(next, key))
            {
                key = next;
                ids = next == null || reader.Locked ? null : reader.BookIds();
                // A failed walk is not kept under its key, so the next refresh walks again.
                if (next != null && !reader.Locked && ids == null) key = null;
                built = true;
            }
            var words = PageJson.BookWords.From(ModTexts.Current());
            PageJson.Book book = null;
            if (key != null && key.Value.Locked) book = new PageJson.Book(true, 0, 0, new int[0]);
            else if (key != null && ids != null) book = new PageJson.Book(false, unlocked, total, ids, words);
            if (!Same(book, Current))
            {
                Current = book;
                PageTick.Request();
            }
            if (Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug(FormattableString.Invariant(
                    $"codex: book locked={(book == null ? "unread" : book.Locked.ToString())} unlocked={book?.Unlocked ?? 0} total={book?.Total ?? 0} ids={book?.Ids.Count ?? 0} built={built} {Timing.Ms(start):0.00} ms"));
            }
        }

        // The same page data: the set is the cached one, so a new set is a new object.
        private static bool Same(PageJson.Book a, PageJson.Book b)
        {
            if (a == null || b == null) return a == b;
            if (a.Locked || b.Locked) return a.Locked == b.Locked;
            return a.Locked == b.Locked && a.Unlocked == b.Unlocked && a.Total == b.Total && ReferenceEquals(a.Ids, b.Ids)
                && (a.Words == null ? b.Words == null : a.Words.SameAs(b.Words));
        }
    }
}
