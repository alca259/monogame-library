using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Navigation;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Navigation;

public sealed class NavGridPhysicsSyncTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    private static NavGrid MakeGrid(int size = 20, float cellSize = 32f) =>
        new(size, size, cellSize, Vector2.Zero);

    [Fact]
    public void Register_NewCollider_RegistrationCountIsOne()
    {
        var (collider, _) = CreateColliderWithWorld();
        NavGridPhysicsSync sync = new();

        sync.Register(collider);

        Assert.Equal(1, sync.RegistrationCount);
    }

    [Fact]
    public void Unregister_RegisteredCollider_RegistrationCountBecomesZero()
    {
        var (collider, _) = CreateColliderWithWorld();
        NavGridPhysicsSync sync = new();
        sync.Register(collider);

        sync.Unregister(collider);

        Assert.Equal(0, sync.RegistrationCount);
    }

    [Fact]
    public void SyncAll_BoxColliderAtCenter_MarksOccupiedCellsNonWalkable()
    {
        NavGrid grid = MakeGrid();
        var (collider, world) = CreateColliderWithWorld(position: new Vector2(160f, 160f), boxSize: new Vector2(64f, 64f));
        world.Update(AnyGameTime()); // flush entity + call Awake on collider

        NavGridPhysicsSync sync = new();
        sync.Register(collider, walkable: false);
        sync.SyncAll(grid);

        // Entity at (160,160), box 64x64 → AABB ≈ (128,128) to (192,192)
        // grid cellSize=32 → cells (4,4) to (6,6)
        Assert.False(grid.IsWalkable(5, 5));
    }

    [Fact]
    public void SyncOne_RegisteredCollider_MarksExpectedCellAndOnlyThatCollider()
    {
        NavGrid grid = MakeGrid();
        var (collider1, world1) = CreateColliderWithWorld(position: new Vector2(160f, 160f), boxSize: new Vector2(64f, 64f));
        world1.Update(AnyGameTime()); // flush

        NavGridPhysicsSync sync = new();
        sync.Register(collider1, walkable: false);
        sync.SyncOne(collider1, grid);

        Assert.False(grid.IsWalkable(5, 5));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static (BoxCollider2D collider, GameWorld world) CreateColliderWithWorld(
        Vector2 position = default, Vector2 boxSize = default)
    {
        var physWorld = new Physics2DWorld();
        var gameWorld = new GameWorld { PhysicsWorld = physWorld };
        var entity = gameWorld.CreateEntity("E", position == default ? new Vector2(160f, 160f) : position);
        var collider = new BoxCollider2D { Size = boxSize == default ? new Vector2(64f, 64f) : boxSize };
        entity.Add(collider);
        return (collider, gameWorld);
    }
}
