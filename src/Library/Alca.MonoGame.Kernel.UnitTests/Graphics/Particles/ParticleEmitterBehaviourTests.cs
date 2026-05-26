using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Graphics.Particles;
using MonoGame.Extended.Particles;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Particles;

public sealed class ParticleEmitterBehaviourTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    private static (GameWorld world, GameEntity entity, ParticleEmitterBehaviour behaviour, ParticleEffect effect) CreateSetup(Vector2 entityPosition = default)
    {
        var particleEffect = new ParticleEffect("test");
        var wrapper = new ParticleEffectWrapper(particleEffect);
        var behaviour = new ParticleEmitterBehaviour(wrapper);
        var world = new GameWorld();
        var entity = world.CreateEntity("E", entityPosition);
        entity.Add(behaviour);
        return (world, entity, behaviour, particleEffect);
    }

    [Fact]
    public void Effect_IsCreatedOnConstruction_NotNull()
    {
        var (_, _, behaviour, _) = CreateSetup();
        Assert.NotNull(behaviour.Effect);
    }

    [Fact]
    public void Update_WhenUseEntityPosition_PassesTransformPositionToWrapper()
    {
        var entityPos = new Vector2(10f, 20f);
        var (world, _, behaviour, effect) = CreateSetup(entityPos);
        behaviour.UseEntityPosition = true;

        world.Update(AnyGameTime());

        Assert.Equal(entityPos, effect.Position);
    }

    [Fact]
    public void Update_WithOffset_AddsOffsetToPosition()
    {
        var entityPos = new Vector2(10f, 20f);
        var offset = new Vector2(5f, 3f);
        var (world, _, behaviour, effect) = CreateSetup(entityPos);
        behaviour.UseEntityPosition = true;
        behaviour.Offset = offset;

        world.Update(AnyGameTime());

        Assert.Equal(entityPos + offset, effect.Position);
    }

    [Fact]
    public void Trigger_PassesEntityPositionToWrapper_DoesNotThrow()
    {
        var entityPos = new Vector2(30f, 40f);
        var (world, _, behaviour, _) = CreateSetup(entityPos);
        world.Update(AnyGameTime()); // flush entity into world update loop

        Exception? ex = Record.Exception(() => behaviour.Trigger());

        Assert.Null(ex);
    }
}
