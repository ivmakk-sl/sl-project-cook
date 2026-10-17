using ProjectCook;
using Xunit;

public class QualityChancesTests
{
    private static readonly int[] Map = { 0, 30, 70, 90 };
    private const int Fail = 0, Normal = 1, Good = 2, Perfect = 3;

    [Fact]
    public void Level3WithBonus33_HasOnlyGoodAndPerfect()
    {
        // roll 40..100 (61 values), Perfect needs a roll of 57 or more (44 values)
        var c = PreviewLogic.QualityChances(40, 33, Map);
        Assert.Equal(0, c[Fail]);
        Assert.Equal(0, c[Normal]);
        Assert.Equal(100.0 * 17 / 61, c[Good], 6);
        Assert.Equal(100.0 * 44 / 61, c[Perfect], 6);
    }

    [Fact]
    public void Level5WithBonus33_IsAlwaysPerfect()
    {
        var c = PreviewLogic.QualityChances(70, 33, Map);
        Assert.Equal(100, c[Perfect], 6);
    }

    [Fact]
    public void Level0WithRottenPenalty_CanFail()
    {
        // roll 0..100 (101 values) - 20, Fail below 30 means a roll of 0..49
        var c = PreviewLogic.QualityChances(0, -20, Map);
        Assert.Equal(100.0 * 50 / 101, c[Fail], 6);
        Assert.Equal(0, c[Perfect]);
    }

    [Fact]
    public void RollOf100IsIncluded()
    {
        // without a bonus only the rolls 90..100 (11 of 101) are Perfect
        var c = PreviewLogic.QualityChances(0, 0, Map);
        Assert.Equal(100.0 * 11 / 101, c[Perfect], 6);
    }

    [Theory]
    [InlineData(0, -20)]
    [InlineData(10, 0)]
    [InlineData(40, 33)]
    [InlineData(70, 200)]
    [InlineData(0, -500)]
    public void ChancesAddTo100(int floor, int bonus)
    {
        Assert.Equal(100, PreviewLogic.QualityChances(floor, bonus, Map).Sum(), 6);
    }

    [Fact]
    public void ClampKeepsLargeNegativeBonusAtFail()
    {
        Assert.Equal(100, PreviewLogic.QualityChances(0, -500, Map)[Fail], 6);
    }

    [Fact]
    public void ShortMapGivesNormal()
    {
        Assert.Equal(100, PreviewLogic.QualityChances(0, 0, new[] { 0, 30 })[Normal], 6);
    }

    [Fact]
    public void TalentBonusIsJustPartOfTheBonusArgument()
    {
        // floor 40, bonus 15 (base) + 15 (talent) = 30: quality >= 90 needs a roll of 60 or more, 41 of 61 rolls
        var c = PreviewLogic.QualityChances(40, 15 + 15, Map);
        Assert.Equal(100.0 * 41 / 61, c[Perfect], 6);
    }
}

public class TalentQualityBonusTests
{
    [Theory]
    [InlineData(0f, 0f, true, 0)]
    [InlineData(0f, 0f, false, 0)]
    [InlineData(0.15f, 0f, true, 15)]
    [InlineData(0.15f, 0f, false, 15)]
    [InlineData(0.45f, 0.15f, false, 60)] // tag recipe: perfect bonus plus tag bonus
    [InlineData(0.45f, 0.15f, true, 45)]  // exact recipe: tag bonus does not apply
    public void RoundsEachRatioToWholePercent(float perfectRatio, float tagRatio, bool isExact, int expected)
    {
        Assert.Equal(expected, PreviewLogic.TalentQualityBonus(perfectRatio, tagRatio, isExact));
    }
}

public class RottenPenaltyTests
{
    [Theory]
    [InlineData(-20, 0f, -20)]
    [InlineData(-20, 0.3f, -14)]
    [InlineData(-20, 0.6f, -8)]
    [InlineData(-20, 1.0f, 0)]
    [InlineData(-20, 1.5f, 0)]
    public void ReducesThePenaltyByTheRatio(int penalty, float reduceRatio, int expected)
    {
        Assert.Equal(expected, PreviewLogic.RottenPenalty(penalty, reduceRatio));
    }
}

