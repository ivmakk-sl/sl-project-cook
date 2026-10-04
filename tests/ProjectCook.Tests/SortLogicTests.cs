using System.Collections.Generic;
using System.Linq;
using ProjectCook;
using Xunit;

public class SortLogicTests
{
    private static EatLogic.Dish OneUse(params float[] values) => new EatLogic.Dish { Values = values, HasInstanceValues = true, SubCategory = 2, Portions = 1 };

    private static readonly SortLogic.DaysLeft Fresh = SortLogic.DaysLeft.Spoils(3f);

    [Fact]
    public void StatNumbers_AreTheEatValuesOfOneUse()
    {
        var factors = new EatLogic.Factors { Talents = new List<EatLogic.Talent> { new EatLogic.Talent { Nourish = 0.05f } } };
        // A dish of 3 uses: 13.33 Satiety for each use, +5 % nourish = 14
        var n = SortLogic.ItemNumbers(OneUse(13.33f, -3, 0, 6, 2), true, factors, 0, 1, 1, 0, Fresh);
        Assert.Equal(14, n.Stats[SortLogic.Sat]);
        Assert.Equal(-3, n.Stats[SortLogic.Mor]);
        Assert.Null(n.Stats[SortLogic.Sta]);
        Assert.Equal(2, n.Stats[SortLogic.Life]);
    }

    [Fact]
    public void AnItemThatIsNotFood_HasNoStatNumber()
    {
        var n = SortLogic.ItemNumbers(OneUse(5, 5, 5, 5, 5), false, new EatLogic.Factors(), 10, 1, 1, 0, SortLogic.DaysLeft.NoSpoil);
        Assert.All(n.Stats, v => Assert.Null(v));
        Assert.Equal(10, n.Trade);
    }

    [Fact]
    public void TradeValue_IsTheWholeStack_WithAppraisal_HalfUp()
    {
        // 5 x 4 uses x 2 items
        Assert.Equal(40, SortLogic.ItemNumbers(OneUse(0, 0, 0, 0, 0), true, new EatLogic.Factors(), 5, 4, 2, 0, Fresh).Trade);
        // 22 x 1 x 1 x 1.15 = 25.3
        Assert.Equal(25, SortLogic.ItemNumbers(OneUse(0, 0, 0, 0, 0), true, new EatLogic.Factors(), 22, 1, 1, 0.15f, Fresh).Trade);
        // 5 x 1 x 1 x 1.1 = 5.5
        Assert.Equal(6, SortLogic.ItemNumbers(OneUse(0, 0, 0, 0, 0), true, new EatLogic.Factors(), 5, 1, 1, 0.1f, Fresh).Trade);
    }

    [Fact]
    public void NoTradeValue_HasNoNumber()
    {
        Assert.Null(SortLogic.ItemNumbers(OneUse(1, 0, 0, 0, 0), true, new EatLogic.Factors(), 0, 3, 1, 0.2f, Fresh).Trade);
    }

    [Theory]
    [InlineData(3f, "3d")]
    [InlineData(3.9f, "3d")]
    [InlineData(10f, "10d")]
    [InlineData(0.5f, "0.5d")]
    [InlineData(0.04f, "0.1d")]
    public void DaysText(float days, string expected)
    {
        Assert.Equal(expected, SortLogic.DaysText(days));
    }

    [Fact]
    public void DaysText_TheDayUnitOfTheLanguage()
    {
        Assert.Equal("3天", SortLogic.DaysText(3.9f, "天"));
        Assert.Equal("0.5天", SortLogic.DaysText(0.5f, "天"));
        Assert.Equal("3d", SortLogic.DaysText(3.9f, null));
    }

    [Fact]
    public void DaysLeft_Kinds()
    {
        Assert.True(SortLogic.DaysLeft.Expired.IsExpired);
        Assert.False(SortLogic.DaysLeft.NoSpoil.HasNumber);
        Assert.True(SortLogic.DaysLeft.Spoils(0.5f).HasNumber);
        Assert.Equal(0.5f, SortLogic.DaysLeft.Spoils(0.5f).Days);
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(true, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, false, false, false)]
    public void IsSortable_FridgeCookingStorageOrTheBagOfTheCookingWindow(bool isFridge, bool linksToCooking, bool isCookingBag, bool expected)
    {
        Assert.Equal(expected, SortLogic.IsSortable(isFridge, linksToCooking, isCookingBag));
    }
}

