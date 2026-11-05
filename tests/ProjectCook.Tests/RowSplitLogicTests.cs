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

public class RowSplitSplitIdsTests
{
    // A fake of the game's entry builder: each row takes each ID that its multiset still holds and skips the others,
    // as the builder skips a recipe that does not match the items left.
    private static Func<int, int, int, int> Builder(int[] ids, params int[][] rows) => (row, offset, k) =>
    {
        var left = rows[row].ToList();
        int count = 0;
        for (int i = offset; i < offset + k; i++)
            if (left.Remove(ids[i])) count++;
        return count;
    };

    [Fact]
    public void SplitIds_EachRowTakesTheNextIds()
    {
        int[] ids = { 10, 11, 20 };
        Assert.Equal(new[] { 2, 1 }, RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new[] { 10, 11 }, new[] { 20 })));
    }

    [Fact]
    public void SplitIds_ARowWithNoIdTakesNone()
    {
        int[] ids = { 20 };
        Assert.Equal(new[] { 0, 1 }, RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new int[0], new[] { 20 })));
    }

    [Fact]
    public void SplitIds_ARejectedIdEndsTheRowBeforeIt()
    {
        // The top row takes 10, rejects 20, and would take 11 again: it stops at 10, so 20 and 11 are left.
        int[] ids = { 10, 20, 11 };
        Assert.Null(RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new[] { 10, 11 }, new[] { 20 })));
        Assert.Equal(new[] { 1, 2 }, RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new[] { 10, 11 }, new[] { 20, 11 })));
    }

    [Fact]
    public void SplitIds_IdsLeftAfterTheLastRowGiveNull()
    {
        int[] ids = { 10, 30 };
        Assert.Null(RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new[] { 10 }, new[] { 20 })));
    }

    [Fact]
    public void SplitIds_NoRowTakesAnIdGivesNull()
    {
        int[] ids = { 30 };
        Assert.Null(RowSplitLogic.SplitIds(2, ids.Length, Builder(ids, new[] { 10 }, new[] { 20 })));
    }

    [Fact]
    public void SplitIds_NoIdGivesNull()
    {
        Assert.Null(RowSplitLogic.SplitIds(2, 0, Builder(new int[0], new[] { 10 }, new[] { 20 })));
    }
}

// The cooking mode of a row (game 1.1.20719): Reducer_Web_Cooking.UseExactFirst and AlignTagDishTierToMix.
public class RowSplitCookingModeTests
{
    private static RowSplitLogic.RecipeKey Exact(int id) => new RowSplitLogic.RecipeKey(id, 0, null);
    private static RowSplitLogic.RecipeKey Tag(int id, int tier, params int[] combo) => new RowSplitLogic.RecipeKey(id, tier, combo);

    [Theory]
    [InlineData(4, 4, 1, true)]
    [InlineData(5, 4, 1, true)]
    [InlineData(5, 4, 0, false)]
    [InlineData(3, 4, 1, false)]
    [InlineData(5, 0, 1, false)]
    public void ExactFirst_FromTheUnlockLevelInSeparateOnly(int cookingLevel, int unlockLevel, int dishPriority, bool expected)
    {
        Assert.Equal(expected, RowSplitLogic.ExactFirst(cookingLevel, unlockLevel, dishPriority));
    }

    [Fact]
    public void Align_ATagDishTakesTheRecipeOfItsMixedTwin()
    {
        // Separate gives the Low tier tag recipe 501, Mixed the High tier recipe 503 of the same tag combo.
        var ids = RowSplitLogic.AlignToMix(new[] { Exact(10), Tag(501, 3, 7, 1) }, new[] { Tag(503, 1, 1, 7), Exact(10) });
        Assert.Equal(new[] { 10, 503 }, ids);
    }

    [Fact]
    public void Align_NothingWhenTheCountsDiffer()
    {
        // Three Roast Potatoes in Separate against one dish in Mixed: the lists differ, so Separate keeps its own.
        var ids = RowSplitLogic.AlignToMix(new[] { Exact(20), Exact(20), Exact(20) }, new[] { Tag(601, 2, 4, 4, 4) });
        Assert.Equal(new[] { 20, 20, 20 }, ids);
    }

    [Fact]
    public void Align_StopsAtTheFirstDishWithNoTwin()
    {
        // The game's loop returns at the first dish with no same dish in Mixed; the dishes before it stay aligned.
        var ids = RowSplitLogic.AlignToMix(new[] { Tag(501, 3, 7, 1), Exact(30), Tag(502, 3, 7, 1) }, new[] { Tag(503, 1, 7, 1), Exact(40), Tag(504, 1, 7, 1) });
        Assert.Equal(new[] { 503, 30, 502 }, ids);
    }

    [Fact]
    public void Align_EachMixedDishOnce()
    {
        var ids = RowSplitLogic.AlignToMix(new[] { Tag(501, 3, 7, 1), Tag(502, 3, 7, 1) }, new[] { Tag(503, 1, 7, 1), Tag(504, 2, 1, 7) });
        Assert.Equal(new[] { 503, 504 }, ids);
    }

    [Fact]
    public void SameDish_ByIdOrByTheTagComboOfTwoTieredRecipes()
    {
        Assert.True(RowSplitLogic.IsSameDish(Exact(10), Exact(10)));
        Assert.False(RowSplitLogic.IsSameDish(Exact(10), Exact(11)));
        Assert.True(RowSplitLogic.IsSameDish(Tag(501, 3, 7, 1, 1), Tag(503, 1, 1, 7, 1)));
        Assert.False(RowSplitLogic.IsSameDish(Tag(501, 3, 7, 1), Tag(503, 1, 7, 1, 1)));
        Assert.False(RowSplitLogic.IsSameDish(Tag(501, 0, 7, 1), Tag(503, 1, 7, 1)));
    }
}
