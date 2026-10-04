using ProjectCook;
using Xunit;
using Item = ProjectCook.RowSplitLogic.Item;

public class RowSplitRowsTests
{
    private static Item At(long id, int x, int y, int w = 1, int h = 1, int configId = 2001, int sub = 1)
        => new Item(id, configId, sub, x, y, w, h);

    [Fact]
    public void Rows_GroupByRowTopFirst()
    {
        var rows = RowSplitLogic.Rows(new[] { At(1, 0, 2), At(2, 0, 0), At(3, 3, 0), At(4, 1, 1) });
        Assert.Equal(new[] { 0, 1, 2 }, rows.Select(r => r.Y));
        Assert.Equal(new long[] { 2, 3 }, rows[0].Items.Select(i => i.Id));
        Assert.Equal(new long[] { 4 }, rows[1].Items.Select(i => i.Id));
        Assert.Equal(new long[] { 1 }, rows[2].Items.Select(i => i.Id));
    }

    [Fact]
    public void Rows_LeaveOutEmptyRows()
    {
        var rows = RowSplitLogic.Rows(new[] { At(1, 0, 0), At(2, 0, 2) });
        Assert.Equal(new[] { 0, 2 }, rows.Select(r => r.Y));
    }

    [Fact]
    public void Rows_ItemOfTwoByTwoCellsIsInTheRowOfItsTopCell()
    {
        // A 2 x 2 item in rows 0 and 1, and a 1-cell item beside its bottom cells.
        var rows = RowSplitLogic.Rows(new[] { At(1, 0, 0, 2, 2), At(2, 2, 1) });
        Assert.Equal(new[] { 0, 1 }, rows.Select(r => r.Y));
        Assert.Equal(new long[] { 1 }, rows[0].Items.Select(i => i.Id));
        Assert.Equal(new long[] { 2 }, rows[1].Items.Select(i => i.Id));
    }

    [Fact]
    public void Rows_NoItemGivesNoRow()
    {
        Assert.Empty(RowSplitLogic.Rows(new Item[0]));
    }
}

public class RowSplitAppliesTests
{
    [Fact]
    public void Applies_LevelThreeAndTwoRows()
    {
        Assert.True(RowSplitLogic.Applies(rowSplitOn: true, cookingLevel: 3, unlockLevel: 3, hotPot: false, rowCount: 2));
    }

    [Theory]
    [InlineData(true, 3, false, 1)]  // all ingredients in one row
    [InlineData(true, 2, false, 3)]  // cooking level below the unlock level
    [InlineData(true, 3, true, 3)]   // hot pot mode
    [InlineData(false, 3, false, 3)] // switch off
    [InlineData(true, 3, false, 0)]  // empty cooking station
    public void Applies_FalseCases(bool on, int level, bool hotPot, int rowCount)
    {
        Assert.False(RowSplitLogic.Applies(on, level, 3, hotPot, rowCount));
    }
}

public class RowSplitCountsTests
{
    [Fact]
    public void Counts_ByConfigIdAndBySubCategory_OneForEachItem()
    {
        // Two Pork Chops (meat, sub category 2) and a Cabbage (vegetable, sub category 3).
        var row = RowSplitLogic.Rows(new[]
        {
            new Item(1, 2101, 2, 0, 0, 1, 1),
            new Item(2, 2101, 2, 1, 0, 1, 1),
            new Item(3, 2201, 3, 2, 0, 1, 1),
        })[0];
        var counts = RowSplitLogic.Counts(row);
        Assert.Equal(new Dictionary<int, int> { [2101] = 2, [2201] = 1 }, counts.Ids);
        Assert.Equal(new Dictionary<int, int> { [2] = 2, [3] = 1 }, counts.Tags);
    }

    [Fact]
    public void Counts_KeysInTheOrderOfTheItems()
    {
        var row = RowSplitLogic.Rows(new[] { new Item(1, 2201, 3, 0, 0, 1, 1), new Item(2, 2101, 2, 1, 0, 1, 1) })[0];
        Assert.Equal(new[] { 2201, 2101 }, RowSplitLogic.Counts(row).Ids.Keys);
    }
}

