using System.Collections.Generic;
using SlShared.I18n;
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

        // The lock of the fridge tabs of a food window (AE_Open<Window>Panel.CollectFridges): the cooking, Brewing
        // Barrel, and Ration Press windows lock them below UnlockCookingLevel, the Rat Cage window never locks them.
        public enum LockRule { LockBelowLevel2, NeverLocked }

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

        // The name and the description of the tag, from the mod texts of one language.
        internal static Words WordsFrom(I18nTextSet texts) => new Words(texts["cookingTagName"], texts["cookingTagDesc"]);

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

        // listedOwners are the owners that the window lists already (the fridges, and the tool cabinets of the Ration
        // Press), so a listed storage gets no second tab.
        public static List<Tab> ExtraTabs(IReadOnlyList<long> listedOwners, IReadOnlyList<Storage> tagged, int cookingLevel, LockRule rule)
        {
            var fridges = new HashSet<long>(listedOwners);
            var locked = rule == LockRule.LockBelowLevel2 && cookingLevel < UnlockCookingLevel;
            var tabs = new List<Tab>();
            foreach (var s in tagged)
                if (!fridges.Contains(s.OwnerId)) tabs.Add(new Tab(s.OwnerId, s.ConfigId, locked));
            return tabs;
        }
    }
}
