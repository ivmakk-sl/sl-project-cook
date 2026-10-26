using ProjectCook;
using Xunit;

public class SwitchesLogicTests
{
    private static SwitchesLogic.Values All(bool on) => new SwitchesLogic.Values
    {
        DishPreview = on, CodexMarks = on, UnknownWithIngredients = on, UnknownWithoutIngredients = on, SeparatePieces = on, DimUncookable = on,
    };

    [Fact]
    public void The_page_features_of_the_switches_that_are_on_in_a_fixed_order()
    {
        Assert.Equal(new[] { "codexMarks", "unknownCodexRecipes", "separatePieces", "dimUncookable", "portionSwitch" }, SwitchesLogic.PageFeatures(All(true)));
        Assert.Empty(SwitchesLogic.PageFeatures(All(false)));
    }

    [Fact]
    public void The_unknown_recipes_need_the_codex_marks_and_one_of_their_two_switches()
    {
        var v = All(false);
        v.UnknownWithIngredients = true;
        Assert.Empty(SwitchesLogic.PageFeatures(v));
        v.CodexMarks = true;
        Assert.Equal(new[] { "codexMarks", "unknownCodexRecipes" }, SwitchesLogic.PageFeatures(v));
        v.UnknownWithIngredients = false;
        v.UnknownWithoutIngredients = true;
        Assert.Equal(new[] { "codexMarks", "unknownCodexRecipes" }, SwitchesLogic.PageFeatures(v));
    }

    [Fact]
    public void The_portion_switch_is_part_of_the_dish_preview()
    {
        var v = All(false);
        v.DishPreview = true;
        Assert.Equal(new[] { "portionSwitch" }, SwitchesLogic.PageFeatures(v));
    }

    [Fact]
    public void Two_lists_of_the_same_switches_are_equal_so_a_change_of_another_entry_sends_nothing()
    {
        Assert.True(SwitchesLogic.Same(SwitchesLogic.PageFeatures(All(true)), SwitchesLogic.PageFeatures(All(true))));
        Assert.False(SwitchesLogic.Same(SwitchesLogic.PageFeatures(All(true)), SwitchesLogic.PageFeatures(All(false))));
    }

    [Fact]
    public void Only_the_food_sort_turned_off_clears_the_sort_data_of_the_page()
    {
        Assert.True(SwitchesLogic.ClearsSort("FoodSort", false));
        Assert.False(SwitchesLogic.ClearsSort("FoodSort", true));
        Assert.False(SwitchesLogic.ClearsSort("DishPreview", false));
        Assert.Equal("null", SwitchesLogic.NoSortJson);
    }
}
