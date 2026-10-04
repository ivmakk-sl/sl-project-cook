using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free logic of the separate pieces. This file must not use any game or BepInEx type, because the unit tests
    // compile it alone.
    public static class PiecesLogic
    {
        public readonly struct Item
        {
            public readonly long Id;
            public readonly int ConfigId, X, Y, W, H;
            public Item(long id, int configId, int x, int y, int w, int h)
            {
                Id = id; ConfigId = configId; X = x; Y = y; W = w; H = h;
            }
        }

        // The item that a piece dropped at (dropX, dropY) with w x h cells merges into: the one item of the cooking
        // station that the drop rectangle overlaps, when it has the same config id. None when the drop rectangle is
        // free (the piece stays its own item), or when it overlaps another kind or two items (the page refuses that
        // drop). The dragged item itself is left out, for a move inside the cooking station.
        public static long? MergeTarget(int dropX, int dropY, int w, int h, int configId, long draggedId,
            IEnumerable<Item> stationItems)
        {
            long? target = null;
            int overlaps = 0;
            foreach (var item in stationItems)
            {
                if (item.Id == draggedId) continue;
                bool overlap = item.X < dropX + w && dropX < item.X + item.W && item.Y < dropY + h && dropY < item.Y + item.H;
                if (!overlap) continue;
                overlaps++;
                target = item.ConfigId == configId ? item.Id : (long?)null;
            }
            return overlaps == 1 ? target : null;
        }
    }
}
