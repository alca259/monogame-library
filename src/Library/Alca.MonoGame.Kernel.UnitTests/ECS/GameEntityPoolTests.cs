using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameEntityPoolTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // ── Get ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Get_ReturnsActiveEntity()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");

        var entity = pool.Get();

        Assert.True(entity.Active);
    }

    [Fact]
    public void Get_CallsReset_OnRetrievedBehaviour()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");

        var entity = pool.Get();
        var b = entity.GetComponent<PoolableBehaviour>()!;

        Assert.True(b.ResetCalled);
    }

    [Fact]
    public void Get_InvokesConfigureCallback()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");
        bool configured = false;

        pool.Get(e => configured = true);

        Assert.True(configured);
    }

    // ── Return ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Return_DeactivatesEntity()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");
        var entity = pool.Get();

        pool.Return(entity);

        Assert.False(entity.Active);
    }

    [Fact]
    public void Return_IncreasesAvailableCount()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");
        var entity = pool.Get();

        pool.Return(entity);

        Assert.Equal(1, pool.AvailableCount);
    }

    [Fact]
    public void Get_AfterReturn_ReusesSameEntity()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet");
        var first = pool.Get();
        pool.Return(first);

        var second = pool.Get();

        Assert.Same(first, second);
    }

    // ── Prewarm ────────────────────────────────────────────────────────────────

    [Fact]
    public void Prewarm_PopulatesPool_WithInactiveEntities()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet", prewarm: 5);

        Assert.Equal(5, pool.AvailableCount);
    }

    [Fact]
    public void PrewarmedEntities_AreInactive_UntilRetrieved()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet", prewarm: 3);

        // Flush so prewarm entities are in the active list
        world.Update(AnyGameTime());

        // All three should be inactive (in the pool, not visible in game)
        var bullets = world.FindEntities<PoolableBehaviour>().ToList();
        Assert.All(bullets, e => Assert.False(e.Active));
    }

    [Fact]
    public void AvailableCount_DecreasesOnGet_IncreasesOnReturn()
    {
        var world = new GameWorld();
        var pool = new GameEntityPool<PoolableBehaviour>(world, "Bullet", prewarm: 2);

        Assert.Equal(2, pool.AvailableCount);

        var e = pool.Get();
        Assert.Equal(1, pool.AvailableCount);

        pool.Return(e);
        Assert.Equal(2, pool.AvailableCount);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class PoolableBehaviour : GameBehaviour, IPoolable
    {
        public bool ResetCalled { get; private set; }

        public void Reset()
        {
            ResetCalled = true;
            Enabled = true;
        }
    }
}
