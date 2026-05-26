using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Physics;

public sealed class BoxCollider2DTests
{
    private static GameWorld CreateWorldWithPhysics()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        return new GameWorld { PhysicsWorld = physicsWorld };
    }

    [Fact]
    public void Awake_WithRigidBody_AttachesToExistingBody()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("box");
        entity.Add(new RigidBody2D());
        var exception = Record.Exception(() => entity.Add(new BoxCollider2D()));
        Assert.Null(exception);
    }

    [Fact]
    public void Awake_WithoutRigidBody_CreatesImplicitStaticBody()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("static-box");
        var exception = Record.Exception(() => entity.Add(new BoxCollider2D()));
        Assert.Null(exception);
    }

    [Fact]
    public void Awake_WithoutPhysicsWorld_ThrowsInvalidOperationException()
    {
        var gameWorld = new GameWorld();
        var entity = gameWorld.CreateEntity("test");
        Assert.Throws<InvalidOperationException>(() => entity.Add(new BoxCollider2D()));
    }

    [Fact]
    public void DefaultSize_IsOneByOne()
    {
        var collider = new BoxCollider2D();
        Assert.Equal(Vector2.One, collider.Size);
    }

    [Fact]
    public void IsTrigger_Default_IsFalse()
    {
        var collider = new BoxCollider2D();
        Assert.False(collider.IsTrigger);
    }

    [Fact]
    public void OnCollisionEnter_WhenBodiesOverlap_Fires()
    {
        var physicsWorld = new Physics2DWorld(Vector2.Zero);
        var gameWorld = new GameWorld { PhysicsWorld = physicsWorld };

        // Two entities at the same position (will immediately overlap)
        var entityA = gameWorld.CreateEntity("A", new Vector2(0f, 0f));
        var rbA = entityA.Add(new RigidBody2D());
        Collider2D? received = null;
        var colA = new BoxCollider2D { Size = new Vector2(1f, 1f) };
        colA.OnCollisionEnter = c => received = c;
        entityA.Add(colA);

        var entityB = gameWorld.CreateEntity("B", new Vector2(0f, 0f));
        entityB.Add(new RigidBody2D());
        entityB.Add(new BoxCollider2D { Size = new Vector2(1f, 1f) });

        // Need at least one step so Aether processes the contact
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
        gameWorld.Update(gameTime);
        gameWorld.Update(gameTime);

        Assert.NotNull(received);
    }
}
