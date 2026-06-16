using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.Audio.Mixer;
using Alca.MonoGame.Kernel.Audio.Spatial;
using Alca.MonoGame.Kernel.ECS;
using Microsoft.Xna.Framework.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class SpatialAudioSourceTests
{
    private static GameWorld CreateWorld()
        => new GameWorld { AudioController = new AudioController(new AudioMixer()) };

    [Fact]
    public void DefaultSound_IsNull()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.Null(source.Sound);
    }

    [Fact]
    public void DefaultVolume_IsOne()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.Equal(1f, source.Volume);
    }

    [Fact]
    public void DefaultPitch_IsZero()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.Equal(0f, source.Pitch);
    }

    [Fact]
    public void DefaultLoop_IsFalse()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.False(source.Loop);
    }

    [Fact]
    public void DefaultPlayOnAwake_IsFalse()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.False(source.PlayOnAwake);
    }

    [Fact]
    public void State_BeforePlay_IsStopped()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Assert.Equal(SoundState.Stopped, source.State);
    }

    [Fact]
    public void MixerChannel_CanBeAssigned()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();
        AudioMixerChannel channel = new("SFX");

        source.MixerChannel = channel;

        Assert.Same(channel, source.MixerChannel);
    }

    [Fact]
    public void Play_WithNoSound_DoesNotThrow()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Exception? ex = Record.Exception(() => source.Play());

        Assert.Null(ex);
    }

    [Fact]
    public void Stop_WithNoActiveInstance_DoesNotThrow()
    {
        GameWorld world = CreateWorld();
        GameEntity entity = world.CreateEntity("emitter");
        SpatialAudioSourceBehaviour source = entity.AddComponent<SpatialAudioSourceBehaviour>();

        Exception? ex = Record.Exception(() => source.Stop());

        Assert.Null(ex);
    }

    [Fact]
    public void Awake_WithNoControllerOnWorld_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("emitter");

        Exception? ex = Record.Exception(() => entity.AddComponent<SpatialAudioSourceBehaviour>());

        Assert.Null(ex);
    }
}