// The satiety that the Rat Cage gets from one item (Furniture.GetRatCageFoodSatiety of game 1.1.18293).
public class CageSatietyTests
{
    [Theory]
    // Watermelon: raw satiety 60, one use
    [InlineData(null, 60f, 1, 0, 1, 60)]
    // Hardtack with 2 of 10 uses left: 20 x 2
    [InlineData(null, 20f, 2, 10, 10, 40)]
    // A cooked dish: its own satiety, not the raw one, 32.4 x 3 = 97.2
    [InlineData(32.4f, 5f, 3, 3, 3, 97)]
    // No uses count: the effective max, the item's own max first
    [InlineData(null, 10f, 0, 3, 5, 30)]
    [InlineData(null, 10f, 0, 0, 5, 50)]
    [InlineData(null, 10f, 0, 0, 0, 10)]
    // Half away from zero: 2.5 x 1
    [InlineData(null, 2.5f, 1, 1, 1, 3)]
    public void SatietyTimesTheUsesLeft(float? instanceFirst, float valueDisplay1, int useTimes, int maxUseTimes, int cfgUses, int expected)
    {
        var instance = instanceFirst.HasValue ? new[] { instanceFirst.Value, 1f, 1f, 1f, 1f } : null;
        Assert.Equal(expected, SortLogic.CageSatiety(instance, valueDisplay1, useTimes, maxUseTimes, cfgUses));
    }

    [Fact]
    public void NoSatiety_HasNoNumber()
    {
        // a tool, an item with a negative satiety, and a dish whose own satiety is 0 (the raw one does not count)
        Assert.Null(SortLogic.CageSatiety(null, 0f, 1, 1, 1));
        Assert.Null(SortLogic.CageSatiety(null, -5f, 1, 1, 1));
        Assert.Null(SortLogic.CageSatiety(new[] { 0f, 3f }, 20f, 1, 1, 1));
    }

    [Fact]
    public void AnEmptyInstanceArray_ReadsTheRawSatiety()
    {
        Assert.Equal(35, SortLogic.CageSatiety(new float[0], 35f, 1, 1, 1));
    }
}

public class SortDataJsonTests
{
    private static readonly SortLogic.Words Words = new SortLogic.Words
    {
        Choices = new[] { "Default", "Satiety", "Morale", "Stamina", "Life", "Trade value", "Expiration Date" },
        Expired = "Expired",
        Sort = "Sort",
    };

    [Fact]
    public void NumbersByLogicId_WithTheOwnerAndTheWords()
    {
        var items = new List<KeyValuePair<long, SortLogic.Numbers>>
        {
            new KeyValuePair<long, SortLogic.Numbers>(8589969913, new SortLogic.Numbers
            {
                ConfigId = 2109,
                Stats = new int?[] { 14, -3, null, 2 },
                Trade = 40,
                Days = SortLogic.DaysLeft.Spoils(0.5f),
                Cage = 42,
            }),
            new KeyValuePair<long, SortLogic.Numbers>(7, new SortLogic.Numbers
            {
                ConfigId = 556,
                Stats = new int?[] { null, null, null, null },
                Trade = null,
                Days = SortLogic.DaysLeft.Expired,
            }),
            new KeyValuePair<long, SortLogic.Numbers>(8, new SortLogic.Numbers
            {
                ConfigId = 556,
                Stats = new int?[] { 5, null, null, null },
                Trade = 3,
                Days = SortLogic.DaysLeft.NoSpoil,
            }),
        };
        var json = PageJson.SortDataJson(4294986608, items, Words);
        Assert.Equal(
            "{\"owner\":\"4294986608\",\"words\":{\"choices\":[\"Default\",\"Satiety\",\"Morale\",\"Stamina\",\"Life\",\"Trade value\",\"Expiration Date\"],\"expired\":\"Expired\",\"sort\":\"Sort\"},"
            + "\"items\":{\"8589969913\":{\"n\":[14,-3,null,2,40,0.5,42],\"d\":\"0.5d\",\"c\":2109},\"7\":{\"n\":[null,null,null,null,null,-100000,null],\"d\":\"Expired\",\"c\":556},\"8\":{\"n\":[5,null,null,null,3,null,null],\"d\":null,\"c\":556}}}",
            json);
    }

