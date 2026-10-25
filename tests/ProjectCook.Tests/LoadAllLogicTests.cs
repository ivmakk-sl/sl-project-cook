using System.Collections.Generic;
using System.Linq;
using ProjectCook;
using Xunit;
using static ProjectCook.LoadAllLogic;

// The searches of the fill over all container tabs: the game's rule of Reducer_Web_Cooking.SelectItemsByConfigId and
// SelectItemsByTagTiered (read with NativeDisasm in game 1.1.19611, design.md), over a longer owner list.
public class LoadAllLogicTests
{
    private const long Station = 900, Open = 901, Fridge = 902, Storage = 903;

    private static Item Food(long id, long owner, int config, int tag = 0, int tier = 0) => new Item(id, owner, config, true, tag, tier);

    private static KeyValuePair<int, int>[] Need(params int[] pairs)
    {
        var list = new KeyValuePair<int, int>[pairs.Length / 2];
        for (int i = 0; i < list.Length; i++) list[i] = new KeyValuePair<int, int>(pairs[2 * i], pairs[2 * i + 1]);
        return list;
    }

    // The owner list: the station, the open tab, then the other tabs in tab order, with no locked tab.

    [Fact]
    public void Owners_StationOpenTabThenTheOtherTabsInTabOrder()
    {
        var tabs = new[] { new Tab(Fridge, false), new Tab(Open, false), new Tab(Storage, false) };
        Assert.Equal(new[] { Station, Open, Fridge, Storage }, Owners(Station, Open, tabs));
    }

    [Fact]
    public void Owners_NoLockedTab()
    {
        var tabs = new[] { new Tab(Open, false), new Tab(Fridge, true), new Tab(Storage, false) };
        Assert.Equal(new[] { Station, Open, Storage }, Owners(Station, Open, tabs));
    }

    [Fact]
    public void Owners_OpenTabNotInTheTabRowStillComesSecond()
    {
        Assert.Equal(new[] { Station, Open, Fridge }, Owners(Station, Open, new[] { new Tab(Fridge, false) }));
    }

    // The search by config id.

    [Fact]
    public void ByConfigId_TwoOwners_TheGameRule()
    {
        // The game walks the station, then the open tab, in list order, and takes the first N of each needed id.
        var items = new[] { Food(1, Station, 10), Food(2, Open, 20), Food(3, Open, 10), Food(4, Open, 10) };
        Assert.Equal(new long[] { 1, 3, 2 }, SelectByConfigId(new Pool(items), Need(10, 2, 20, 1)));
    }

    [Fact]
    public void ByConfigId_ItemsOfTheOtherTabsAfterTheOpenTab()
    {
        var items = new[] { Food(1, Open, 10), Food(2, Fridge, 10), Food(3, Fridge, 20), Food(4, Storage, 10) };
        Assert.Equal(new long[] { 1, 2, 3 }, SelectByConfigId(new Pool(items), Need(10, 2, 20, 1)));
    }

    [Fact]
    public void ByConfigId_NullWhenAnIdIsShort()
    {
        var items = new[] { Food(1, Open, 10), Food(2, Fridge, 20) };
        Assert.Null(SelectByConfigId(new Pool(items), Need(10, 2, 20, 1)));
    }

    [Fact]
    public void ByConfigId_NullWhenAnIdIsMissing()
    {
        Assert.Null(SelectByConfigId(new Pool(new[] { Food(1, Open, 10) }), Need(10, 1, 30, 1)));
    }

    [Fact]
    public void ByConfigId_OnlyCookableItems()
    {
        var items = new[] { new Item(1, Open, 10, false, 0, 0), Food(2, Fridge, 10) };
        Assert.Equal(new long[] { 2 }, SelectByConfigId(new Pool(items), Need(10, 1)));
    }

    // The search by tag and tier.

    [Fact]
    public void Rank_OfEachTier()
    {
        Assert.Equal(3, Rank(1));
        Assert.Equal(2, Rank(2));
        Assert.Equal(1, Rank(3));
        Assert.Equal(0, Rank(0));
        Assert.Equal(0, Rank(4));
    }

    [Fact]
    public void ByTag_TwoOwners_TheGameRule()
    {
        // Tier 2 required (rank 2): the first pass takes the items of rank 2 in order, the second pass the items of a
        // lower rank or rank 0; an item of a higher rank (tier 1) is never taken.
        var items = new[]
        {
            Food(1, Station, 10, tag: 5, tier: 1),
            Food(2, Station, 11, tag: 5, tier: 3),
            Food(3, Open, 12, tag: 5, tier: 2),
            Food(4, Open, 13, tag: 5, tier: 0),
            Food(5, Open, 14, tag: 5, tier: 2),
        };
        Assert.Equal(new long[] { 3, 5, 2 }, SelectByTagTiered(new Pool(items), Need(5, 3), 2, null));
        Assert.Equal(new long[] { 3, 5, 2, 4 }, SelectByTagTiered(new Pool(items), Need(5, 4), 2, null));
        Assert.Null(SelectByTagTiered(new Pool(items), Need(5, 5), 2, null));
    }