public class CookExpTests
{
    private const int Fail = 0, Good = 2;

    [Theory]
    [InlineData(40, 0f, Good, 40)]
    [InlineData(40, 0.25f, Good, 50)]
    [InlineData(45, 0.75f, Good, 79)]  // 78.75 rounds up to 79
    [InlineData(15, 0.5f, Good, 22)]   // 22.5 rounds to the even 22
    [InlineData(13, 0.5f, Good, 20)]   // 19.5 rounds to the even 20
    [InlineData(40, 0.25f, Fail, 0)]
    public void RoundsTheExpRatio_ZeroForFail(int recipeExp, float expRatio, int level, int expected)
    {
        Assert.Equal(expected, PreviewLogic.CookExp(recipeExp, expRatio, level));
    }
}

public class SeasoningBonusTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 18)]
    [InlineData(2, 27)]
    [InlineData(3, 32)] // 31.5
    [InlineData(4, 34)] // 33.75
    public void Series(int count, int expected)
    {
        Assert.Equal(expected, PreviewLogic.SeasoningBonus(count, 18, 0.5f));
    }
}

public class LinesTests
{
    // stat order: satiety, mood, energy, health, life
    private static int[][] Stats(params int[][] byLevel) => byLevel;

    [Fact]
    public void HighestQualityFirst_ZeroChanceOmitted_ZeroStatsOmitted()
    {
        var stats = Stats(new[] { 3, 0, 0, 0, 0 }, new[] { 22, 5, 0, 0, 3 }, new[] { 27, 9, 0, 0, 5 }, new[] { 32, 13, 0, 0, 7 });
        var lines = PreviewLogic.Lines(new[] { 0, 0, 27.87, 72.13 }, stats, new[] { 1, 1, 1, 1 }, TestTexts.English());
        Assert.Equal(new[] { "3|Perfect|72%|🍖32|🧠13|❤️7", "2|Good|28%|🍖27|🧠9|❤️5" }, lines);
    }

    [Fact]
    public void AllLinesHaveTheSameCells()
    {
        // energy is 0 only at Normal, and only Perfect has 2 portions
        var stats = Stats(new int[5], new[] { 7, 0, 0, 0, 0 }, new[] { 9, 0, 3, 0, 0 }, new[] { 40, 0, 6, 0, 0 });
        var lines = PreviewLogic.Lines(new[] { 0, 25.0, 33, 42 }, stats, new[] { 1, 1, 1, 2 }, TestTexts.English());
        Assert.Equal(new[] { "3|Perfect|42%|🍖40|⚡6|x2", "2|Good|33%|🍖9|⚡3|x1", "1|Average|25%|🍖7|⚡0|x1" }, lines);
    }

    [Fact]
    public void AllStatsButFitness_AndPortions()
    {
        // Eating does not change Fitness, so the lines never show it.
        var stats = Stats(new int[5], new int[5], new int[5], new[] { 221, 10, -4, 6, 2 });
        var lines = PreviewLogic.Lines(new[] { 0, 0, 0, 100.0 }, stats, new[] { 1, 1, 1, 7 }, TestTexts.English());
        Assert.Equal(new[] { "3|Perfect|100%|🍖221|🧠10|⚡-4|❤️2|x7" }, lines);
    }

    [Fact]
    public void PerPortion_DividesTheUnroundedValuesAndRoundsHalfAwayFromZero()
    {
        // The example of the spec: 123 satiety and 68 morale for the whole dish of 2 portions.
        var values = new[] { new double[5], new double[5], new double[5], new[] { 123.0, 68.0, -5.0, 0, 7.4 } };
        var stats = PreviewLogic.PerPortion(values, new[] { 1, 1, 1, 2 });
        Assert.Equal(new[] { 62, 34, -3, 0, 4 }, stats[3]);
    }

