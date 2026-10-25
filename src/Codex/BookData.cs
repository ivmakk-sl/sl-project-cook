using System;
using System.Collections.Generic;

namespace ProjectCook
{
    // The book data of the Recipe Book (the codex marks of its rows and the Codex filter): the set of the recipes is
    // cached by the lock of the codex, the unlocked count of the dish entries, and the dishes in progress (each recipe of
    // a dish that a cooking station cooks now leaves the set), so the walk of the recipe config runs only for a new dish
    // entry, another game mode, or a new or stopped cook, not for each refresh. A language change builds the book again
    // with the new words and the same set. Null when the codex cannot be read: the page then shows no mark and no
    // Codex filter.
    internal static class BookData
    {
        private static BookKey? key;
        private static HashSet<int> ids;
        private static HashSet<int> lastInProgress;

        internal static PageJson.Book Current { get; private set; }

        // The ids of the book data: the recipes whose dish has no codex entry and is not a dish in progress. Null when
        // the codex cannot be read or is locked.
        internal static HashSet<int> Ids => key != null && !key.Value.Locked ? ids : null;

        // Called at each prediction refresh while CodexMarks is on. A new book asks the page tick for a send.
        internal static void Update()
        {
            long start = Timing.Start();
            var reader = CodexReader.Take();
            BookKey? next = null;
            int unlocked = 0, total = 0;
            HashSet<int> inProgress = null;
            if (reader != null && !reader.Locked)
            {
                inProgress = CodexLogic.DishesInProgress(CodexReader.CookingRecipeIds(), CodexReader.DishOf);
                if (lastInProgress == null || !lastInProgress.SetEquals(inProgress))
                {
                    lastInProgress = inProgress;
                    if (Plugin.Verbose.Value) Plugin.Log.LogDebug($"codex: in progress dishes={inProgress.Count}");
                }
            }
            if (reader != null && (reader.Locked || reader.DishCounts(out unlocked, out total)))
                next = new BookKey(reader.Locked, reader.Locked ? 0 : unlocked, inProgress);
            bool built = false;
            if (!Nullable.Equals(next, key))
            {
                key = next;
                ids = next == null || reader.Locked ? null : CodexLogic.WithoutInProgress(reader.BookIds(), inProgress, CodexReader.DishOf);
                // A failed walk is not kept under its key, so the next refresh walks again.
                if (next != null && !reader.Locked && ids == null) key = null;
                built = true;
            }
            var words = PageJson.BookWords.From(ModTexts.Current());
            PageJson.Book book = null;
            if (key != null && key.Value.Locked) book = new PageJson.Book(true, 0, 0, new int[0]);
            // The unknown recipe rows stay until the next refresh of the Recipe Book builds them again.
            else if (key != null && ids != null) book = new PageJson.Book(false, unlocked, total, ids, words, Current?.Unknown);
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

        // The unknown recipe rows of a refresh of the Recipe Book (UnknownRecipesWithIngredients, UnknownRecipesWithoutIngredients): new rows give new book data and
        // ask the page tick for a send. Nothing while there is no book data.
        internal static void SetUnknown(IReadOnlyList<PageJson.UnknownRow> rows)
        {
            var book = Current;
            if (book == null || book.Locked || PageJson.UnknownRow.SameRows(book.Unknown, rows)) return;
            Current = new PageJson.Book(false, book.Unlocked, book.Total, book.Ids, book.Words, rows);
            PageTick.Request();
        }

        // The same page data: the set is the cached one, so a new set is a new object.
        private static bool Same(PageJson.Book a, PageJson.Book b)
        {
            if (a == null || b == null) return a == b;
            if (a.Locked || b.Locked) return a.Locked == b.Locked;
            return a.Locked == b.Locked && a.Unlocked == b.Unlocked && a.Total == b.Total && ReferenceEquals(a.Ids, b.Ids)
                && (a.Words == null ? b.Words == null : a.Words.SameAs(b.Words)) && PageJson.UnknownRow.SameRows(a.Unknown, b.Unknown);
        }
    }
}
