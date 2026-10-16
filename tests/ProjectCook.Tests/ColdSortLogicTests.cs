using ProjectCook;
using SlShared.ModTags;
using Xunit;
using static ProjectCook.ColdSortLogic;

public class ColdSortLogicTests
{
    private const int Meat = 1021;

    // Config_Furniture values of game 1.1.18902 (Furniture.tsv, by id): IsElectrical, ElectricalType, ColdRate,
    // PowerCost.
    [Theory]
    [InlineData("Fridge 215", true, 1, 0.2f, 2)]
    [InlineData("Refrigerator 431", true, 1, 0.2f, 1)]
    [InlineData("Double-Door Fridge 15000", true, 1, 0.2f, 2)]
    [InlineData("Deluxe Fridge 80062", true, 1, 0.2f, 1)]
    [InlineData("Freezer 15001", true, 1, 0.2f, 6)]
    [InlineData("Large-Capacity Freezer 15002", true, 1, 0.2f, 9)]
    [InlineData("Display Freezer 107", true, 1, 0.1f, 4)]
    public void IsFridge_EachFridge(string name, bool electrical, int type, float coldRate, int powerCost)
    {
        Assert.True(IsFridge(electrical, type, coldRate, powerCost), name);
    }

    [Theory]
    [InlineData("Storage Cabinet 67079", false, 0, 0.2f, 0)]
    [InlineData("Medium Storage Rack 393", false, 0, 0f, 0)]
    [InlineData("Cold Storage Chiller 42056", true, 1, 0f, 30)]
    [InlineData("electric, type 2", true, 2, 0.2f, 2)]
    [InlineData("no power cost", true, 1, 0.2f, 0)]
    public void IsFridge_NotAFridge(string name, bool electrical, int type, float coldRate, int powerCost)
    {
        Assert.False(IsFridge(electrical, type, coldRate, powerCost), name);
    }

    private static Facts Fridge(params int[] tags) => new Facts(true, false, false, tags, false);
    private static Facts Shelf(bool inRoom, bool roomCold, params int[] tags) => new Facts(false, inRoom, roomCold, tags, false);

    [Fact]
    public void FridgeWithFood_IsFoodFridge() => Assert.Equal(StoreGroup.FoodFridge, GroupOf(Fridge(ModTagRule.FoodTagId)));

    [Fact]
    public void FridgeWithOnlyCooking_IsFoodFridge() => Assert.Equal(StoreGroup.FoodFridge, GroupOf(Fridge(ModTagRule.CookingTagId)));

    [Fact]
    public void FridgeWithMeatAndFood_IsFoodFridge() => Assert.Equal(StoreGroup.FoodFridge, GroupOf(Fridge(Meat, ModTagRule.FoodTagId)));

    [Fact]
    public void FridgeWithMeatAndCooking_IsFoodFridge() => Assert.Equal(StoreGroup.FoodFridge, GroupOf(Fridge(Meat, ModTagRule.CookingTagId)));

    [Fact]
    public void FridgeWithOnlyMeat_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Fridge(Meat)));

    [Fact]
    public void FridgeWithChilledAndFood_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Fridge(ChilledTagId, ModTagRule.FoodTagId)));

    [Fact]
    public void FridgeWithEmptyRule_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Fridge()));

    [Fact]
    public void FridgeWithNullRule_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(new Facts(true, false, false, null, false)));

    [Fact]
    public void ShelfWithFood_InColdRoom_IsColdRoom() => Assert.Equal(StoreGroup.ColdRoom, GroupOf(Shelf(true, true, ModTagRule.FoodTagId)));

    [Fact]
    public void ShelfWithCooking_InColdRoom_IsColdRoom() => Assert.Equal(StoreGroup.ColdRoom, GroupOf(Shelf(true, true, ModTagRule.CookingTagId)));

    [Fact]
    public void ShelfWithFood_InWarmRoom_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Shelf(true, false, ModTagRule.FoodTagId)));

    [Fact]
    public void ShelfWithFood_OutsideTheRoom_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Shelf(false, true, ModTagRule.FoodTagId)));

    [Fact]
    public void ShelfWithEmptyRule_InColdRoom_IsNone() => Assert.Equal(StoreGroup.None, GroupOf(Shelf(true, true)));

    [Fact]
    public void FridgeInColdRoom_IsFoodFridge() =>
        Assert.Equal(StoreGroup.FoodFridge, GroupOf(new Facts(true, true, true, new[] { ModTagRule.FoodTagId }, false)));

    [Fact]
    public void Cooking_WithTheTagDisabled_IsNone()
    {
        Assert.Equal(StoreGroup.None, GroupOf(new Facts(true, false, false, new[] { ModTagRule.CookingTagId }, true)));
        Assert.Equal(StoreGroup.None, GroupOf(new Facts(false, true, true, new[] { ModTagRule.CookingTagId }, true)));
    }

    [Fact]
    public void Food_WithTheCookingTagDisabled_StaysFoodFridge() =>
        Assert.Equal(StoreGroup.FoodFridge, GroupOf(new Facts(true, false, false, new[] { ModTagRule.FoodTagId }, true)));

    // The levels of TryGetPutRank are 1 to 99 (SortPriority, 99 as the fallback); the game gives a fridge with an
    // empty rule 1000.
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(MaxTagLevel)]
    public void Level_FoodFridge_IsAboveEachChilledLevel_AndBelowTheEmptyFridge(int tagLevel)
    {
        var level = Level(StoreGroup.FoodFridge, tagLevel);
        Assert.True(level > MaxTagLevel);
        Assert.True(level < EmptyFridgeLevel);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(MaxTagLevel)]
    public void Level_ColdRoom_IsAboveTheEmptyFridge(int tagLevel)
    {
        Assert.True(Level(StoreGroup.ColdRoom, tagLevel) > EmptyFridgeLevel);
    }

    // In the Cold Storage room the game re-bases the days left of the backpack food to the slower speed (TimeScale
    // 0.2 with the chiller on): 35 days there are 7 days at the normal speed.
    [Theory]
    [InlineData(35f, 0.2f, 7f)]
    [InlineData(7f, 1f, 7f)]
    [InlineData(500f, 0.002f, 1f)]
    [InlineData(7f, 0f, 7f)]
    [InlineData(7f, -1f, 7f)]
    public void NormalSpeedDays_TheDaysLeftAtTheNormalSpeed(float days, float timeScale, float expected)
    {
        Assert.Equal(expected, NormalSpeedDays(days, timeScale), 3);
    }

    [Fact]
    public void Level_KeepsTheTagOrder_InsideEachGroup()
    {
        Assert.True(Level(StoreGroup.FoodFridge, 2) < Level(StoreGroup.FoodFridge, 4));
        Assert.True(Level(StoreGroup.ColdRoom, 2) < Level(StoreGroup.ColdRoom, 4));
        Assert.True(Level(StoreGroup.FoodFridge, MaxTagLevel) < Level(StoreGroup.ColdRoom, 1));
    }
}