    [Fact]
    public void TheDaysUseTheDayUnitOfTheWords()
    {
        var items = new List<KeyValuePair<long, SortLogic.Numbers>>
        {
            new KeyValuePair<long, SortLogic.Numbers>(1, new SortLogic.Numbers { ConfigId = 5, Stats = new int?[4], Days = SortLogic.DaysLeft.Spoils(12.4f) }),
        };
        var chinese = new SortLogic.Words { Choices = Words.Choices, Expired = "已过期", Sort = "排序", DayUnit = "天" };
        Assert.Contains("\"d\":\"12天\"", PageJson.SortDataJson(1, items, chinese));
    }

    [Fact]
    public void TheBagOwnerOfTheStorageWindow_FollowsTheOwner()
    {
        var json = PageJson.SortDataJson(42, new List<KeyValuePair<long, SortLogic.Numbers>>(), Words, 5);
        Assert.StartsWith("{\"owner\":\"42\",\"bag\":\"5\",\"words\":", json);
        Assert.DoesNotContain("\"bag\"", PageJson.SortDataJson(42, new List<KeyValuePair<long, SortLogic.Numbers>>(), Words));
    }
}

public class SortWordsTests
{
    [Fact]
    public void English_GameWordsForTheStatsDefaultExpirationAndExpired()
    {
        var w = SortLogic.WordsFrom(PreviewLogic.EnglishWords, "Default", "Expired", "Expiration Date", false);
        Assert.Equal(new[] { "Default", "Satiety", "Morale", "Stamina", "Life", "Trade value", "Expiration Date" }, w.Choices);
        Assert.Equal("Expired", w.Expired);
        Assert.Equal("Sort", w.Sort);
        Assert.Equal("d", w.DayUnit);
    }

    [Fact]
    public void Chinese_GameWordForExpiration_ModWordForSort()
    {
        var w = SortLogic.WordsFrom(WordsTests.Chinese(), "默认", "已过期", "保质期", true);
        Assert.Equal("默认", w.Choices[0]);
        Assert.Equal("保质期", w.Choices[6]);
        Assert.Equal("已过期", w.Expired);
        Assert.Equal("排序", w.Sort);
        Assert.Equal("天", w.DayUnit);
    }

    [Fact]
    public void NoGameText_GivesTheModWord()
    {
        var w = SortLogic.WordsFrom(PreviewLogic.EnglishWords, "", null, "", false);
        Assert.Equal("Default", w.Choices[0]);
        Assert.Equal("Expiration Date", w.Choices[6]);
        Assert.Equal("Expired", w.Expired);
        var zh = SortLogic.WordsFrom(WordsTests.Chinese(), null, "", null, true);
        Assert.Equal("默认", zh.Choices[0]);
        Assert.Equal("保质期", zh.Choices[6]);
        Assert.Equal("已过期", zh.Expired);
    }
}

public class TradeUsesTests
{
    [Theory]
    [InlineData(4, 4, 4, 4f)]
    [InlineData(2, 4, 8, 4f)]
    [InlineData(0, 0, 3, 1f)]
    [InlineData(3, 0, 3, 3f)]
    [InlineData(1, 1, 0, 1f)]
    public void UsesLeftTimesConfigUses(int useTimes, int maxUseTimes, int cfgUses, float expected)
    {
        Assert.Equal(expected, SortLogic.TradeUses(useTimes, maxUseTimes, cfgUses), 3);
    }
}

public class DaysLeftOfTests
{
    // Life 10 days (240 h), started at hour 0, now at hour 120, no fridge: 5 days left.
    [Fact]
    public void BeforeTheExpiry_TheDaysLeft()
    {
        var d = SortLogic.DaysLeftOf(10, 1.5f, 0, 240, 240, 1f, 1f, 120f, false);
        Assert.True(d.HasNumber);
        Assert.False(d.IsExpired);
        Assert.Equal(5f, d.Days, 3);
    }

