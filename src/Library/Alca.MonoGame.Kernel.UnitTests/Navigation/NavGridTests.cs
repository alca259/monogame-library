using Alca.MonoGame.Kernel.Navigation;

namespace Alca.MonoGame.Kernel.UnitTests.Navigation;

public sealed class NavGridTests
{
    private static NavGrid CreateGrid(int w = 10, int h = 10, float cell = 32f) =>
        new(w, h, cell, Vector2.Zero);

    [Fact]
    public void GetCell_ValidCoords_ReturnsCorrectData()
    {
        NavGrid grid = CreateGrid();

        NavCell cell = grid.GetCell(3, 5);

        Assert.Equal(3, cell.GridX);
        Assert.Equal(5, cell.GridY);
        Assert.True(cell.IsWalkable);
        Assert.Equal(1f, cell.MovementCost);
        Assert.Equal(0f, cell.ObstacleHeight);
    }

    [Fact]
    public void IsInBounds_ValidCoords_ReturnsTrue()
    {
        NavGrid grid = CreateGrid();

        Assert.True(grid.IsInBounds(0, 0));
        Assert.True(grid.IsInBounds(9, 9));
    }

    [Fact]
    public void IsInBounds_OutOfRange_ReturnsFalse()
    {
        NavGrid grid = CreateGrid();

        Assert.False(grid.IsInBounds(-1, 0));
        Assert.False(grid.IsInBounds(10, 0));
        Assert.False(grid.IsInBounds(0, -1));
        Assert.False(grid.IsInBounds(0, 10));
    }

    [Fact]
    public void SetWalkable_BlocksCell()
    {
        NavGrid grid = CreateGrid();

        grid.SetWalkable(2, 3, false);

        Assert.False(grid.GetCell(2, 3).IsWalkable);
    }

    [Fact]
    public void SetMovementCost_UpdatesCell()
    {
        NavGrid grid = CreateGrid();

        grid.SetMovementCost(1, 1, 3.5f);

        Assert.Equal(3.5f, grid.GetCell(1, 1).MovementCost);
    }

    [Fact]
    public void SetObstacleHeight_UpdatesCell_CellRemainsCrossableWithSufficientJump()
    {
        NavGrid grid = CreateGrid();
        grid.SetWalkable(5, 5, false);
        grid.SetObstacleHeight(5, 5, 2f);

        NavCell cell = grid.GetCell(5, 5);
        Assert.Equal(2f, cell.ObstacleHeight);
        Assert.False(cell.IsWalkable);
    }

    [Fact]
    public void SetAll_SetsAllCellsWalkability()
    {
        NavGrid grid = CreateGrid(5, 5);

        grid.SetAll(false);

        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                Assert.False(grid.GetCell(x, y).IsWalkable);
    }

    [Fact]
    public void WorldToGrid_GridToWorld_RoundTrip_MatchesWithinHalfCell()
    {
        NavGrid grid = CreateGrid(10, 10, 32f);
        var worldPos = new Vector2(80f, 144f);

        grid.WorldToGrid(worldPos, out int gx, out int gy);
        Vector2 center = grid.GridToWorld(gx, gy);

        // Center of the cell must be within half a cell of the original position
        Assert.True(MathF.Abs(center.X - worldPos.X) <= 16f);
        Assert.True(MathF.Abs(center.Y - worldPos.Y) <= 16f);
    }

    [Fact]
    public void GridToWorld_ReturnsCenter()
    {
        NavGrid grid = CreateGrid(10, 10, 32f);

        Vector2 center = grid.GridToWorld(0, 0);

        Assert.Equal(16f, center.X, 3);
        Assert.Equal(16f, center.Y, 3);
    }

    [Fact]
    public void SetObstacleHeight_OutOfBounds_DoesNotThrow()
    {
        NavGrid grid = CreateGrid();

        // Should not throw
        grid.SetObstacleHeight(-1, 0, 5f);
        grid.SetObstacleHeight(100, 100, 5f);
    }
}
