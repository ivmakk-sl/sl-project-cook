using ProjectCook;
using Xunit;

// The hold of the refreshes of the cooking window during a click on Load: the refreshes that the item moves of the
// click start are held, and the click ends with one refresh when at least one was held.
public class FastLoadLogicTests
{
    [Fact]
    public void ClickWithTenHeldRefreshes_GivesOneRefreshAtItsEnd()
    {
        var hold = new FastLoadLogic();
        hold.Begin();
        for (int i = 0; i < 10; i++) Assert.True(hold.TryHold());
        Assert.Equal(10, hold.Held);
        Assert.True(hold.End());
        Assert.Equal(0, hold.Held);
    }

    [Fact]
    public void ClickWithNoHeldRefresh_GivesNone()
    {
        var hold = new FastLoadLogic();
        hold.Begin();
        Assert.False(hold.End());
    }

    [Fact]
    public void NestedBegin_GivesTheRefreshOnlyAtTheOuterEnd()
    {
        var hold = new FastLoadLogic();
        hold.Begin();
        Assert.True(hold.TryHold());
        hold.Begin();
        Assert.True(hold.TryHold());
        Assert.False(hold.End());
        Assert.True(hold.TryHold());
        Assert.True(hold.End());
        Assert.False(hold.TryHold());
    }

    [Fact]
    public void AbortAfterAHeldRefresh_GivesTrueAndHoldsNothingAfterIt()
    {
        var hold = new FastLoadLogic();
        hold.Begin();
        hold.Begin();
        Assert.True(hold.TryHold());
        Assert.True(hold.Abort());
        Assert.Equal(0, hold.Held);
        Assert.False(hold.TryHold());
    }

    [Fact]
    public void AbortWithNoHeldRefresh_GivesFalse()
    {
        var hold = new FastLoadLogic();
        hold.Begin();
        Assert.False(hold.Abort());
    }

    [Fact]
    public void RefreshOutsideAClick_IsNotHeld()
    {
        var hold = new FastLoadLogic();
        Assert.False(hold.TryHold());
        hold.Begin();
        hold.End();
        Assert.False(hold.TryHold());
        Assert.Equal(0, hold.Held);
    }

    [Fact]
    public void InClick_IsTrueOnlyBetweenTheOuterBeginAndItsEnd()
    {
        var hold = new FastLoadLogic();
        Assert.False(hold.InClick);
        hold.Begin();
        hold.Begin();
        hold.End();
        Assert.True(hold.InClick);
        hold.End();
        Assert.False(hold.InClick);
        hold.Begin();
        hold.Abort();
        Assert.False(hold.InClick);
    }

    [Fact]
    public void EndWithNoBegin_GivesFalseAndStaysOutsideAClick()
    {
        var hold = new FastLoadLogic();
        Assert.False(hold.End());
        Assert.False(hold.TryHold());
    }
}
