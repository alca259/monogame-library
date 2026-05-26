using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Physics;

public sealed class Physics2DWorldTests
{
    [Fact]
    public void Constructor_WithDefaultGravity_UsesNegativeY()
    {
        var world = new Physics2DWorld();
        Assert.Equal(0f, world.Gravity.X);
        Assert.Equal(-9.8f, world.Gravity.Y);
    }

    [Fact]
    public void Constructor_WithCustomGravity_SetsCorrectGravity()
    {
        var gravity = new Vector2(0f, -20f);
        var world = new Physics2DWorld(gravity);
        Assert.Equal(gravity, world.Gravity);
    }

    [Fact]
    public void Gravity_SetValue_UpdatesWorld()
    {
        var world = new Physics2DWorld();
        var newGravity = new Vector2(5f, -15f);
        world.Gravity = newGravity;
        Assert.Equal(newGravity, world.Gravity);
    }

    [Fact]
    public void Step_WithZeroDt_DoesNotThrow()
    {
        var world = new Physics2DWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.Zero);
        var exception = Record.Exception(() => world.Step(gameTime));
        Assert.Null(exception);
    }

    [Fact]
    public void DefaultIterations_AreBoxD2Recommended()
    {
        var world = new Physics2DWorld();
        Assert.Equal(8, world.VelocityIterations);
        Assert.Equal(3, world.PositionIterations);
    }

    [Fact]
    public void PhysicsWorld_WhenSetOnGameWorld_IsAutomaticallyStepped()
    {
        // GameWorld.Update should step the physics world automatically.
        var physicsWorld = new Physics2DWorld(new Vector2(0f, -9.8f));
        var gameWorld = new GameWorld { PhysicsWorld = physicsWorld };

        // Just verify no exception is thrown.
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
        gameWorld.Update(gameTime);
    }
}
