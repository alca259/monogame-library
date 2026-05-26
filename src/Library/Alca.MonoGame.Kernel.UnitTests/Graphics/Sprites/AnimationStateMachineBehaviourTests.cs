using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Graphics.Sprites;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Sprites;

public sealed class AnimationStateMachineBehaviourTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16));

    private static (GameWorld world, GameEntity entity, AnimationStateMachineBehaviour behaviour) CreateSetup()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var behaviour = new AnimationStateMachineBehaviour();
        entity.Add(behaviour);
        return (world, entity, behaviour);
    }

    [Fact]
    public void StateMachine_IsCreatedOnConstruction_NotNull()
    {
        var (_, _, behaviour) = CreateSetup();
        Assert.NotNull(behaviour.StateMachine);
    }

    [Fact]
    public void CurrentState_IsNull_BeforeAnyPlay()
    {
        var (_, _, behaviour) = CreateSetup();
        Assert.Null(behaviour.CurrentState);
    }

    [Fact]
    public void Play_UnknownState_ThrowsKeyNotFoundException()
    {
        var (world, _, behaviour) = CreateSetup();
        world.Update(AnyGameTime()); // flush entity

        Assert.Throws<KeyNotFoundException>(() => behaviour.Play("nonexistent"));
    }

    [Fact]
    public void Play_KnownState_SetsCurrentState()
    {
        var (world, _, behaviour) = CreateSetup();
        world.Update(AnyGameTime()); // flush entity
        behaviour.StateMachine.Register("idle", new Animation { IsLooping = true });

        behaviour.Play("idle");

        Assert.Equal("idle", behaviour.CurrentState);
    }

    [Fact]
    public void Update_WhenAttachedToEntity_DoesNotThrow()
    {
        var (world, _, behaviour) = CreateSetup();
        behaviour.StateMachine.Register("walk", new Animation { IsLooping = true });
        behaviour.Play("walk");

        Exception? ex = Record.Exception(() =>
        {
            world.Update(AnyGameTime());
            world.Update(AnyGameTime());
        });

        Assert.Null(ex);
    }
}
