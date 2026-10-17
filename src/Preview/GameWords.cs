namespace ProjectCook
{
    // The words that the mod adds to the cooking window, in the current display language.
    internal static class GameWords
    {
        // The language can change while the game runs, so the words are read again at each refresh.
        public static PreviewLogic.Words Current() => PreviewLogic.WordsFrom(ModTexts.Current());
    }
}