    [Fact]
    public void PerPortion_LinesKeepThePortionCount()
    {
        var values = new[] { new double[5], new double[5], new double[5], new[] { 123.0, 68.0, 0, 0, 0 } };
        var lines = PreviewLogic.Lines(new[] { 0, 0, 0, 100.0 }, PreviewLogic.PerPortion(values, new[] { 1, 1, 1, 2 }), new[] { 1, 1, 1, 2 }, TestTexts.English());
        Assert.Equal(new[] { "3|Perfect|100%|🍖62|🧠34|x2" }, lines);
    }

    [Fact]
    public void PerPortion_OnePortion_SameAsTheWholeDish()
    {
        var values = new[] { new double[5], new double[5], new[] { 26.5, 9.2, 0, 0, 4.5 }, new[] { 32.4, 13.0, 0, 0, 7.6 } };
        var whole = Array.ConvertAll(values, l => Array.ConvertAll(l, v => (int)Math.Round(v)));
        Assert.Equal(whole, PreviewLogic.PerPortion(values, new[] { 1, 1, 1, 1 }));
    }

    [Fact]
    public void TinyChanceShowsAsOnePercent()
    {
        var stats = Stats(new[] { 10, 0, 0, 0, 0 }, new[] { 20, 0, 0, 0, 0 }, new int[5], new int[5]);
        var lines = PreviewLogic.Lines(new[] { 0.2, 99.8, 0, 0 }, stats, new[] { 1, 1, 1, 1 }, TestTexts.English());
        Assert.Equal(new[] { "1|Average|100%|🍖20", "0|Failed|1%|🍖10" }, lines);
    }

    [Fact]
    public void QualityNameComesFromTheWords_OtherCellsDoNotChange()
    {
        var stats = Stats(new int[5], new int[5], new[] { 9, 0, 3, 0, 0 }, new[] { 40, 0, 6, 0, 0 });
        var lines = PreviewLogic.Lines(new[] { 0, 0, 58.0, 42 }, stats, new[] { 1, 1, 1, 2 }, TestTexts.Chinese());
        Assert.Equal(new[] { "3|完美|42%|🍖40|⚡6|x2", "2|良好|58%|🍖9|⚡3|x1" }, lines);
    }
}
public class IngredientTipTests
{
    [Theory]
    // stat order: satiety, mood, energy, health, life
    [InlineData(new[] { 2, -3, 0, 0, 2 }, 3, 0, "T3|Tier|Low-grade\n🍖 Satiety: +2\n🧠 Morale: -3\n❤️ Life: +2")]
    [InlineData(new[] { 5, 0, 4, 2, 0 }, 1, 12, "T1|Tier|High-end\nTrade value: 12\n🍖 Satiety: +5\n⚡ Stamina: +4\n💚 Fitness: +2")]
    [InlineData(new[] { 0, 0, 0, 0, 0 }, 2, 0, "T2|Tier|Mid-tier")]
    [InlineData(new[] { 8, 0, 0, 0, 0 }, 0, 3, "Trade value: 3\n🍖 Satiety: +8")]
    public void TierThenTradeValueThenOneSignedLineForEachStat(int[] stats, int tier, int tradeValue, string expected)
    {
        Assert.Equal(expected, PreviewLogic.IngredientTip(stats, tier, tradeValue, TestTexts.English()));
    }

    [Fact]
    public void NothingToShow()
    {
        Assert.Null(PreviewLogic.IngredientTip(new int[5], 0, 0, TestTexts.English()));
    }

    [Fact]
    public void NamesComeFromTheWords()
    {
        Assert.Equal("T3|档次|低档\n交易价值: 5\n🍖 饱腹: +7\n🧠 心态: -4", PreviewLogic.IngredientTip(new[] { 7, -4, 0, 0, 0 }, 3, 5, TestTexts.Chinese()));
    }
}
public class WordsTests
{
    [Fact]
    public void EnglishTexts()
    {
        var words = TestTexts.English();
        Assert.Equal(new[] { "Failed", "Average", "Good", "Perfect" }, words.Quality);
        Assert.Equal(new[] { null, "High-end", "Mid-tier", "Low-grade" }, words.Tier);
        Assert.Equal(new[] { "Satiety", "Morale", "Stamina", "Fitness", "Life" }, words.Stat);
        Assert.Equal("Tier", words.TierLabel);
        Assert.Equal("Trade value", words.TradeLabel);
        Assert.Equal("Cooking XP", words.ExpLabel);
        Assert.Equal(new[] { "Whole dish", "Per portion" }, words.Portion);
    }

