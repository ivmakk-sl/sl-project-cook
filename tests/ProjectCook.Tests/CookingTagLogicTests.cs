using System.Collections.Generic;
using ProjectCook;
using Xunit;

public class CookingTagLogicTests
{
    [Fact]
    public void RowValues_FollowTheStatusRow()
    {
        Assert.Equal(1900, CookingTagLogic.TagId);
        Assert.Equal(3, CookingTagLogic.Dim);
        Assert.Equal(320, CookingTagLogic.Order);
        Assert.Equal(4, CookingTagLogic.SortPriority);
        Assert.Equal("#E07A3C", CookingTagLogic.Color);
        Assert.Equal("cook", CookingTagLogic.IconKey);
        Assert.Equal("FurnitureTag_TagName_1900", CookingTagLogic.NameKey);
        Assert.Equal("FurnitureTag_TagDesc_1900", CookingTagLogic.DescKey);
    }

    [Fact]
    public void Words_English()
    {
        var w = CookingTagLogic.WordsFor(1);
        Assert.Equal("Cooking", w.Name);
        Assert.Equal("Food here shows as a tab of the cooking window. It does not change which food goes here.", w.Desc);
    }

    [Fact]
    public void Words_Chinese()
    {
        var w = CookingTagLogic.WordsFor(0);
        Assert.Equal("烹饪", w.Name);
        Assert.Equal("这里的食物作为烹饪台的一个页签接入，直接取用；不会改变哪些食物放进来。", w.Desc);
    }

    [Fact]
    public void Words_UnknownLanguage_IsEnglish()
    {
        Assert.Equal("Cooking", CookingTagLogic.WordsFor(7).Name);
    }

    [Fact]
    public void RuleForGame_DropsTheCookingTag()
    {
        Assert.Equal(new[] { 1010, 1006 }, CookingTagLogic.RuleForGame(new[] { 1010, 1900, 1006 }));
    }

    [Fact]
    public void RuleForGame_CookingAlone_IsFood()
    {
        Assert.Equal(new[] { 1001 }, CookingTagLogic.RuleForGame(new[] { 1900 }));
    }

    [Fact]
    public void RuleForGame_WithoutCookingTag_IsTheSameList()
    {
        var rule = new[] { 1010, 1006 };
        Assert.Same(rule, CookingTagLogic.RuleForGame(rule));
    }

    [Fact]
    public void RuleForGame_EmptyRule_IsTheSameList()
    {
        var rule = new int[0];
        Assert.Same(rule, CookingTagLogic.RuleForGame(rule));
    }

    [Fact]
    public void ExtraTabs_KeepsTaggedStoragesThatAreNotFridges_InTheirOrder()
    {
        var tabs = CookingTagLogic.ExtraTabs(
            new long[] { 10, 11 },
            new[] { new CookingTagLogic.Storage(30, 501), new CookingTagLogic.Storage(11, 401), new CookingTagLogic.Storage(20, 502) },
            2);
        Assert.Equal(2, tabs.Count);
        Assert.Equal(30, tabs[0].OwnerId);
        Assert.Equal(501, tabs[0].ConfigId);
        Assert.False(tabs[0].Locked);
        Assert.Equal(20, tabs[1].OwnerId);
        Assert.Equal(502, tabs[1].ConfigId);
    }

    [Fact]
    public void ExtraTabs_BelowCookingLevel2_AreLocked()
    {
        var tabs = CookingTagLogic.ExtraTabs(new long[0], new[] { new CookingTagLogic.Storage(30, 501) }, 1);
        Assert.True(tabs[0].Locked);
    }

    [Fact]
    public void ExtraTabs_NoTaggedStorage_IsEmpty()
    {
        Assert.Empty(CookingTagLogic.ExtraTabs(new long[] { 10 }, new List<CookingTagLogic.Storage>(), 3));
    }

    [Theory]
    [InlineData(new[] { 1900 }, true)]
    [InlineData(new[] { 1001 }, true)]
    [InlineData(new[] { 1006, 1001 }, true)]
    [InlineData(new[] { 1021 }, false)]
    [InlineData(new[] { 1003, 1006 }, false)]
    [InlineData(new int[0], false)]
    public void LinksToCooking_TheCookingTagOrTheGameTagFood(int[] tagIds, bool expected)
    {
        Assert.Equal(expected, CookingTagLogic.LinksToCooking(tagIds));
    }
}
