using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free rules of Load from all tabs: the owner list of the fill, and the game's two item searches of the fill
    // (Reducer_Web_Cooking.SelectItemsByConfigId and SelectItemsByTagTiered, read with NativeDisasm in game 1.1.19611)
    // over that longer list. The game walks only the cooking station and the open container tab; with the same two
    // owners these copies give the game's result. This file must not use any game or BepInEx type, because the unit
    // tests compile it alone.
    public static class LoadAllLogic
    {
        // Config_Item.Category of a food.
        public const int FoodCategory = 1;

        // One item of a place of the fill: its id, its owner, its config id, the cookable rule, its SubCategory (the
        // tag of a tag recipe), and its ingredient tier (Reducer_Web_Cooking.ResolveIngredientTier).
        public readonly struct Item
        {
            public readonly long Id, Owner;
            public readonly int ConfigId, SubCategory, Tier;
            public readonly bool Cookable;

            public Item(long id, long owner, int configId, bool cookable, int subCategory, int tier)
            {
                Id = id; Owner = owner; ConfigId = configId; Cookable = cookable; SubCategory = subCategory; Tier = tier;
            }
        }

        // A container tab of the cooking window (Data_Web_Cooking_BagTab): its owner and its lock.
        public readonly struct Tab
        {
            public readonly long Owner;
            public readonly bool Locked;

            public Tab(long owner, bool locked) { Owner = owner; Locked = locked; }
        }

        // The cookable rule of both searches: a food that can be cooked and needs no cut (Category 1, CanCook,
        // CutProductId < 1).
        public static bool IsCookable(int category, bool canCook, int cutProductId) =>
            category == FoodCategory && canCook && cutProductId < 1;

        // The places of the fill in order: the cooking station, the open tab, then the other tabs that are not locked,
        // left to right as the tab row shows them.
        public static List<long> Owners(long station, long openTab, IEnumerable<Tab> tabs)
        {
            var owners = new List<long> { station, openTab };
            foreach (var tab in tabs)
                if (!tab.Locked && tab.Owner != station && tab.Owner != openTab && !owners.Contains(tab.Owner)) owners.Add(tab.Owner);
            return owners;
        }

        // Reducer_Web_Cooking.CountMaterials: the count of each id of a recipe list, in the order of its first place.
        public static List<KeyValuePair<int, int>> Needed(IEnumerable<int> ids)
        {
            var order = new List<int>();
            var counts = new Dictionary<int, int>();
            foreach (var id in ids)
            {
                if (!counts.ContainsKey(id)) { order.Add(id); counts[id] = 0; }
                counts[id]++;
            }
            var result = new List<KeyValuePair<int, int>>(order.Count);
            foreach (var id in order) result.Add(new KeyValuePair<int, int>(id, counts[id]));
            return result;
        }

        // The rank of an ingredient tier in the tag search: tier 1 (High) is 3, tier 2 is 2, tier 3 (Low) is 1, any
        // other tier 0.
        public static int Rank(int tier) => tier == 1 ? 3 : tier == 2 ? 2 : tier == 3 ? 1 : 0;

        // The items of the places in their order, and the cookable ones grouped once by config id and by tag, so that a
        // search reads only the items of its config ids or tags: the work of the searches of a refresh grows with the
        // matching items, not with all items for each recipe.
        public sealed class Pool
        {
            private static readonly List<Item> None = new List<Item>();
            private readonly Dictionary<int, List<Item>> byConfigId = new Dictionary<int, List<Item>>();
            private readonly Dictionary<int, List<Item>> byTag = new Dictionary<int, List<Item>>();

            public IReadOnlyList<Item> Items { get; }

            public Pool(IEnumerable<Item> items)
            {
                var all = new List<Item>(items);
                Items = all;
                foreach (var item in all)
                {
                    if (!item.Cookable) continue;
                    Add(byConfigId, item.ConfigId, item);
                    Add(byTag, item.SubCategory, item);
                }
            }

            // The cookable items of a config id, or of a tag, in the order of the places.
            public IReadOnlyList<Item> ByConfigId(int configId) => byConfigId.TryGetValue(configId, out var list) ? list : None;
            public IReadOnlyList<Item> ByTag(int tag) => byTag.TryGetValue(tag, out var list) ? list : None;

            private static void Add(Dictionary<int, List<Item>> groups, int key, Item item)
            {
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<Item>();
                list.Add(item);
            }
        }

        // The search of an exact recipe: the items of each needed config id in the order of the places and of each
        // list, the first N of each id; null when one id is short. needed is in the order of CountMaterials.
        public static List<long> SelectByConfigId(Pool pool, IReadOnlyList<KeyValuePair<int, int>> needed)
        {
            var result = new List<long>();
            foreach (var kv in needed)
            {
                var list = pool.ByConfigId(kv.Key);
                if (list.Count < kv.Value) return null;
                for (int i = 0; i < kv.Value; i++) result.Add(list[i].Id);
            }
            return result;
        }

        // The search of a tag recipe: for each tag in order, two passes over the items in the order of the places,
        // each until the tag has its count. An item is taken once over all tags, and never with an excluded config id.
        // With a required tier, an item of a higher rank than the required one is never taken, rank 0 is always
        // allowed, and the first pass takes only the required rank. Null when a tag is short after both passes.
        public static List<long> SelectByTagTiered(Pool pool, IReadOnlyList<KeyValuePair<int, int>> needed, int requiredTier, ICollection<int> excludeConfigIds)
        {
            int required = Rank(requiredTier);
            var taken = new HashSet<long>();
            var result = new List<long>();
            foreach (var kv in needed)
            {
                int found = 0;
                for (int pass = 0; pass < 2 && found < kv.Value; pass++)
                {
                    // The cookable items of the tag only: the game skips each other item.
                    foreach (var item in pool.ByTag(kv.Key))
                    {
                        if (found >= kv.Value) break;
                        if (taken.Contains(item.Id)) continue;
                        if (excludeConfigIds != null && excludeConfigIds.Contains(item.ConfigId)) continue;
                        if (required != 0)
                        {
                            int rank = Rank(item.Tier);
                            if (rank != 0 && rank > required) continue;
                            if (pass == 0 && rank != required) continue;
                        }
                        result.Add(item.Id);
                        taken.Add(item.Id);
                        found++;
                    }
                }
                if (found < kv.Value) return null;
            }
            return result;
        }

        // The counts of the Recipe Book over the other tabs: the game's RefreshRecipeList counts the station and the
        // open tab, one for each cookable item by its config id and one by its SubCategory (not its pieces). This adds
        // the items of the other places in the same way.
        public static void AddCounts(IEnumerable<Item> items, long station, long openTab, IDictionary<int, int> ids, IDictionary<int, int> tags)
        {
            foreach (var item in items)
            {
                if (!item.Cookable || item.Owner == station || item.Owner == openTab) continue;
                ids[item.ConfigId] = (ids.TryGetValue(item.ConfigId, out int n) ? n : 0) + 1;
                tags[item.SubCategory] = (tags.TryGetValue(item.SubCategory, out int t) ? t : 0) + 1;
            }
        }
    }

    // The once-for-each-refresh rule of the counts: a refresh of the Recipe Book sets the mark, and only the first
    // evaluation after it takes it. A compare of the dictionaries cannot do this under Il2CppInterop (a new wrapper for
    // each call, and a freed address can come back in a later refresh).
    public sealed class RefreshMark
    {
        private bool marked;

        public void Mark() => marked = true;

        public bool TakeFirst()
        {
            if (!marked) return false;
            marked = false;
            return true;
        }
    }
}
