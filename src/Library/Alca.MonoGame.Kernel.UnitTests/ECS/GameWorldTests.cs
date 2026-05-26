using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.ECS;

public sealed class GameWorldTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    // ── CreateEntity ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateEntity_ReturnsEntity_WithCorrectName()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("Player");

        Assert.Equal("Player", entity.Name);
    }

    [Fact]
    public void CreateEntity_IsDeferred_NotVisibleBeforeUpdate()
    {
        var world = new GameWorld();
        world.CreateEntity("E");

        var found = world.FindByName("E");

        Assert.Null(found);
    }

    [Fact]
    public void CreateEntity_IsVisible_AfterUpdate()
    {
        var world = new GameWorld();
        world.CreateEntity("E");

        world.Update(AnyGameTime());

        Assert.NotNull(world.FindByName("E"));
    }

    // ── Destroy ────────────────────────────────────────────────────────────────

    [Fact]
    public void Destroy_IsDeferred_EntityStillVisibleSameFrame()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        world.Update(AnyGameTime());

        world.Destroy(entity);

        Assert.NotNull(world.FindByName("E"));
    }

    [Fact]
    public void Destroy_RemovesEntity_AfterNextUpdate()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        world.Update(AnyGameTime());

        world.Destroy(entity);
        world.Update(AnyGameTime());

        Assert.Null(world.FindByName("E"));
    }

    [Fact]
    public void Destroy_CalledTwice_DoesNotThrow()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        world.Update(AnyGameTime());

        world.Destroy(entity);
        world.Destroy(entity);
        var ex = Record.Exception(() => world.Update(AnyGameTime()));

        Assert.Null(ex);
    }

    // ── FindByName ─────────────────────────────────────────────────────────────

    [Fact]
    public void FindByName_ReturnsNull_WhenNoMatch()
    {
        var world = new GameWorld();
        world.CreateEntity("A");
        world.Update(AnyGameTime());

        Assert.Null(world.FindByName("Z"));
    }

    [Fact]
    public void FindByName_ReturnsFirstMatch_WhenMultipleEntitiesExist()
    {
        var world = new GameWorld();
        world.CreateEntity("A");
        world.CreateEntity("B");
        world.Update(AnyGameTime());

        var found = world.FindByName("B");

        Assert.NotNull(found);
        Assert.Equal("B", found!.Name);
    }

    // ── FindEntities ───────────────────────────────────────────────────────────

    [Fact]
    public void FindEntities_ReturnOnlyMatchingEntities_ByConcreteType()
    {
        var world = new GameWorld();
        var e1 = world.CreateEntity("A");
        var e2 = world.CreateEntity("B");
        var e3 = world.CreateEntity("C");
        e1.Add(new DamageBehaviour());
        e3.Add(new DamageBehaviour());
        world.Update(AnyGameTime());

        var results = world.FindEntities<DamageBehaviour>().ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(e1, results);
        Assert.Contains(e3, results);
        Assert.DoesNotContain(e2, results);
    }

    [Fact]
    public void FindEntities_WorksByInterface()
    {
        var world = new GameWorld();
        var e1 = world.CreateEntity("A");
        e1.Add(new DamageBehaviour());
        world.CreateEntity("B");
        world.Update(AnyGameTime());

        var results = world.FindEntities<IDamageable>().ToList();

        Assert.Single(results);
        Assert.Same(e1, results[0]);
    }

    // ── FindComponents ─────────────────────────────────────────────────────────

    [Fact]
    public void FindComponents_ReturnsAllMatchingBehaviours()
    {
        var world = new GameWorld();
        var e1 = world.CreateEntity("A");
        var e2 = world.CreateEntity("B");
        var b1 = new DamageBehaviour();
        var b2 = new DamageBehaviour();
        e1.Add(b1);
        e2.Add(b2);
        world.CreateEntity("C");
        world.Update(AnyGameTime());

        var results = world.FindComponents<DamageBehaviour>().ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(b1, results);
        Assert.Contains(b2, results);
    }

    [Fact]
    public void FindComponents_WorksByInterface()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("A");
        var b = new DamageBehaviour();
        entity.Add(b);
        world.Update(AnyGameTime());

        var results = world.FindComponents<IDamageable>().ToList();

        Assert.Single(results);
        Assert.Same(b, results[0]);
    }

    // ── IsEnabled ──────────────────────────────────────────────────────────────

    [Fact]
    public void Update_IsSkipped_WhenWorldIsDisabled()
    {
        var world = new GameWorld();
        var spy = new CounterBehaviour();
        var entity = world.CreateEntity("E");
        entity.Add(spy);
        world.Update(AnyGameTime()); // flush + first update
        world.IsEnabled = false;

        world.Update(AnyGameTime());
        world.Update(AnyGameTime());

        Assert.Equal(1, spy.UpdateCount); // only the first update counted
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private interface IDamageable { }

    private sealed class DamageBehaviour : GameBehaviour, IDamageable { }

    private sealed class CounterBehaviour : GameBehaviour
    {
        public int UpdateCount { get; private set; }
        public override void Update(GameTime gameTime) => UpdateCount++;
    }
}
