using ProjectCook;
using Xunit;
using static ProjectCook.CodexLogic;

public class CodexLogicTests
{
    [Fact]
    public void DishSource_DarkDish_IsStruggleMeal() => Assert.Equal(StruggleMealRecipeId, DishSource(0));

    [Fact]
    public void DishSource_StruggleMealIs100() => Assert.Equal(100, StruggleMealRecipeId);

    [Fact]
    public void DishSource_RecipeStays() => Assert.Equal(1234, DishSource(1234));

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(1, false, false)]
    [InlineData(2, true, false)]
    [InlineData(0, true, false)]
    public void IsFoodEntry_FoodCategoryInCodexOnly(int category, bool inCodex, bool expected) =>
        Assert.Equal(expected, IsFoodEntry(category, inCodex));

    [Theory]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, true)]
    public void ShowMark_OnlyForAnEntryNotUnlockedWhileOpen(bool locked, bool isEntry, bool unlocked, bool expected) =>
        Assert.Equal(expected, ShowMark(locked, isEntry, unlocked));

    [Fact]
    public void BookKey_SameLockAndCount_IsEqual() => Assert.Equal(new BookKey(false, 41), new BookKey(false, 41));

    [Fact]
    public void BookKey_NewDishEntry_Differs() => Assert.NotEqual(new BookKey(false, 41), new BookKey(false, 42));

    [Fact]
    public void BookKey_LockChange_Differs() => Assert.NotEqual(new BookKey(false, 41), new BookKey(true, 41));
}
