using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.Audio.Ambient;
using Alca.MonoGame.Kernel.Audio.Mixer;
using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class AudioZoneTests
{
    private static GameTime OneFrame()
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    [Fact]
    public void DefaultRadius_Is50()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        Assert.Equal(50f, zone.Radius);
    }

    [Fact]
    public void DefaultFadeInTime_Is1()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        Assert.Equal(1f, zone.FadeInTime);
    }

    [Fact]
    public void DefaultFadeOutTime_Is1()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        Assert.Equal(1f, zone.FadeOutTime);
    }

    [Fact]
    public void DefaultAmbientSound_IsNull()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        Assert.Null(zone.AmbientSound);
    }

    [Fact]
    public void DefaultMixerChannel_IsNull()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        Assert.Null(zone.MixerChannel);
    }

    [Fact]
    public void Awake_WithNoControllerOnWorld_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("zone");

        Exception? ex = Record.Exception(() => entity.AddComponent<AudioZoneBehaviour>());

        Assert.Null(ex);
    }

    [Fact]
    public void Update_WithNoAmbientSound_DoesNotThrow()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        entity.AddComponent<AudioZoneBehaviour>();

        Exception? ex = Record.Exception(() => world.Update(OneFrame()));

        Assert.Null(ex);
    }

    [Fact]
    public void Update_WithNoController_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("zone");
        entity.AddComponent<AudioZoneBehaviour>();

        Exception? ex = Record.Exception(() => world.Update(OneFrame()));

        Assert.Null(ex);
    }

    [Fact]
    public void Radius_CanBeSet()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();

        zone.Radius = 100f;

        Assert.Equal(100f, zone.Radius);
    }

    [Fact]
    public void MixerChannel_CanBeAssigned()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("zone");
        AudioZoneBehaviour zone = entity.AddComponent<AudioZoneBehaviour>();
        AudioMixerChannel channel = new("Ambient");

        zone.MixerChannel = channel;

        Assert.Same(channel, zone.MixerChannel);
    }

    [Fact]
    public void ListenerPositionViaController_ReflectsDefaultZero_WhenListenerNotUpdated()
    {
        AudioController controller = new();
        GameWorld world = new GameWorld { AudioController = controller };

        Assert.Equal(Vector3.Zero, controller.ListenerPosition);
    }
}
