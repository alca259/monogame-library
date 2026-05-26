using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Navigation;

namespace Alca.MonoGame.Kernel.UnitTests.Navigation;

public sealed class AsyncPathfinderTests : IDisposable
{
    private static NavGrid OpenGrid(int w = 20, int h = 20, float cell = 32f)
    {
        var grid = new NavGrid(w, h, cell, Vector2.Zero);
        grid.SetAll(true);
        return grid;
    }

    private readonly AsyncPathfinder _sut = new();

    public void Dispose() => _sut.Dispose();

    [Fact]
    public async Task FindPathAsync_WhenPathExists_ReturnsNonNullNavPath()
    {
        NavGrid grid = OpenGrid();
        NavPath? result = await _sut.FindPathAsync(
            grid, new Vector2(16f, 16f), new Vector2(16f + 32f * 5, 16f), NavAgentProfile.Default);

        Assert.NotNull(result);
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public async Task FindPathAsync_WhenNoPath_ReturnsNull()
    {
        var grid = new NavGrid(5, 5, 32f, Vector2.Zero);
        grid.SetAll(true);
        // Block all cells in column 2 — creates an impassable wall from top to bottom
        for (int y = 0; y < 5; y++)
            grid.SetWalkable(2, y, false);

        NavPath? result = await _sut.FindPathAsync(
            grid, new Vector2(16f, 16f), new Vector2(16f + 32f * 4, 16f), NavAgentProfile.Default);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindPathAsync_MultipleRequests_AllComplete()
    {
        NavGrid grid = OpenGrid();

        Task<NavPath?>[] tasks =
        [
            _sut.FindPathAsync(grid, new Vector2(16f, 16f), new Vector2(16f + 32f * 3, 16f), NavAgentProfile.Default),
            _sut.FindPathAsync(grid, new Vector2(16f, 16f), new Vector2(16f + 32f * 5, 16f), NavAgentProfile.Default),
            _sut.FindPathAsync(grid, new Vector2(16f, 16f), new Vector2(16f + 32f * 7, 16f), NavAgentProfile.Default),
        ];

        NavPath?[] results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public void FindPathAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        _sut.Dispose();

        void Call() => _sut.FindPathAsync(OpenGrid(), Vector2.Zero, Vector2.One, NavAgentProfile.Default);
        Assert.Throws<ObjectDisposedException>(Call);
    }

    [Fact]
    public async Task NavAgent_SetDestinationAsync_UsesAsyncPathfinderWhenAvailable()
    {
        NavGrid grid = OpenGrid();
        grid.SetAll(true);

        var world = new GameWorld
        {
            NavGrid = grid,
            Pathfinder = new Pathfinder(20 * 20),
            AsyncPathfinder = new AsyncPathfinder()
        };

        var entity = world.CreateEntity("agent", new Vector2(16f, 16f));
        var agent = entity.AddComponent<NavAgent>();

        bool result = await agent.SetDestinationAsync(new Vector2(16f + 32f * 5, 16f));

        Assert.True(result);
        Assert.True(agent.HasPath);

        world.AsyncPathfinder.Dispose();
    }
}