    [Fact]
    public void ChineseGameTextsWithTheChineseModTexts()
    {
        var words = TestTexts.Chinese();
        Assert.Equal(new[] { "失败", "普通", "良好", "完美" }, words.Quality);
        Assert.Equal(new[] { null, "高档", "中档", "低档" }, words.Tier);
        Assert.Equal(new[] { "饱腹", "心态", "精力", "健康", "生命" }, words.Stat);
        Assert.Equal("档次", words.TierLabel);
        Assert.Equal("交易价值", words.TradeLabel);
        Assert.Equal("烹饪熟练度", words.ExpLabel);
        Assert.Equal(new[] { "整道菜", "每份" }, words.Portion);
    }

    [Fact]
    public void AMissingGameTextGivesTheEnglishText_OtherWordsStay()
    {
        var game = TestTexts.ChineseGameTexts();
        game.Remove("SR_Web_Cooking_84");          // Perfect
        game["SR_Web_Cooking_54"] = "";            // Mid
        game["GameKey_2"] = "GameKey_2";           // the game returns the key for an unknown key
        var texts = TestTexts.Load();
        var words = PreviewLogic.WordsFrom(texts.For(0, TestTexts.Game(game)));
        Assert.Equal(new[] { "失败", "普通", "良好", "Perfect" }, words.Quality);
        Assert.Equal(new[] { null, "高档", "Mid-tier", "低档" }, words.Tier);
        Assert.Equal(new[] { "饱腹", "Morale", "精力", "健康", "生命" }, words.Stat);
        foreach (var key in new[] { "SR_Web_Cooking_84", "SR_Web_Cooking_54", "GameKey_2" })
            Assert.Contains(texts.Warnings, w => w.Contains(key));
    }

    [Fact]
    public void SeparatorCharactersInAWordBecomeSpaces()
    {
        var game = TestTexts.ChineseGameTexts();
        game["SR_Web_Cooking_85"] = "Go|od";
        game["GameKey_1"] = "Sat\niety";
        game["GameKey_3"] = "Sta\rmina";
        var words = PreviewLogic.WordsFrom(TestTexts.Load().For(1, TestTexts.Game(game)));
        Assert.Equal("Go od", words.Quality[2]);
        Assert.Equal("Sat iety", words.Stat[0]);
        Assert.Equal("Sta mina", words.Stat[2]);
    }

    [Fact]
    public void SameWordsAreEqual()
    {
        Assert.True(TestTexts.Chinese().SameAs(TestTexts.Chinese()));
        Assert.False(TestTexts.Chinese().SameAs(TestTexts.English()));
        Assert.False(TestTexts.Chinese().SameAs(null));
    }

    [Fact]
    public void SameAsIsFalseWhenOnlyATooltipLabelDiffers()
    {
        var a = TestTexts.Chinese();
        var b = TestTexts.Chinese();
        b.ExpLabel = "other";
        Assert.False(a.SameAs(b));
    }
}
public class TipLinesTests
{
    [Fact]
    public void ExpLineThenQualityHeaderAndTradeRow_HighestFirst_ZeroChanceOmitted()
    {
        var lines = PreviewLogic.TipLines(new[] { 0, 0, 28.0, 72.0 }, new[] { 20, 50, 90, 200 }, 79, 0, TestTexts.English());
        Assert.Equal(new[]
        {
            "Cooking XP: +79",
            "|3:Perfect|2:Good",
            "Trade value|200|90",
        }, lines);
    }

