using ProjectCook;
using Xunit;

public class IconTabsLogicTests
{
    private static readonly IconTabsLogic.Tab[] Tabs =
    {
        new IconTabsLogic.Tab(1, "Character Inventory", false),
        new IconTabsLogic.Tab(2, "Fridge", true),
        new IconTabsLogic.Tab(3, "Large Storage Rack", false),
    };

    [Fact]
    public void Entries_TheFirstTabIsTheBag_InTheOrderOfTheWindow()
    {
        var e = IconTabsLogic.Entries(Tabs, null);
        Assert.Equal(new[] { true, false, false }, new[] { e[0].Bag, e[1].Bag, e[2].Bag });
        Assert.Equal(new long[] { 1, 2, 3 }, new[] { e[0].OwnerId, e[1].OwnerId, e[2].OwnerId });
        Assert.Equal("Large Storage Rack", e[2].Name);
    }

    [Fact]
    public void Entries_ALockedTab_CarriesTheLockText()
    {
        var e = IconTabsLogic.Entries(Tabs, "Unlocks at Cooking Lv.2");
        Assert.Null(e[0].Lock);
        Assert.Equal("Unlocks at Cooking Lv.2", e[1].Lock);
        Assert.Null(e[2].Lock);
    }

    // The cooking window keeps a lock text on each tab (Data_Web_Cooking_BagTab.LockText); it wins over the text of
    // the window.
    [Fact]
    public void Entries_TheLockTextOfTheTab_WinsOverTheTextOfTheWindow()
    {
        var tabs = new[] { new IconTabsLogic.Tab(1, "Bag", false), new IconTabsLogic.Tab(2, "Fridge", true, "Cooking Lv.2") };
        Assert.Equal("Cooking Lv.2", IconTabsLogic.Entries(tabs, "window text")[1].Lock);
        Assert.Equal("window text", IconTabsLogic.Entries(new[] { tabs[0], new IconTabsLogic.Tab(2, "Fridge", true, "") }, "window text")[1].Lock);
    }

    // The list goes to the root page script, which keeps it for the frame of its window. The script can be missing
    // (a new root page), so the answer "no script" tries again one second later, while the request is young.
    [Fact]
    public void Send_ARequest_GoesOnTheNextTick_Once()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        Assert.Equal("A", s.Tick(0f));
        Assert.Null(s.Tick(0.1f));
        s.OnResult("A", "stored", 0.2f);
        Assert.Null(s.Tick(5f));
    }

    [Fact]
    public void Send_NoScriptOrNoAnswer_TriesAgainAfterOneSecond()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        s.Tick(0f);
        s.OnResult("A", PageJson.NoScript, 0.1f);
        Assert.Null(s.Tick(0.5f));
        Assert.Equal("A", s.Tick(1.2f));
        s.OnResult("A", null, 1.3f);
        Assert.Equal("A", s.Tick(2.4f));
    }

    // Another full send of the script is in flight: the list does not carry the script too (two full sends at once can
    // crash the browser), and tries again soon, when the script is there.
    [Fact]
    public void Send_ScriptBusy_TriesAgainSoon()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        s.Tick(0f);
        s.OnResult("A", IconTabsSend.ScriptBusy, 0.05f);
        Assert.Null(s.Tick(0.1f));
        Assert.Equal("A", s.Tick(0.2f));
    }

    [Fact]
    public void Send_AnOldRequest_IsDropped()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        s.Tick(0f);
        s.OnResult("A", PageJson.NoScript, 4.8f);
        Assert.Null(s.Tick(6f));
    }

    [Fact]
    public void Send_ANewRequest_Replaces_AndALateAnswerOfTheOldOneKeepsIt()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        s.Tick(0f);
        s.Request("B", 0.1f);
        s.OnResult("A", "stored", 0.2f);
        Assert.Equal("B", s.Tick(0.3f));
    }

    [Fact]
    public void Send_ALostAnswer_FreesTheSendAfterFiveSeconds()
    {
        var s = new IconTabsSend();
        s.Request("A", 0f);
        s.Tick(0f);
        Assert.Null(s.Tick(1f));
        s.Request("B", 2f);
        Assert.Null(s.Tick(3f));
        Assert.Equal("B", s.Tick(5.1f));
    }
}

