using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics.Triggers;

namespace Alca.MonoGame.Kernel.UnitTests.Physics.Triggers;

public sealed class TriggerWorldTests
{
    private static GameTime ZeroGameTime() =>
        new(TimeSpan.Zero, TimeSpan.Zero);

    /// <summary>
    /// Creates a world with entities flushed (post-Update), returns the TriggerWorld separately
    /// so tests can register zones manually without relying on Awake auto-registration.
    /// </summary>
    private static (GameWorld world, TriggerWorld triggerWorld) CreateFlushedWorld()
    {
        var world = new GameWorld();
        var triggerWorld = new TriggerWorld();
        // Do NOT assign world.TriggerWorld yet — zones will be manually registered in tests.
        return (world, triggerWorld);
    }

    private static GameEntity FlushEntity(GameWorld world, string name, Vector2 position)
    {
        var entity = world.CreateEntity(name, position);
        world.Update(ZeroGameTime()); // flush deferred add
        return world.FindByName(name)!;
    }

    // ── Enter event ───────────────────────────────────────────────────────────

    [Fact]
    public void Update_TwoOverlappingAABBZones_RaisesEnterEvent()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        // Entities at (0,0) and (32,0) — 64x64 AABB zones overlap in X from 0 to 32.
        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(32f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        bool enterFiredA = false;
        bool enterFiredB = false;
        zoneA.OnEnter = _ => enterFiredA = true;
        zoneB.OnEnter = _ => enterFiredB = true;

        triggerWorld.Update(ZeroGameTime());

        Assert.True(enterFiredA);
        Assert.True(enterFiredB);
    }

    [Fact]
    public void Update_TwoNonOverlappingZones_DoesNotRaiseEnterEvent()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        // Entity B is far enough that 64x64 boxes do not intersect.
        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(200f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        bool enterFired = false;
        zoneA.OnEnter = _ => enterFired = true;
        zoneB.OnEnter = _ => enterFired = true;

        triggerWorld.Update(ZeroGameTime());

        Assert.False(enterFired);
    }

    // ── Stay event ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_OverlappingZonesConsecutiveFrames_RaisesStayEvent()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(32f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        // Frame 1: Enter
        triggerWorld.Update(ZeroGameTime());

        bool stayFired = false;
        zoneA.OnStay = _ => stayFired = true;

        // Frame 2: Stay
        triggerWorld.Update(ZeroGameTime());

        Assert.True(stayFired);
    }

    // ── Exit event ────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ZonesMoveApart_RaisesExitEvent()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(32f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        // Frame 1: zones overlap → Enter
        triggerWorld.Update(ZeroGameTime());

        bool exitFired = false;
        zoneA.OnExit = _ => exitFired = true;

        // Move B far away so they no longer intersect.
        entityB.Transform.Position2d = new Vector2(200f, 0f);

        // Frame 2: no overlap → Exit
        triggerWorld.Update(ZeroGameTime());

        Assert.True(exitFired);
    }

    // ── Layer mask filtering ──────────────────────────────────────────────────

    [Fact]
    public void Update_DifferentLayerMasks_NoOverlapDetected()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(10f, 0f));

        // Layer bits 0b01 and 0b10 share no bits → overlap test skipped.
        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64, LayerMask = 0b01 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64, LayerMask = 0b10 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        bool enterFired = false;
        zoneA.OnEnter = _ => enterFired = true;

        triggerWorld.Update(ZeroGameTime());

        Assert.False(enterFired);
    }

    // ── Circle shapes ─────────────────────────────────────────────────────────

    [Fact]
    public void Update_TwoOverlappingCircleZones_RaisesEnterEvent()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        // Two circles radius=32 at (0,0) and (10,0): distance=10 < combined radius=64 → overlap.
        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(10f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.Circle, Radius = 32f };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.Circle, Radius = 32f };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        bool enterFired = false;
        zoneA.OnEnter = _ => enterFired = true;

        triggerWorld.Update(ZeroGameTime());

        Assert.True(enterFired);
    }

    // ── Mixed AABB + Circle ───────────────────────────────────────────────────

    [Fact]
    public void Update_MixedAABBAndCircle_OverlapDetected()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        // AABB at origin 64x64, circle at (20,0) radius=32.
        // Nearest point on rect to circle center: clamp(20, -32, 32)=20, clamp(0,-32,32)=0 → (20,0).
        // Distance = 0 ≤ 32 → overlap.
        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(20f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.Circle, Radius = 32f };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        bool enterFired = false;
        zoneA.OnEnter = _ => enterFired = true;

        triggerWorld.Update(ZeroGameTime());

        Assert.True(enterFired);
    }

    // ── Unregister ────────────────────────────────────────────────────────────

    [Fact]
    public void Unregister_ZoneNoLongerDetected()
    {
        var (world, triggerWorld) = CreateFlushedWorld();

        var entityA = FlushEntity(world, "A", new Vector2(0f, 0f));
        var entityB = FlushEntity(world, "B", new Vector2(32f, 0f));

        var zoneA = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityA.Add(zoneA);
        triggerWorld.Register(zoneA);

        var zoneB = new TriggerZone2D { Shape = TriggerShapeType.AABB, Width = 64, Height = 64 };
        entityB.Add(zoneB);
        triggerWorld.Register(zoneB);

        // Frame 1: Enter fires
        triggerWorld.Update(ZeroGameTime());

        // Unregister zoneB — clears overlap tracking.
        triggerWorld.Unregister(zoneB);

        int enterCount = 0;
        int stayCount = 0;
        zoneA.OnEnter = _ => enterCount++;
        zoneA.OnStay  = _ => stayCount++;

        // Frame 2: only zoneA remains, no pair → no events.
        triggerWorld.Update(ZeroGameTime());

        Assert.Equal(0, enterCount);
        Assert.Equal(0, stayCount);
    }
}