    [Fact]
    public void ExpLineNamesTheZeroOfAFailedDish()
    {
        var lines = PreviewLogic.TipLines(new[] { 20.0, 0, 0, 80.0 }, new[] { 5, 0, 0, 200 }, 79, 0, TestTexts.English());
        Assert.Equal(new[]
        {
            "Cooking XP: +79 (Failed 0)",
            "|3:Perfect|0:Failed",
            "Trade value|200|5",
        }, lines);
    }

    [Fact]
    public void OneLevelGivesPlainLines_WithNoTalentsLine()
    {
        var lines = PreviewLogic.TipLines(new[] { 0, 0, 0, 100.0 }, new[] { 0, 0, 0, 200 }, 79, 0, TestTexts.English());
        Assert.Equal(new[] { "Cooking XP: +79", "Trade value: 200" }, lines);
    }

    [Fact]
    public void OneLevelGivesPlainLines_ChineseWordsGiveChineseLabels()
    {
        var lines = PreviewLogic.TipLines(new[] { 0, 0, 0, 100.0 }, new[] { 0, 0, 0, 200 }, 79, 0, TestTexts.Chinese());
        Assert.Equal(new[]
        {
            "烹饪熟练度: +79",
            "交易价值: 200",
        }, lines);
    }
}

public class TipLinesOnlyFailTests
{
    [Fact]
    public void OnlyFailedCanOccur_ExpIsAPlainZero()
    {
        var lines = PreviewLogic.TipLines(new[] { 100.0, 0, 0, 0 }, new[] { 5, 0, 0, 0 }, 79, 0, TestTexts.English());
        Assert.Equal(new[] { "Cooking XP: 0", "Trade value: 5" }, lines);
    }
}

public class TipLinesTierTests
{
    [Fact]
    public void TierLineComesFirst_InTheFormatOfTheIngredientTip()
    {
        var lines = PreviewLogic.TipLines(new[] { 0, 0, 0, 100.0 }, new[] { 0, 0, 0, 200 }, 79, 2, TestTexts.English());
        Assert.Equal(new[] { "T2|Tier|Mid-tier", "Cooking XP: +79", "Trade value: 200" }, lines);
    }

    [Fact]
    public void DishWithNoTierHasNoTierLine()
    {
        var lines = PreviewLogic.TipLines(new[] { 0, 0, 0, 100.0 }, new[] { 0, 0, 0, 200 }, 79, 0, TestTexts.English());
        Assert.Equal(new[] { "Cooking XP: +79", "Trade value: 200" }, lines);
    }
}

public class PortionsTests
{
    [Theory]
    [InlineData(221f, 32f, 7)]
    [InlineData(32f, 32f, 1)]
    [InlineData(33f, 32f, 2)]
    [InlineData(10f, 32f, 1)]
    [InlineData(0f, 32f, 1)]
    public void CeilAboveTheThreshold(float satiety, float threshold, int expected)
    {
        Assert.Equal(expected, PreviewLogic.Portions(satiety, threshold));
    }

    // The game compares the satiety before it is rounded for the display.
    [Theory]
    [InlineData(40.3f, 40f, 2)]
    [InlineData(40f, 40f, 1)]
    [InlineData(80.2f, 40f, 3)]
    [InlineData(10f, 0f, 1)]
    [InlineData(31f, 30.5f, 2)]
    public void UsesTheUnroundedSatiety(float satiety, float threshold, int expected)
    {
        Assert.Equal(expected, PreviewLogic.Portions(satiety, threshold));
    }
}

public class TradeValueTests
{
    [Theory]
    // Shrewd Appraisal I: 22 x 1.15 = 25.3
    [InlineData(22, 0.15f, 25)]
    // Shrewd Appraisal I and Bargaining I: 73 x 1.25 = 91.25
    [InlineData(73, 0.25f, 91)]
    // A half rounds up: 10 x 1.25 = 12.5
    [InlineData(10, 0.25f, 13)]
    [InlineData(40, 0f, 40)]
    [InlineData(0, 0.3f, 0)]
    public void ConfigValueTimesOnePlusAppraisal_HalfUp(int configValue, float appraisal, int expected)
    {
        Assert.Equal(expected, PreviewLogic.TradeValue(configValue, appraisal));
    }
}
