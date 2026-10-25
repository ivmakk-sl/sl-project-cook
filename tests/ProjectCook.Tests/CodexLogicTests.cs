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

    // Recipes 10 and 11 are one dish (10), recipe 20 is its own dish.
    private static int DishOf(int recipeId) => recipeId == 11 ? 10 : recipeId;

    [Fact]
    public void DishesInProgress_MapsEachCookedRecipeToItsDish() =>
        Assert.Equal(new[] { 10 }, DishesInProgress(new[] { 11, 0 }, DishOf));

    [Fact]
    public void WithoutInProgress_RemovesEachRecipeOfTheDish()
    {
        var ids = new System.Collections.Generic.HashSet<int> { 10, 11, 20 };
        Assert.Equal(new[] { 20 }, WithoutInProgress(ids, DishesInProgress(new[] { 10 }, DishOf), DishOf));
    }

    [Fact]
    public void WithoutInProgress_EmptySet_GivesTheSameIds()
    {
        var ids = new System.Collections.Generic.HashSet<int> { 10, 11, 20 };
        Assert.Same(ids, WithoutInProgress(ids, new System.Collections.Generic.HashSet<int>(), DishOf));
    }

    [Fact]
    public void BookKey_NewCook_Differs() => Assert.NotEqual(new BookKey(false, 41), new BookKey(false, 41, new[] { 10 }));

    [Fact]
    public void BookKey_StoppedCook_Differs() => Assert.NotEqual(new BookKey(false, 41, new[] { 10 }), new BookKey(false, 41, new int[0]));

    [Fact]
    public void BookKey_SameDishesInProgress_IsEqual() =>
        Assert.Equal(new BookKey(false, 41, new[] { 10, 20 }), new BookKey(false, 41, new[] { 20, 10 }));

    // The unknown recipes: recipes 10 and 11 are dish 10, 20 is its own dish, 30 is known, 40 is craft-locked, 50 has a
    // codex entry (not in the book ids), 60 is not allowed by the station.
    private static readonly System.Collections.Generic.HashSet<int> Book = new System.Collections.Generic.HashSet<int> { 10, 11, 20, 30, 40, 60 };
    private static readonly int[] Allowed = { 10, 11, 20, 30, 40, 50 };

    private static System.Collections.Generic.List<int> Pick(int[] rowGroupIds, bool evaluated = true, bool hotPot = false) =>
        UnknownCandidates(Allowed, Book, rowGroupIds, DishOf, id => id == 30, id => id != 40, evaluated, hotPot);

    [Fact]
    public void UnknownCandidates_AllowedInTheBookNotKnownCraftUnlocked() =>
        Assert.Equal(new[] { 10, 11, 20 }, Pick(new[] { 30 }));

    [Fact]
    public void UnknownCandidates_NoneForADishWithARow() =>
        // A single row of recipe 11 has the recipe id as its groupId: its dish is 10.
        Assert.Equal(new[] { 20 }, Pick(new[] { 30, 11 }));

    [Fact]
    public void UnknownCandidates_NoneWithoutAnEvaluation() => Assert.Empty(Pick(new int[0], evaluated: false));

    [Fact]
    public void UnknownCandidates_NoneInHotPotMode() => Assert.Empty(Pick(new[] { 30 }, hotPot: true));

    [Fact]
    public void UnknownCandidates_NoneWithoutBookIds() =>
        Assert.Empty(UnknownCandidates(Allowed, null, new[] { 30 }, DishOf, id => false, id => true, true, false));

    private static readonly System.Collections.Generic.Dictionary<int, int> Station = new System.Collections.Generic.Dictionary<int, int> { [1] = 2 };
    private static readonly System.Collections.Generic.Dictionary<int, int> Total = new System.Collections.Generic.Dictionary<int, int> { [1] = 3, [2] = 1 };
    private static int StationCount(int id) => Station.TryGetValue(id, out int n) ? n : 0;
    private static int TotalCount(int id) => Total.TryGetValue(id, out int n) ? n : 0;

    private static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>> Need(params int[] idCount)
    {
        var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>();
        for (int i = 0; i < idCount.Length; i += 2) list.Add(new System.Collections.Generic.KeyValuePair<int, int>(idCount[i], idCount[i + 1]));
        return list;
    }

    [Theory]
    [InlineData(new[] { 1, 2 }, 2)]          // the station holds 2 of id 1
    [InlineData(new[] { 1, 3 }, 1)]          // only the totals hold 3
    [InlineData(new[] { 1, 2, 2, 1 }, 1)]    // id 2 is only in the totals
    [InlineData(new[] { 1, 4 }, 0)]
    [InlineData(new int[0], 0)]              // a recipe with no ingredient list
    public void CountStatus_StationThenTotalsThenNot(int[] idCount, int expected) =>
        Assert.Equal(expected, CountStatus(Need(idCount), StationCount, TotalCount));

    [Fact]
    public void CountStatus_TagRecipeReadsTheTagCounts()
    {
        // The same rule over the tag counts: the caller passes the tag dictionaries for a tag recipe.
        var stationTags = new System.Collections.Generic.Dictionary<int, int> { [7] = 1 };
        var totalTags = new System.Collections.Generic.Dictionary<int, int> { [7] = 2 };
        Assert.Equal(1, CountStatus(Need(7, 2), id => stationTags.TryGetValue(id, out int n) ? n : 0, id => totalTags.TryGetValue(id, out int n) ? n : 0));
    }

    [Theory]
    [InlineData(2, true, false, 2)]    // Ready and in the pot: no plan check
    [InlineData(2, false, true, 1)]    // Ready but not the pot's dish: the plan gives Load
    [InlineData(2, false, false, 0)]
    [InlineData(1, false, true, 1)]
    [InlineData(1, true, false, 0)]
    [InlineData(0, false, true, 0)]    // not enough: no plan check
    public void GameStatus_FollowsEvaluateRecipeForBook(int count, bool inPot, bool plan, int expected)
    {
        int plans = 0;
        Assert.Equal(expected, GameStatus(count, inPot, () => { plans++; return plan; }));
        Assert.Equal(count == 0 || (count == 2 && inPot) ? 0 : 1, plans);
    }

    private static UnknownTexts Texts(int id) => new UnknownTexts("Dish " + id, "Cost " + id, "Effect " + id);

    private static readonly System.Collections.Generic.Dictionary<int, int> RowStatus = new System.Collections.Generic.Dictionary<int, int> { [10] = 1, [11] = 0, [20] = 2 };

    [Fact]
    public void UnknownRows_WithIngredients_GroupIdIsTheDish()
    {
        var rows = UnknownRows(new[] { 10, 11, 20 }, DishOf, id => RowStatus[id], Texts, withIngredients: true, withoutIngredients: false);

        Assert.Equal(new[] { 10, 20 }, rows.ConvertAll(r => r.RecipeId));
        Assert.Equal(new[] { 10, 20 }, rows.ConvertAll(r => r.GroupId));
        Assert.Equal("Dish 20", rows[1].Name);
        Assert.Equal("Cost 20", rows[1].CostText);
        Assert.Equal("Effect 20", rows[1].EffectText);
        Assert.Equal(2, rows[1].Status);
    }

    [Fact]
    public void UnknownRows_WithoutIngredients_OnlyStatusZero()
    {
        var rows = UnknownRows(new[] { 10, 11, 20 }, DishOf, id => RowStatus[id], Texts, withIngredients: false, withoutIngredients: true);

        Assert.Single(rows);
        Assert.Equal(11, rows[0].RecipeId);
        Assert.Equal(10, rows[0].GroupId);
        Assert.Equal(0, rows[0].Status);
    }

    [Fact]
    public void UnknownRows_BothSwitches_AllRows() =>
        Assert.Equal(new[] { 10, 11, 20 }, UnknownRows(new[] { 10, 11, 20 }, DishOf, id => RowStatus[id], Texts, true, true).ConvertAll(r => r.RecipeId));

    [Fact]
    public void TextCache_BuildsOnceForEachRecipeAndLanguage()
    {
        var cache = new TextCache();
        int builds = 0;
        UnknownTexts Build(int id) { builds++; return Texts(id); }

        cache.Get(10, 1, Build);
        cache.Get(10, 1, Build);
        Assert.Equal(1, builds);
        cache.Get(20, 1, Build);
        Assert.Equal(2, builds);
        Assert.Equal("Dish 10", cache.Get(10, 0, Build).Name);
        Assert.Equal(3, builds);
    }
}
