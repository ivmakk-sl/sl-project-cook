namespace ProjectCook
{
    // Game-free rule of the codex mark: which dish card or food has no codex entry yet. This file must not use any
    // game or BepInEx type, because the unit tests compile it alone.
    public static class CodexLogic
    {
        // CodexCategory of the game: Food (the tasted foods) and Dish (the cooked dishes).
        public const int FoodCategory = 1, DishCategory = 2;

        // Config_CookingRecipe id of Struggle Meal: Furniture.SettleCookingResult cooks it when no recipe matches.
        public const int StruggleMealRecipeId = 100;

        // The recipe that a cook unlocks in the codex for a prediction entry: a dark dish (RecipeId 0) is Struggle Meal.
        public static int DishSource(int recipeId) => recipeId == 0 ? StruggleMealRecipeId : recipeId;

        // Config_Item.Category of a food.
        public const int FoodItemCategory = 1;

        // CodexManager.IsInCodex(Food, id): the item config is a food and has InCodex.
        public static bool IsFoodEntry(int itemCategory, bool inCodex) => itemCategory == FoodItemCategory && inCodex;

        // No mark while the game keeps the codex locked (guide and explore mode), for a thing with no codex entry, or
        // for an entry that is already unlocked.
        public static bool ShowMark(bool locked, bool isEntry, bool unlocked) => !locked && isEntry && !unlocked;
    }
}