public class RowSplitSignatureTests
{
    private static string Sig(long owner, params Item[] items) => RowSplitLogic.Signature(owner, RowSplitLogic.Rows(items));

    [Fact]
    public void Signature_ChangesWhenAnItemMovesToAnotherRow()
    {
        string before = Sig(7, new Item(1, 2101, 2, 0, 0, 1, 1), new Item(2, 2101, 2, 0, 1, 1, 1));
        string after = Sig(7, new Item(1, 2101, 2, 0, 0, 1, 1), new Item(2, 2101, 2, 1, 0, 1, 1));
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Signature_SameForAnotherListOrderAndAnotherColumn()
    {
        string a = Sig(7, new Item(1, 2101, 2, 0, 0, 1, 1), new Item(2, 2101, 2, 1, 0, 1, 1));
        string b = Sig(7, new Item(2, 2101, 2, 3, 0, 1, 1), new Item(1, 2101, 2, 0, 0, 1, 1));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Signature_ChangesWithTheOwner()
    {
        Assert.NotEqual(Sig(7, new Item(1, 2101, 2, 0, 0, 1, 1)), Sig(8, new Item(1, 2101, 2, 0, 0, 1, 1)));
    }
}

public class RowSplitRowOrderTests
{
    private static Item At(long id, int x, int y, int w = 1, int h = 1) => new Item(id, 2101, 2, x, y, w, h);

    [Fact]
    public void RowOrder_TopRowFirstAndStableInsideARow()
    {
        // List order: row 2, row 0, row 1, row 0.
        var order = RowSplitLogic.RowOrder(new[] { At(1, 0, 2), At(2, 3, 0), At(3, 0, 1), At(4, 0, 0) });
        Assert.Equal(new[] { 1, 3, 2, 0 }, order);
    }

    [Fact]
    public void RowOrder_ItemOfTwoByTwoCellsGoesWithItsTopRow()
    {
        var order = RowSplitLogic.RowOrder(new[] { At(1, 2, 1), At(2, 0, 0, 2, 2) });
        Assert.Equal(new[] { 1, 0 }, order);
    }

    [Fact]
    public void RowOrder_EmptyRowBetween()
    {
        var order = RowSplitLogic.RowOrder(new[] { At(1, 0, 3), At(2, 0, 0) });
        Assert.Equal(new[] { 1, 0 }, order);
    }

    [Fact]
    public void RowOrder_ListAlreadyInOrderStaysTheSame()
    {
        var order = RowSplitLogic.RowOrder(new[] { At(1, 0, 0), At(2, 1, 0), At(3, 0, 1) });
        Assert.Equal(new[] { 0, 1, 2 }, order);
    }
}

public class RowSplitWithSeasoningsTests
{
    [Fact]
    public void WithSeasonings_TagRecipe_AddsOneForEachSeasoningItem()
    {
        // A meat and a vegetable, with Honey (2155) and Salt (2151) on the cooking station, Honey used by no recipe.
        var participated = new Dictionary<int, int> { [2101] = 1, [2201] = 1 };
        var result = RowSplitLogic.WithSeasonings(participated, new[] { 2155, 2151, 2155 }, isExact: false);
        Assert.Equal(new Dictionary<int, int> { [2101] = 1, [2201] = 1, [2155] = 2, [2151] = 1 }, result);
        Assert.Equal(2, participated.Count);
    }

    [Fact]
    public void WithSeasonings_SeasoningAlsoUsedByTheRecipe_CountsTwice()
    {
        // The settle adds each seasoning item of the cooking station to a copy of the participated counts.
        var result = RowSplitLogic.WithSeasonings(new Dictionary<int, int> { [2151] = 1 }, new[] { 2151 }, isExact: false);
        Assert.Equal(2, result[2151]);
    }

    [Fact]
    public void WithSeasonings_ExactRecipe_SameCounts()
    {
        var result = RowSplitLogic.WithSeasonings(new Dictionary<int, int> { [2101] = 1 }, new[] { 2155 }, isExact: true);
        Assert.Equal(new Dictionary<int, int> { [2101] = 1 }, result);
    }
}
