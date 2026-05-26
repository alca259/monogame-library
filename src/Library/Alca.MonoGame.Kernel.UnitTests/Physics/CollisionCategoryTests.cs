using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Physics;

public sealed class CollisionCategoryTests
{
    private static (GameWorld world, GameEntity entity) CreateEntityWithBox(
        Vector2 position = default,
        CollisionCategory layer = CollisionCategory.Default,
        CollisionCategory mask = CollisionCategory.All,
        Vector2? size = null)
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var world = new GameWorld { PhysicsWorld = physicsWorld };
        var entity = world.CreateEntity("e", position);
        entity.Add(new RigidBody2D());
        var collider = new BoxCollider2D
        {
            Size = size ?? Vector2.One,
            Layer = layer,
            Mask = mask,
        };
        entity.Add(collider);
        return (world, entity);
    }

    [Fact]
    public void Collider_DefaultLayer_IsDefault()
    {
        var collider = new BoxCollider2D();
        Assert.Equal(CollisionCategory.Default, collider.Layer);
    }

    [Fact]
    public void Collider_DefaultMask_IsAll()
    {
        var collider = new BoxCollider2D();
        Assert.Equal(CollisionCategory.All, collider.Mask);
    }

    [Fact]
    public void SetLayer_UpdatesAetherFixtureCollisionCategories()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var world = new GameWorld { PhysicsWorld = physicsWorld };
        var entity = world.CreateEntity("e");
        entity.Add(new RigidBody2D());
        var collider = new BoxCollider2D { Layer = CollisionCategory.Player };
        entity.Add(collider);

        int expected = (int)(ushort)CollisionCategory.Player;
        int actual = (int)collider.AetherFixture.CollisionCategories;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SetMask_UpdatesAetherFixtureCollidesWith()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var world = new GameWorld { PhysicsWorld = physicsWorld };
        var entity = world.CreateEntity("e");
        entity.Add(new RigidBody2D());
        var collider = new BoxCollider2D { Mask = CollisionCategory.Terrain };
        entity.Add(collider);

        int expected = (int)(ushort)CollisionCategory.Terrain;
        int actual = (int)collider.AetherFixture.CollidesWith;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CollisionMatrix_SetCanCollide_False_PreventsMaskOverlap()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var world = new GameWorld { PhysicsWorld = physicsWorld };
        var matrix = new CollisionMatrix();

        var entityA = world.CreateEntity("A");
        entityA.Add(new RigidBody2D());
        var colA = new BoxCollider2D { Layer = CollisionCategory.Player, Mask = CollisionCategory.All };
        entityA.Add(colA);

        var entityB = world.CreateEntity("B");
        entityB.Add(new RigidBody2D());
        var colB = new BoxCollider2D { Layer = CollisionCategory.Enemy, Mask = CollisionCategory.All };
        entityB.Add(colB);

        matrix.Register(colA);
        matrix.Register(colB);

        matrix.SetCanCollide(CollisionCategory.Player, CollisionCategory.Enemy, false);

        Assert.Equal(CollisionCategory.None, colA.Mask & CollisionCategory.Enemy);
        Assert.Equal(CollisionCategory.None, colB.Mask & CollisionCategory.Player);
    }
}
