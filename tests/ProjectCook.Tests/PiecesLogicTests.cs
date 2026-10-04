using ProjectCook;
using Xunit;
using Item = ProjectCook.PiecesLogic.Item;

public class PiecesMergeTargetTests
{
    private const int Rabbit = 30014, Cabbage = 2001;

    private static Item At(long id, int x, int y, int configId = Rabbit, int w = 1, int h = 1)
        => new Item(id, configId, x, y, w, h);

    [Fact]
    public void EmptyCell_GivesNoTarget()
    {
        var station = new[] { At(1, 0, 0), At(2, 2, 1, Cabbage) };
        Assert.Null(PiecesLogic.MergeTarget(1, 0, 1, 1, Rabbit, 99, station));
    }

    [Fact]
    public void DropOnItemOfSameKind_GivesThatItem()
    {
        var station = new[] { At(1, 0, 0), At(2, 0, 1) };
        Assert.Equal(2L, PiecesLogic.MergeTarget(0, 1, 1, 1, Rabbit, 99, station));
    }

    [Fact]
    public void ItemOfTwoByTwoCells_CoversFourCells()
    {
        var station = new[] { At(1, 2, 0, Rabbit, 2, 2) };
        Assert.Equal(1L, PiecesLogic.MergeTarget(3, 1, 1, 1, Rabbit, 99, station));
        Assert.Null(PiecesLogic.MergeTarget(4, 1, 1, 1, Rabbit, 99, station));
        Assert.Null(PiecesLogic.MergeTarget(2, 2, 1, 1, Rabbit, 99, station));
    }

    [Fact]
    public void DropRectangleOfTheDraggedItem_IsChecked()
    {
        // A 2 x 1 piece dropped at (0, 0) covers (1, 0), where the station item is.
        var station = new[] { At(1, 1, 0) };
        Assert.Equal(1L, PiecesLogic.MergeTarget(0, 0, 2, 1, Rabbit, 99, station));
    }

    [Fact]
    public void OverlapWithOtherKind_GivesNoTarget()
    {
        var station = new[] { At(1, 0, 0, Cabbage) };
        Assert.Null(PiecesLogic.MergeTarget(0, 0, 1, 1, Rabbit, 99, station));
    }

    [Fact]
    public void OverlapWithTwoItems_GivesNoTarget()
    {
        var station = new[] { At(1, 0, 0), At(2, 1, 0) };
        Assert.Null(PiecesLogic.MergeTarget(0, 0, 2, 1, Rabbit, 99, station));
    }

    [Fact]
    public void DraggedItemOnTheStation_IsLeftOut()
    {
        // A move inside the station: the dragged item still has its old cells.
        var station = new[] { At(1, 0, 0), At(2, 3, 0) };
        Assert.Null(PiecesLogic.MergeTarget(0, 0, 1, 1, Rabbit, 1, station));
        Assert.Equal(2L, PiecesLogic.MergeTarget(3, 0, 1, 1, Rabbit, 1, station));
    }
}