    [Fact]
    public void ByTag_NoTierRequired_AnyItemOfTheTagInOrder()
    {
        var items = new[] { Food(1, Station, 10, tag: 5, tier: 1), Food(2, Open, 11, tag: 6), Food(3, Open, 12, tag: 5, tier: 3) };
        Assert.Equal(new long[] { 1, 3 }, SelectByTagTiered(new Pool(items), Need(5, 2), 0, null));
    }

    [Fact]
    public void ByTag_EachItemOnceOverAllTags()
    {
        var items = new[] { Food(1, Open, 10, tag: 5), Food(2, Open, 11, tag: 6), Food(3, Fridge, 12, tag: 5) };
        Assert.Equal(new long[] { 1, 3, 2 }, SelectByTagTiered(new Pool(items), Need(5, 2, 6, 1), 0, null));
    }

    [Fact]
    public void ByTag_TheExcludeSetOfConfigIds()
    {
        var items = new[] { Food(1, Open, 10, tag: 5), Food(2, Fridge, 11, tag: 5) };
        Assert.Equal(new long[] { 2 }, SelectByTagTiered(new Pool(items), Need(5, 1), 0, new HashSet<int> { 10 }));
    }

    [Fact]
    public void ByTag_OnlyCookableItems()
    {
        var items = new[] { new Item(1, Open, 10, false, 5, 0), Food(2, Fridge, 11, tag: 5) };
        Assert.Equal(new long[] { 2 }, SelectByTagTiered(new Pool(items), Need(5, 1), 0, null));
    }

    [Fact]
    public void ByTag_ItemsOfTheOtherTabsAfterTheOpenTab()
    {
        var items = new[] { Food(1, Open, 10, tag: 5, tier: 3), Food(2, Fridge, 11, tag: 5, tier: 2), Food(3, Storage, 12, tag: 5, tier: 2) };
        Assert.Equal(new long[] { 2, 3 }, SelectByTagTiered(new Pool(items), Need(5, 2), 2, null));
    }

    // The counts of the Recipe Book: one for each cookable item of the other tabs, by its config id and its tag, added
    // to the game's counts of the station and the open tab.

    [Fact]
    public void Counts_AddTheItemsOfTheOtherTabs()
    {
        var items = new[]
        {
            Food(1, Station, 10, tag: 5), Food(2, Open, 10, tag: 5),
            Food(3, Fridge, 10, tag: 5), Food(4, Fridge, 20, tag: 6), Food(5, Storage, 10, tag: 5),
            new Item(6, Storage, 30, false, 7, 0),
        };
        var ids = new Dictionary<int, int> { [10] = 2 };
        var tags = new Dictionary<int, int> { [5] = 2 };

        AddCounts(items, Station, Open, ids, tags);

        Assert.Equal(new Dictionary<int, int> { [10] = 4, [20] = 1 }, ids);
        Assert.Equal(new Dictionary<int, int> { [5] = 4, [6] = 1 }, tags);
    }

    [Fact]
    public void Counts_NothingWithNoOtherTab()
    {
        var ids = new Dictionary<int, int> { [10] = 1 };
        var tags = new Dictionary<int, int>();
        AddCounts(new[] { Food(1, Open, 10, tag: 5) }, Station, Open, ids, tags);
        Assert.Equal(new Dictionary<int, int> { [10] = 1 }, ids);
        Assert.Empty(tags);
    }

    // The counts go in once for each refresh of the Recipe Book: at the first evaluation after the mark of a refresh.

    [Fact]
    public void RefreshMark_TheFirstCallAfterAMarkOnly()
    {
        var mark = new RefreshMark();
        Assert.False(mark.TakeFirst());
        mark.Mark();
        Assert.True(mark.TakeFirst());
        Assert.False(mark.TakeFirst());
        Assert.False(mark.TakeFirst());
        mark.Mark();
        Assert.True(mark.TakeFirst());
        Assert.False(mark.TakeFirst());
    }

    [Fact]
    public void Needed_CountsEachIdInTheOrderOfItsFirstPlace()
    {
        Assert.Equal(Need(5, 2, 7, 1, 3, 1), Needed(new[] { 5, 7, 5, 3 }));
        Assert.Empty(Needed(new int[0]));
    }

    // The pool groups the items once: a search reads only the items of its config ids or its tags, in the order of the
    // places (the work grows with the matching items, not with all items for each recipe).
    [Fact]
    public void Pool_GroupsTheItemsByConfigIdAndTagInOrder()
    {
        var pool = new Pool(new[] { Food(1, Open, 10, tag: 5), Food(2, Open, 20, tag: 6), Food(3, Fridge, 10, tag: 5), new Item(4, Fridge, 10, false, 5, 0) });
        Assert.Equal(new long[] { 1, 3 }, pool.ByConfigId(10).Select(i => i.Id));
        Assert.Equal(new long[] { 1, 3 }, pool.ByTag(5).Select(i => i.Id));
        Assert.Empty(pool.ByConfigId(30));
        Assert.Equal(4, pool.Items.Count);
    }

    [Fact]
    public void Cookable_TheRuleOfTheSearches()
    {
        Assert.True(IsCookable(1, true, 0));
        Assert.False(IsCookable(2, true, 0));
        Assert.False(IsCookable(1, false, 0));
        Assert.False(IsCookable(1, true, 7));
    }
}
