using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class SpatialAudioSourceTests
{
    private static GameWorld CreateWorld()
        => new GameWorld { AudioController = new AudioController() };

    [Fact]
    public void DefaultSound_IsNull()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.Null(source.Sound);
    }

    [Fact]
    public void DefaultVolume_IsOne()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.Equal(1f, source.Volume);
    }

    [Fact]
    public void DefaultPitch_IsZero()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.Equal(0f, source.Pitch);
    }

    [Fact]
    public void DefaultLoop_IsFalse()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.False(source.Loop);
    }

    [Fact]
    public void DefaultPlayOnAwake_IsFalse()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.False(source.PlayOnAwake);
    }

    [Fact]
    public void State_BeforePlay_IsStopped()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Assert.Equal(SoundState.Stopped, source.State);
    }

    [Fact]
    public void MixerChannel_CanBeAssigned()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();
        AudioMixerChannel channel = new("SFX");

        source.MixerChannel = channel;

        Assert.Same(channel, source.MixerChannel);
    }

    [Fact]
    public void Play_WithNoSound_DoesNotThrow()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Exception? ex = Record.Exception(() => source.Play());

        Assert.Null(ex);
    }

    [Fact]
    public void Stop_WithNoActiveInstance_DoesNotThrow()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSource source = entity.AddComponent<SpatialAudioSource>();

        Exception? ex = Record.Exception(() => source.Stop());

        Assert.Null(ex);
    }

    [Fact]
    public void Awake_WithNoControllerOnWorld_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("emitter");

        Exception? ex = Record.Exception(() => entity.AddComponent<SpatialAudioSource>());

        Assert.Null(ex);
    }
}
