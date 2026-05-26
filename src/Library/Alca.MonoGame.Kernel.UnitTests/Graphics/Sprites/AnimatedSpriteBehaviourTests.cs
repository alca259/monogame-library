using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Graphics.Sprites;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Sprites;

public sealed class AnimatedSpriteBehaviourTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16));

    private static (GameWorld world, GameEntity entity, AnimatedSpriteBehaviour behaviour) CreateSetup()
    {
        var world = new GameWorld();
        var entity = world.CreateEntity("E");
        var behaviour = new AnimatedSpriteBehaviour();
        entity.Add(behaviour);
        return (world, entity, behaviour);
    }

    [Fact]
    public void Sprite_IsCreatedOnConstruction_NotNull()
    {
        var (_, _, behaviour) = CreateSetup();
        Assert.NotNull(behaviour.Sprite);
    }

    [Fact]
    public void Play_SetsAnimation_SpriteAnimationMatchesProvided()
    {
        var (world, _, behaviour) = CreateSetup();
        world.Update(AnyGameTime()); // flush entity
        Animation anim = new() { IsLooping = true };

        behaviour.Play(anim);

        Assert.Same(anim, behaviour.Sprite.Animation);
    }

    [Fact]
    public void Play_StartsPlayback_IsPlayingIsTrue()
    {
        var (world, _, behaviour) = CreateSetup();
        world.Update(AnyGameTime()); // flush entity
        Animation anim = new() { IsLooping = true };

        behaviour.Play(anim);

        Assert.True(behaviour.Sprite.IsPlaying);
    }

    [Fact]
    public void Update_WhenAttachedToEntity_WithNoAnimation_DoesNotThrow()
    {
        var (world, _, _) = CreateSetup();

        Exception? ex = Record.Exception(() =>
        {
            world.Update(AnyGameTime());
            world.Update(AnyGameTime());
        });

        Assert.Null(ex);
    }
}
