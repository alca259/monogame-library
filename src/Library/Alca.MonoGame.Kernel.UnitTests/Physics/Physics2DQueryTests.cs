using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Physics;

public sealed class Physics2DQueryTests
{
    private static (Physics2DWorld physicsWorld, GameWorld world) CreateWorld()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var world = new GameWorld { PhysicsWorld = physicsWorld };
        return (physicsWorld, world);
    }

    private static void AddStaticBox(GameWorld world, Vector2 position, Vector2 size,
        CollisionCategory layer = CollisionCategory.Default)
    {
        var entity = world.CreateEntity("box", position);
        entity.Add(new BoxCollider2D { Size = size, Layer = layer, Mask = CollisionCategory.All });
    }

    private static void StepPhysics(Physics2DWorld physicsWorld)
    {
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
        physicsWorld.Step(gameTime);
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    [Fact]
    public void Raycast_WhenNoObstacle_ReturnsFalse()
    {
        var (physicsWorld, _) = CreateWorld();

        bool hit = physicsWorld.Query.Raycast(Vector2.Zero, Vector2.UnitX, 10f, CollisionCategory.Default, out _);

        Assert.False(hit);
    }

    [Fact]
    public void Raycast_WhenBoxColliderPresent_ReturnsHit()
    {
        var (physicsWorld, world) = CreateWorld();
        AddStaticBox(world, new Vector2(5f, 0f), Vector2.One);
        StepPhysics(physicsWorld);

        bool hit = physicsWorld.Query.Raycast(Vector2.Zero, Vector2.UnitX, 10f, CollisionCategory.Default, out var result);

        Assert.True(hit);
        Assert.NotNull(result.Collider);
        Assert.True(result.IsHit);
    }

    [Fact]
    public void Raycast_HitDistance_MatchesExpectedValue()
    {
        var (physicsWorld, world) = CreateWorld();
        // Box at x=5 with width=1 → left edge at x=4.5
        AddStaticBox(world, new Vector2(5f, 0f), Vector2.One);
        StepPhysics(physicsWorld);

        physicsWorld.Query.Raycast(Vector2.Zero, Vector2.UnitX, 10f, CollisionCategory.Default, out var result);

        // Left edge of 1×1 box centered at (5,0) is at x=4.5
        Assert.True(result.IsHit);
        Assert.True(MathF.Abs(result.Distance - 4.5f) < 0.01f, $"Expected ~4.5 but got {result.Distance}");
    }

    [Fact]
    public void Raycast_WithMaskFilter_IgnoresNonMatchingLayer()
    {
        var (physicsWorld, world) = CreateWorld();
        // Box on Enemy layer, query with Player mask → should not match
        AddStaticBox(world, new Vector2(5f, 0f), Vector2.One, CollisionCategory.Enemy);
        StepPhysics(physicsWorld);

        bool hit = physicsWorld.Query.Raycast(Vector2.Zero, Vector2.UnitX, 10f, CollisionCategory.Player, out _);

        Assert.False(hit);
    }

    [Fact]
    public void RaycastAll_ReturnsMultipleHits()
    {
        var (physicsWorld, world) = CreateWorld();
        AddStaticBox(world, new Vector2(3f, 0f), Vector2.One);
        AddStaticBox(world, new Vector2(7f, 0f), Vector2.One);
        StepPhysics(physicsWorld);

        var results = new List<RaycastHit2D>();
        physicsWorld.Query.RaycastAll(Vector2.Zero, Vector2.UnitX, 20f, CollisionCategory.Default, results);

        Assert.Equal(2, results.Count);
    }

    // ── Overlap queries ───────────────────────────────────────────────────────

    [Fact]
    public void OverlapCircle_WhenColliderInsideRadius_ReturnsIt()
    {
        var (physicsWorld, world) = CreateWorld();
        AddStaticBox(world, Vector2.Zero, Vector2.One);
        StepPhysics(physicsWorld);

        var results = new List<Collider2D>();
        physicsWorld.Query.OverlapCircle(Vector2.Zero, 5f, CollisionCategory.Default, results);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void OverlapCircle_WhenColliderOutsideRadius_ReturnsEmpty()
    {
        var (physicsWorld, world) = CreateWorld();
        AddStaticBox(world, new Vector2(100f, 0f), Vector2.One);
        StepPhysics(physicsWorld);

        var results = new List<Collider2D>();
        physicsWorld.Query.OverlapCircle(Vector2.Zero, 5f, CollisionCategory.Default, results);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapBox_WhenColliderOutside_ReturnsEmpty()
    {
        var (physicsWorld, world) = CreateWorld();
        AddStaticBox(world, new Vector2(100f, 0f), Vector2.One);
        StepPhysics(physicsWorld);

        var results = new List<Collider2D>();
        physicsWorld.Query.OverlapBox(Vector2.Zero, new Vector2(1f, 1f), 0f, CollisionCategory.Default, results);

        Assert.Empty(results);
    }
}
