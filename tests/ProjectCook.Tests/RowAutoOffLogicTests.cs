using ProjectCook;
using Xunit;

public class RowAutoOffLogicTests
{
    private static RowSplitLogic.RecipeKey Exact(int id) => new RowSplitLogic.RecipeKey(id, 0, null);
    private static RowSplitLogic.RecipeKey Tag(int id, int tier, params int[] tags) => new RowSplitLogic.RecipeKey(id, tier, tags);
    private static RowSplitLogic.RecipeKey[] Keys(params RowSplitLogic.RecipeKey[] keys) => keys;

    [Fact]
    public void Starts_ForAnExactRecipeOverTwoRows()
    {
        Assert.True(RowAutoOff.Starts(Exact(1001), Keys(Exact(1001)), Keys(Exact(6036), Exact(4022))));
    }

    [Fact]
    public void NoStart_WhenThePlanHoldsTheRecipe()
    {
        Assert.False(RowAutoOff.Starts(Exact(3003), Keys(Exact(3003)), Keys(Exact(3003), Exact(3004))));
    }

    [Fact]
    public void NoStart_WhenTheGamesListDoesNotHoldTheRecipe()
    {
        Assert.False(RowAutoOff.Starts(Exact(1001), Keys(Exact(6036)), Keys(Exact(4022))));
    }

    [Fact]
    public void Ends_WhenThePlanHoldsTheRecipeOrAnotherTierOfIt()
    {
        Assert.True(RowAutoOff.Ends(Exact(3003), Keys(Exact(3003), Exact(3004))));
        Assert.True(RowAutoOff.Ends(Tag(10, 2, 1, 2), Keys(Tag(11, 1, 2, 1))));
        Assert.False(RowAutoOff.Ends(Exact(1001), Keys(Exact(6036))));
    }

    [Fact]
    public void Starts_ForATagRecipeOfAnotherTierOfTheDish()
    {
        Assert.True(RowAutoOff.Starts(Tag(5004, 2, 7, 1), Keys(Tag(6004, 3, 7, 1)), Keys(Exact(4022))));
    }

    [Fact]
    public void NoStart_WhenThePlanHoldsAnotherTierOfTheDish()
    {
        Assert.False(RowAutoOff.Starts(Tag(5004, 2, 7, 1), Keys(Tag(6004, 3, 7, 1)), Keys(Tag(7004, 1, 7, 1))));
    }

    [Fact]
    public void Active_ForItsOwnerOnly()
    {
        var off = new RowAutoOff();
        Assert.False(off.IsActive(7));
        off.Begin(7, new long[] { 1, 2 });
        Assert.True(off.IsActive(7));
        Assert.False(off.IsActive(8));
    }

    [Fact]
    public void Keep_EndsWhenAnItemLeaves()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1, 2 });
        Assert.True(off.Keep(7, new long[] { 1 }));
        Assert.False(off.IsActive(7));
    }

    [Fact]
    public void Keep_StaysWithAnAddedItem()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1, 2 });
        Assert.False(off.Keep(7, new long[] { 3, 2, 1 }));
        Assert.True(off.IsActive(7));
    }

    [Fact]
    public void Keep_IgnoresAnotherOwner()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1, 2 });
        Assert.False(off.Keep(8, new long[] { 9 }));
        Assert.True(off.IsActive(7));
    }

    [Fact]
    public void Begin_AtAnotherOwnerReplacesTheEntry()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1 });
        off.Begin(8, new long[] { 2 });
        Assert.False(off.IsActive(7));
        Assert.True(off.IsActive(8));
    }

    [Fact]
    public void Click_EndsAutoOffOfItsOwner()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1 });
        Assert.True(off.Click(7));
        Assert.False(off.IsActive(7));
    }

    [Fact]
    public void Click_WithNoAutoOffOfTheOwnerChangesNothing()
    {
        var off = new RowAutoOff();
        Assert.False(off.Click(7));
        off.Begin(7, new long[] { 1 });
        Assert.False(off.Click(8));
        Assert.True(off.IsActive(7));
    }

    [Theory]
    [InlineData("{\"recipeId\":1001}", 1001)]
    [InlineData("{\"other\":1}", 0)]
    [InlineData("not json", 0)]
    [InlineData(null, 0)]
    public void ClickedRecipe_ReadsTheIdOfTheClick(string json, int expected)
    {
        Assert.Equal(expected, RowAutoOff.ClickedRecipe(json));
    }

    [Fact]
    public void Clear_EndsAutoOff()
    {
        var off = new RowAutoOff();
        off.Begin(7, new long[] { 1 });
        off.Clear();
        Assert.False(off.IsActive(7));
    }
}
