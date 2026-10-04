using ProjectCook;
using SlShared.I18n;

// The words of the tests, from the i18n files of the mod (src/i18n/), embedded in the test assembly as in the mod DLL.
internal static class TestTexts
{
    // The game's Chinese texts of the text keys of the preview words.
    internal static Dictionary<string, string> ChineseGameTexts() => new Dictionary<string, string>
    {
        ["SR_Web_Cooking_87"] = "失败", ["SR_Web_Cooking_86"] = "普通", ["SR_Web_Cooking_85"] = "良好", ["SR_Web_Cooking_84"] = "完美",
        ["SR_Web_Cooking_53"] = "高档", ["SR_Web_Cooking_54"] = "中档", ["SR_Web_Cooking_55"] = "低档",
        ["GameKey_1"] = "饱腹", ["GameKey_2"] = "心态", ["GameKey_3"] = "精力", ["GameKey_4"] = "健康", ["GameKey_5"] = "生命",
    };

    // A new I18nTexts for each call: the tests run in parallel, and the texts record their warnings.
    internal static I18nTexts Load() => I18nTexts.Load(typeof(TestTexts).Assembly, "ProjectCook");

    // The game's text function of a map; a key that the map does not have gives an empty text, as the game does.
    internal static Func<string, string> Game(Dictionary<string, string> texts) =>
        key => texts.TryGetValue(key, out var text) ? text : "";

    internal static I18nTextSet EnglishSet() => Load().For(1, null);

    internal static I18nTextSet ChineseSet() => Load().For(0, Game(ChineseGameTexts()));

    internal static PreviewLogic.Words English() => PreviewLogic.WordsFrom(EnglishSet());

    internal static PreviewLogic.Words Chinese() => PreviewLogic.WordsFrom(ChineseSet());
}
