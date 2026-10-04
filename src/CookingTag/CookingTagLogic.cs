using System.Collections.Generic;
using SlShared.ModTags;

namespace ProjectCook
{
    // Game-free logic of the cooking tag. This file must not use any game or BepInEx type, because the unit tests
    // compile it alone.
    public static class CookingTagLogic
    {
        // The row of the game's FurnitureTag table that the mod adds, with the id ModTagRule.CookingTagId and the keys
        // of the mod tags library. Dim 3 is the Status row, Order 320 puts it after Chilled (310), and SortPriority is
        // the value of Food.
        public const int Dim = 3, Order = 320, SortPriority = 4;
        public const string Color = "#E07A3C", IconKey = "cook";

        // A fridge tab of the cooking window is locked below this cooking level (CookingFridgeInfo.UnlockCookingLevel).
        public const int UnlockCookingLevel = 2;

        public readonly struct Words
        {
            public readonly string Name, Desc;
            public Words(string name, string desc) { Name = name; Desc = desc; }
        }

        public readonly struct Storage
        {
            public readonly long OwnerId;
            public readonly int ConfigId;
            public Storage(long ownerId, int configId) { OwnerId = ownerId; ConfigId = configId; }
        }

        public readonly struct Tab
        {
            public readonly long OwnerId;
            public readonly int ConfigId;
            public readonly bool Locked;
            public Tab(long ownerId, int configId, bool locked) { OwnerId = ownerId; ConfigId = configId; Locked = locked; }
        }

        private static readonly Words English = new Words(
            "Cooking", "Food here shows as a tab of the cooking window. It does not change which food goes here.");

        // 烹饪 is the game's word in 开始烹饪 (SR_Web_Cooking_16). The description uses the words of the game's
        // cooking guide (SR_Web_Cooking_Guide_15: 家里每台冰箱作为一个页签接入，直接取用) and of the Chilled tag (放进来).
        private static readonly Words Chinese = new Words(
            "烹饪", "这里的食物作为烹饪台的一个页签接入，直接取用；不会改变哪些食物放进来。");

        // languageType is the game's LanguageType: 0 is Chinese. Each other language gets the English words.
        public static Words WordsFor(int languageType) => languageType == 0 ? Chinese : English;

        // The tabs that the cooking tag adds after the fridge tabs: each tagged storage that is not a fridge, in the
        // order of tagged.
        // A storage links to the cooking window (a container tab, the food sort) with the cooking tag or the tag Food,
        // because Food takes every food. When the cooking tag is disabled (the game took its id), only Food links.
        public static bool LinksToCooking(IEnumerable<int> tagIds, bool disabled)
        {
            foreach (var id in tagIds)
                if ((id == ModTagRule.CookingTagId && !disabled) || id == ModTagRule.FoodTagId) return true;
            return false;
        }

        public static List<Tab> ExtraTabs(IReadOnlyList<long> fridgeOwners, IReadOnlyList<Storage> tagged, int cookingLevel)
        {
            var fridges = new HashSet<long>(fridgeOwners);
            var locked = cookingLevel < UnlockCookingLevel;
            var tabs = new List<Tab>();
            foreach (var s in tagged)
                if (!fridges.Contains(s.OwnerId)) tabs.Add(new Tab(s.OwnerId, s.ConfigId, locked));
            return tabs;
        }
    }
}
