using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free logic of the cooking tag. This file must not use any game or BepInEx type, because the unit tests
    // compile it alone.
    public static class CookingTagLogic
    {
        // The row of the game's FurnitureTag table that the mod adds. The id is far from the game's ids (1001-1032),
        // Dim 3 is the Status row, Order 320 puts it after Chilled (310), and SortPriority is the value of Food.
        public const int TagId = 1900, Dim = 3, Order = 320, SortPriority = 4;
        public const string Color = "#E07A3C", IconKey = "cook";
        public const string NameKey = "FurnitureTag_TagName_1900", DescKey = "FurnitureTag_TagDesc_1900";

        // The game's tag Food. With only the cooking tag, the game sees this rule. A storage with Food also links to
        // the cooking window, because Food takes every food.
        public const int FoodTagId = 1001;

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

        // The rule that the game's tag logic sees: the rule without the cooking tag, or Food when the cooking tag is
        // the only tag. A rule without the cooking tag comes back as the same list.
        public static IReadOnlyList<int> RuleForGame(IReadOnlyList<int> rule)
        {
            var at = -1;
            for (var i = 0; i < rule.Count; i++)
                if (rule[i] == TagId) { at = i; break; }
            if (at < 0) return rule;
            var result = new List<int>(rule.Count);
            for (var i = 0; i < rule.Count; i++)
                if (rule[i] != TagId) result.Add(rule[i]);
            if (result.Count == 0) result.Add(FoodTagId);
            return result;
        }

        // The tabs that the cooking tag adds after the fridge tabs: each tagged storage that is not a fridge, in the
        // order of tagged.
        // A storage links to the cooking window (a container tab, the food sort) with the cooking tag or the tag Food.
        public static bool LinksToCooking(IEnumerable<int> tagIds)
        {
            foreach (var id in tagIds)
                if (id == TagId || id == FoodTagId) return true;
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
