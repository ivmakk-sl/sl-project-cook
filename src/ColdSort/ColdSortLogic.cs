using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free logic of the targets that the mod adds to the game's [Store Chilled Food] button: the group of a
    // storage and its level in the game's sort. This file must not use any game or BepInEx type, because the unit
    // tests compile it alone.
    public static class ColdSortLogic
    {
        // The tag Chilled (FurnitureTag 1006). A rule with it stays with the game's own ranking.
        public const int ChilledTagId = 1006;

        // FurnitureTagComponent.TryGetPutRank gives the lowest SortPriority of the matching rows (1 to 5 in the
        // FurnitureTag table), and 99 as its fallback, so no tag level is above 99. ItemManager.TryRankQuickStoreTarget
        // gives a fridge with an empty rule the level 1000.
        public const int MaxTagLevel = 99, EmptyFridgeLevel = 1000;

        // The game sorts the targets by level, lowest first. A band keeps each mod group in its place: a food fridge
        // after each Chilled storage and before the fridge with an empty rule, a cold room storage after it.
        public const int FoodFridgeBand = 100, ColdRoomBand = 1100;

        public enum StoreGroup { None, FoodFridge, ColdRoom }

        public readonly struct Facts
        {
            // IsFridge: the game's fridge test. InColdRoom: the storage is in the Cold Storage area. RoomCold: the
            // room keeps food cold (sealed, or a food decay scale below 1). CookingDisabled: the cooking tag counts
            // for nothing (the game took its id, or the cooking storages are off).
            public readonly bool IsFridge, InColdRoom, RoomCold, CookingDisabled;
            public readonly IReadOnlyList<int> TagIds;
            public Facts(bool isFridge, bool inColdRoom, bool roomCold, IReadOnlyList<int> tagIds, bool cookingDisabled)
            { IsFridge = isFridge; InColdRoom = inColdRoom; RoomCold = roomCold; TagIds = tagIds; CookingDisabled = cookingDisabled; }
        }

        // Config_Furniture.ElectricalType of a power consumer (the game enum ElectricalType.Consumer).
        public const int ConsumerElectricalType = 1;

        // The fridge test of the game (ItemManager.IsFridge), from the fields of Config_Furniture: an electric power
        // consumer with a cold rate and a power cost. A cold rate alone does not mark a fridge.
        public static bool IsFridge(bool isElectrical, int electricalType, float coldRate, int powerCost) =>
            isElectrical && electricalType == ConsumerElectricalType && coldRate > 0f && powerCost > 0;

        public static StoreGroup GroupOf(Facts f)
        {
            if (f.TagIds == null || f.TagIds.Count == 0) return StoreGroup.None;
            foreach (var id in f.TagIds)
                if (id == ChilledTagId) return StoreGroup.None;
            if (!CookingTagLogic.LinksToCooking(f.TagIds, f.CookingDisabled)) return StoreGroup.None;
            if (f.IsFridge) return StoreGroup.FoodFridge;
            return f.InColdRoom && f.RoomCold ? StoreGroup.ColdRoom : StoreGroup.None;
        }

        // The days left of an item at the normal speed, from its days left at its own speed (ItemData.TimeScale). The
        // game re-bases the time left of an item when its scale changes, so in the Cold Storage room (scale 0.2 with
        // the chiller on) the backpack food has five times the days, and the game no longer counts it as food that
        // needs cold. A scale of 0 or less counts as 1, as in FoodPreservationRules.GetRemainingDays.
        public static float NormalSpeedDays(float days, float timeScale) => timeScale > 0f ? days * timeScale : days;

        // The level that the mod gives the game for a storage of a group, from the level of TryGetPutRank.
        public static int Level(StoreGroup group, int tagLevel) =>
            (group == StoreGroup.ColdRoom ? ColdRoomBand : FoodFridgeBand) + tagLevel;
    }
}
