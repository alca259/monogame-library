using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Physics;

public sealed class RigidBody2DTests
{
    private static GameWorld CreateWorldWithPhysics(Vector2 gravity = default)
    {
        var physicsWorld = new Physics2DWorld(gravity == default ? new Vector2(0f, -9.8f) : gravity);
        return new GameWorld { PhysicsWorld = physicsWorld };
    }

    private static GameTime MakeGameTime(double dtSeconds)
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(dtSeconds));

    [Fact]
    public void Awake_WithPhysicsWorld_DoesNotThrow()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("test");
        var exception = Record.Exception(() => entity.AddComponent<RigidBody2D>());
        Assert.Null(exception);
    }

    [Fact]
    public void Awake_WithoutPhysicsWorld_ThrowsInvalidOperationException()
    {
        var gameWorld = new GameWorld(); // no PhysicsWorld
        var entity = gameWorld.CreateEntity("test");
        Assert.Throws<InvalidOperationException>(() => entity.AddComponent<RigidBody2D>());
    }

    [Fact]
    public void IsStatic_DefaultIsFalse()
    {
        var rb = new RigidBody2D();
        Assert.False(rb.IsStatic);
    }

    [Fact]
    public void UseGravity_Default_IsTrue()
    {
        var rb = new RigidBody2D();
        Assert.True(rb.UseGravity);
    }

    [Fact]
    public void Update_AfterPhysicsStep_SyncsTransformPosition()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("falling", new Vector2(0f, 0f));
        entity.AddComponent<RigidBody2D>();

        var dt = 1.0 / 60.0;
        var gameTime = MakeGameTime(dt);

        // One step — body should have a non-zero velocity after gravity
        gameWorld.Update(gameTime);

        // After one frame under gravity (0, -9.8), Y should have moved downward
        Assert.True(entity.Transform.Position2d.Y < 0f, "Entity should have fallen under gravity.");
    }

    [Fact]
    public void UseGravity_False_NoFall()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("floating", new Vector2(0f, 0f));
        var rb = entity.AddComponent<RigidBody2D>();
        rb.UseGravity = false;

        var gameTime = MakeGameTime(1.0 / 60.0);
        for (int i = 0; i < 60; i++)
            gameWorld.Update(gameTime);

        Assert.Equal(0f, entity.Transform.Position2d.Y, precision: 3);
    }

    [Fact]
    public void Fall_OneSecond_ReachesExpectedDistance()
    {
        // Verification from roadmap: gravity (0, -9.8), 1 second, ~4.9 units fallen.
        // Using small fixed steps: y = 0.5 * 9.8 * t^2 = 4.9
        var gameWorld = CreateWorldWithPhysics(new Vector2(0f, -9.8f));
        var entity = gameWorld.CreateEntity("body", new Vector2(0f, 0f));
        entity.AddComponent<RigidBody2D>();

        const double dt = 1.0 / 60.0;
        var gameTime = MakeGameTime(dt);
        for (int i = 0; i < 60; i++)
            gameWorld.Update(gameTime);

        float y = entity.Transform.Position2d.Y;
        // Allow 10 % tolerance for Euler integration error
        Assert.InRange(y, -5.4f, -4.4f);
    }

    [Fact]
    public void ApplyImpulse_ChangesLinearVelocity()
    {
        var gameWorld = CreateWorldWithPhysics(Vector2.Zero);
        var entity = gameWorld.CreateEntity("body");
        var rb = entity.AddComponent<RigidBody2D>();
        rb.UseGravity = false;

        rb.ApplyImpulse(new Vector2(10f, 0f));

        Assert.True(rb.LinearVelocity.X > 0f, "Linear velocity X should be positive after impulse.");
    }

    [Fact]
    public void StaticBody_DoesNotMoveUnderGravity()
    {
        var gameWorld = CreateWorldWithPhysics();
        var entity = gameWorld.CreateEntity("static", new Vector2(5f, 5f));
        var rb = entity.AddComponent<RigidBody2D>();
        rb.IsStatic = true;

        var gameTime = MakeGameTime(1.0 / 60.0);
        for (int i = 0; i < 60; i++)
            gameWorld.Update(gameTime);

        Assert.Equal(5f, entity.Transform.Position2d.X, precision: 3);
        Assert.Equal(5f, entity.Transform.Position2d.Y, precision: 3);
    }
}
