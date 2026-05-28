using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Physics;
using Alca.MonoGame.Kernel.Weather;

namespace Alca.MonoGame.Kernel.UnitTests.Weather;

public sealed class WeatherBehaviourTests
{
    private static GameWorld CreateWorld(out WeatherWorld weather)
    {
        weather = new WeatherWorld();
        return new GameWorld { WeatherWorld = weather };
    }

    [Fact]
    public void Properties_Defaults()
    {
        var b = new WeatherBehaviour();
        Assert.False(b.ReceivesWind);
        Assert.False(b.ReceivesLightningImpulse);
        Assert.Equal(1f, b.WindForceMultiplier, 3);
    }

    [Fact]
    public void AddComponent_WithWeatherWorld_DoesNotThrow()
    {
        var gameWorld = CreateWorld(out _);
        var entity = gameWorld.CreateEntity("e");
        var ex = Record.Exception(() => entity.AddComponent<WeatherBehaviour>());
        Assert.Null(ex);
    }

    [Fact]
    public void AddComponent_WithoutWeatherWorld_DoesNotThrow()
    {
        var gameWorld = new GameWorld(); // no WeatherWorld
        var entity = gameWorld.CreateEntity("e");
        var ex = Record.Exception(() => entity.AddComponent<WeatherBehaviour>());
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyWindForce_ReceivesWindFalse_NoopWithoutRigidBody()
    {
        var gameWorld = CreateWorld(out _);
        var entity = gameWorld.CreateEntity("e");
        var behaviour = entity.AddComponent<WeatherBehaviour>();
        behaviour.ReceivesWind = false;

        var ex = Record.Exception(() => behaviour.ApplyWindForce(new Vector2(99f, 0f)));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyWindForce_NoRigidBody_Noop()
    {
        var gameWorld = CreateWorld(out _);
        var entity = gameWorld.CreateEntity("e");
        var behaviour = entity.AddComponent<WeatherBehaviour>();
        behaviour.ReceivesWind = true;

        // No RigidBody2D on the entity — should silently no-op
        var ex = Record.Exception(() => behaviour.ApplyWindForce(new Vector2(50f, 0f)));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyLightningImpulse_ReceivesImpulseFalse_Noop()
    {
        var gameWorld = CreateWorld(out _);
        var entity = gameWorld.CreateEntity("e");
        var behaviour = entity.AddComponent<WeatherBehaviour>();
        behaviour.ReceivesLightningImpulse = false;

        var ex = Record.Exception(() => behaviour.ApplyLightningImpulse(Vector2.Zero, 100f, 200f));
        Assert.Null(ex);
    }

    [Fact]
    public void ApplyLightningImpulse_OutsideRadius_NoVelocityChange()
    {
        var gameWorld = CreateWorld(out _);
        gameWorld.PhysicsWorld = new Physics2DWorld(Vector2.Zero);

        // Entity at (500, 0) — far outside radius 100
        var entity = gameWorld.CreateEntity("e", new Vector2(500f, 0f));
        var rb = entity.AddComponent<RigidBody2D>();
        rb.UseGravity = false;
        var behaviour = entity.AddComponent<WeatherBehaviour>();
        behaviour.ReceivesLightningImpulse = true;

        behaviour.ApplyLightningImpulse(Vector2.Zero, 100f, 300f);

        Assert.Equal(0f, rb.LinearVelocity.X, 2);
    }

    [Fact]
    public void ApplyLightningImpulse_InsideRadius_AppliesOutwardVelocity()
    {
        var gameWorld = CreateWorld(out _);
        gameWorld.PhysicsWorld = new Physics2DWorld(Vector2.Zero);

        // Entity at (10, 0) — well inside radius 100
        var entity = gameWorld.CreateEntity("e", new Vector2(10f, 0f));
        var rb = entity.AddComponent<RigidBody2D>();
        rb.UseGravity = false;
        var behaviour = entity.AddComponent<WeatherBehaviour>();
        behaviour.ReceivesLightningImpulse = true;

        // Strike at origin pushes entity rightward (positive X)
        behaviour.ApplyLightningImpulse(Vector2.Zero, 100f, 300f);

        Assert.True(rb.LinearVelocity.X > 0f, "Entity should be pushed away from strike point.");
    }

    [Fact]
    public void OnDestroy_UnregistersFromWeatherWorld()
    {
        var gameWorld = CreateWorld(out _);
        var entity = gameWorld.CreateEntity("e");
        entity.AddComponent<WeatherBehaviour>();

        // Destroy entity — should call OnDestroy without throwing
        gameWorld.Destroy(entity);
        var ex = Record.Exception(() =>
            gameWorld.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0))));
        Assert.Null(ex);
    }
}