    // Expired at hour 240, rot at 1.5 x 240 = 360 h of life; now 300: 60 h = 2.5 days to spoil.
    [Fact]
    public void Expired_TheDaysUntilItSpoils()
    {
        var d = SortLogic.DaysLeftOf(10, 1.5f, 0, 240, 240, 1f, 1f, 300f, false);
        Assert.True(d.IsExpired);
        Assert.True(d.HasNumber);
        Assert.Equal(2.5f, d.Days, 3);
    }

    // A fridge (time scale 0.2) makes the life pass slower: 60 h of life left take 300 h.
    [Fact]
    public void Expired_InAFridge_TheTimeScaleSlowsTheSpoil()
    {
        var d = SortLogic.DaysLeftOf(10, 1.5f, 0, 240, 240, 0.2f, 1f, 300f, false);
        // progress = (240 - (240 - 300) x 0.2) / 240 = 1.05; (1.5 - 1.05) x 240 / 0.2 = 540 h
        Assert.Equal(22.5f, d.Days, 3);
    }

    [Fact]
    public void Expired_PastTheRotThreshold_IsRotten()
    {
        var d = SortLogic.DaysLeftOf(10, 1.5f, 0, 240, 240, 1f, 1f, 400f, false);
        Assert.True(d.IsRotten);
        Assert.False(d.HasNumber);
    }

    [Fact]
    public void Expired_NoRotThreshold_IsJustExpired()
    {
        var d = SortLogic.DaysLeftOf(10, 1f, 0, 240, 240, 1f, 1f, 300f, false);
        Assert.True(d.IsExpired);
        Assert.False(d.HasNumber);
        Assert.False(d.IsRotten);
    }

    [Fact]
    public void NoLife_OrCompostFrozen_DoesNotSpoil()
    {
        Assert.False(SortLogic.DaysLeftOf(0, 1.5f, 0, 240, 240, 1f, 1f, 300f, false).IsExpired);
        Assert.False(SortLogic.DaysLeftOf(0, 1.5f, 0, 240, 240, 1f, 1f, 300f, false).HasNumber);
        Assert.False(SortLogic.DaysLeftOf(10, 1.5f, 0, 240, 240, 1f, 1f, 120f, true).HasNumber);
    }

    [Fact]
    public void SortKey_RottenFirst_ThenExpired_ThenTheDaysUntilItSpoils_ThenTheDaysLeft()
    {
        float Key(SortLogic.DaysLeft d) => SortLogic.DaysKey(d).Value;
        Assert.True(Key(SortLogic.DaysLeft.Rotten) < Key(SortLogic.DaysLeft.Expired));
        Assert.True(Key(SortLogic.DaysLeft.Expired) < Key(SortLogic.DaysLeft.ExpiredSpoils(0.5f)));
        Assert.True(Key(SortLogic.DaysLeft.ExpiredSpoils(0.5f)) < Key(SortLogic.DaysLeft.ExpiredSpoils(3f)));
        Assert.True(Key(SortLogic.DaysLeft.ExpiredSpoils(3000f)) < Key(SortLogic.DaysLeft.Spoils(0.1f)));
        Assert.Null(SortLogic.DaysKey(SortLogic.DaysLeft.NoSpoil));
    }
}

public class MergeTests
{
    private static KeyValuePair<long, SortLogic.Numbers> Item(long id, int satiety) =>
        new KeyValuePair<long, SortLogic.Numbers>(id, new SortLogic.Numbers { ConfigId = (int)id, Stats = new int?[] { satiety, null, null, null } });

    [Fact]
    public void FirstGridItemsFirst_ThenTheOtherItemsThatItDoesNotHave()
    {
        var tab = new[] { Item(1, 5), Item(2, 30) };
        var workbench = new[] { Item(9, 12), Item(2, 99) };
        var merged = SortLogic.Merge(tab, workbench);
        Assert.Equal(new long[] { 1, 2, 9 }, merged.Select(kv => kv.Key).ToArray());
        Assert.Equal(30, merged[1].Value.Stats[0]);
    }

    [Fact]
    public void NoOtherGrid_IsTheFirst()
    {
        var tab = new[] { Item(1, 5) };
        Assert.Equal(new long[] { 1 }, SortLogic.Merge(tab, null).Select(kv => kv.Key).ToArray());
    }
}
