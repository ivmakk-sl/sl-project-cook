using System.Collections.Generic;
using SlShared.Json;

namespace ProjectCook
{
    // Auto off: after a click on Load, one cooking station uses the game's own split until an item of that moment leaves
    // it. It holds one cooking station, in memory only, and the config entry RowSplit does not change. This file must not
    // use any game or BepInEx type, because the unit tests compile it alone.
    public sealed class RowAutoOff
    {
        private bool active;
        private long ownerId;
        private readonly HashSet<long> itemIds = new HashSet<long>();

        // The clicked recipe needs all items of the cooking station: the game's match of all items holds it, and the
        // dishes of the rows do not. A recipe of another tier of the same dish counts as the clicked one.
        public static bool Starts(RowSplitLogic.RecipeKey clicked, IReadOnlyList<RowSplitLogic.RecipeKey> whole, IReadOnlyList<RowSplitLogic.RecipeKey> plan)
            => Holds(whole, clicked) && !Holds(plan, clicked);

        // The dishes of the rows give the clicked recipe, so row split stays on after this click.
        public static bool Ends(RowSplitLogic.RecipeKey clicked, IReadOnlyList<RowSplitLogic.RecipeKey> plan) => Holds(plan, clicked);

        private static bool Holds(IReadOnlyList<RowSplitLogic.RecipeKey> keys, RowSplitLogic.RecipeKey clicked)
        {
            foreach (var key in keys)
                if (RowSplitLogic.IsSameDish(clicked, key)) return true;
            return false;
        }

        // The recipe id of the JSON of a click on Load (Ac_Cooking_RecipeClick.JsonData), or 0.
        public static int ClickedRecipe(string json)
        {
            var objects = FlatJson.ReadArray("[" + json + "]");
            if (objects == null || objects.Count != 1) return 0;
            return objects[0].Fields.TryGetValue("recipeId", out object id) && id is double number ? (int)number : 0;
        }

        public void Begin(long owner, IEnumerable<long> items)
        {
            active = true;
            ownerId = owner;
            itemIds.Clear();
            itemIds.UnionWith(items);
        }

        public bool IsActive(long owner) => active && ownerId == owner;

        // True when this call ended auto off: an item of its start is not on the cooking station now. A refresh of
        // another owner leaves it alone.
        public bool Keep(long owner, IEnumerable<long> items)
        {
            if (!IsActive(owner)) return false;
            var now = new HashSet<long>(items);
            if (now.IsSupersetOf(itemIds)) return false;
            Clear();
            return true;
        }

        public void Clear()
        {
            active = false;
            itemIds.Clear();
        }

        // A click on the toggle at that owner: true when it ended auto off.
        public bool Click(long owner)
        {
            if (!IsActive(owner)) return false;
            Clear();
            return true;
        }
    }
}
