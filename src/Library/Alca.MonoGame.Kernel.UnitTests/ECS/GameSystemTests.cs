using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameSystemTests
{
    // ── World access ───────────────────────────────────────────────────────────

    [Fact]
    public void World_BeforeRegistration_Throws()
    {
        var system = new StubSystem();

        Assert.Throws<InvalidOperationException>(() => _ = system.World);
    }

    [Fact]
    public void SetWorldInternal_CalledTwice_Throws()
    {
        var system = new StubSystem();
        var world1 = new GameWorld();
        var world2 = new GameWorld();
        world1.AddSystem(system);

        Assert.Throws<InvalidOperationException>(() => world2.AddSystem(system));
    }

    // ── Defaults ───────────────────────────────────────────────────────────────

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var system = new StubSystem();

        Assert.True(system.Enabled);
    }

    [Fact]
    public void Order_DefaultsToZero()
    {
        var system = new StubSystem();

        Assert.Equal(0, system.Order);
    }

    // ── Default hooks ─────────────────────────────────────────────────────────

    [Fact]
    public void LifecycleHooks_DefaultImplementation_DoNotThrow()
    {
        var system = new StubSystem();
        var world = new GameWorld();
        world.AddSystem(system);
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        var ex = Record.Exception(() =>
        {
            world.Update(gt);
            world.RemoveSystem(system);
        });

        Assert.Null(ex);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class StubSystem : GameSystem { }
}
