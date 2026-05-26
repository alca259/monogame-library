using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class SpatialAudioListenerTests
{
    private static GameTime OneFrame()
        => new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    [Fact]
    public void DefaultIsMain_IsTrue()
    {
        GameWorld world = new GameWorld { AudioController = new AudioController() };
        GameEntity entity = world.CreateEntity("listener");
        SpatialAudioListener listener = entity.AddComponent<SpatialAudioListener>();

        Assert.True(listener.IsMain);
    }

    [Fact]
    public void Awake_WithNoControllerOnWorld_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("listener");

        Exception? ex = Record.Exception(() => entity.AddComponent<SpatialAudioListener>());

        Assert.Null(ex);
    }

    [Fact]
    public void Update_WithNoController_DoesNotThrow()
    {
        GameWorld world = new GameWorld();
        GameEntity entity = world.CreateEntity("listener");
        entity.AddComponent<SpatialAudioListener>();

        Exception? ex = Record.Exception(() => world.Update(OneFrame()));

        Assert.Null(ex);
    }

    [Fact]
    public void Update_SyncsListenerPosition_XAxis()
    {
        AudioController controller = new();
        GameWorld world = new GameWorld { AudioController = controller };
        GameEntity entity = world.CreateEntity("listener", new Vector3(7f, 0f, 0f));
        entity.AddComponent<SpatialAudioListener>();

        world.Update(OneFrame());

        Assert.Equal(7f, controller.ListenerPosition.X, precision: 4);
    }

    [Fact]
    public void Update_SyncsListenerPosition_YAxis()
    {
        AudioController controller = new();
        GameWorld world = new GameWorld { AudioController = controller };
        GameEntity entity = world.CreateEntity("listener", new Vector3(0f, 12f, 0f));
        entity.AddComponent<SpatialAudioListener>();

        world.Update(OneFrame());

        Assert.Equal(12f, controller.ListenerPosition.Y, precision: 4);
    }

    [Fact]
    public void Update_SyncsListenerPosition_ZAxis()
    {
        AudioController controller = new();
        GameWorld world = new GameWorld { AudioController = controller };
        GameEntity entity = world.CreateEntity("listener", new Vector3(0f, 0f, 5f));
        entity.AddComponent<SpatialAudioListener>();

        world.Update(OneFrame());

        Assert.Equal(5f, controller.ListenerPosition.Z, precision: 4);
    }

    [Fact]
    public void Update_SyncsListenerPosition_AllThreeAxes()
    {
        AudioController controller = new();
        GameWorld world = new GameWorld { AudioController = controller };
        Vector3 expected = new(3f, 8f, -2f);
        GameEntity entity = world.CreateEntity("listener", expected);
        entity.AddComponent<SpatialAudioListener>();

        world.Update(OneFrame());

        Assert.Equal(expected.X, controller.ListenerPosition.X, precision: 4);
        Assert.Equal(expected.Y, controller.ListenerPosition.Y, precision: 4);
        Assert.Equal(expected.Z, controller.ListenerPosition.Z, precision: 4);
    }

    [Fact]
    public void Update_WhenIsMainFalse_DoesNotUpdateController()
    {
        AudioController controller = new();
        controller.UpdateListener(new Vector3(99f, 99f, 99f), Vector3.Forward);

        GameWorld world = new GameWorld { AudioController = controller };
        GameEntity entity = world.CreateEntity("listener", new Vector3(1f, 2f, 3f));
        SpatialAudioListener listener = entity.AddComponent<SpatialAudioListener>();
        listener.IsMain = false;

        world.Update(OneFrame());

        Assert.Equal(99f, controller.ListenerPosition.X, precision: 4);
    }
}
