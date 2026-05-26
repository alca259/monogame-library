using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

public sealed class NetworkManagerBehaviourTests
{
    private const int TestPort = 49160;

    private static (GameWorld world, GameEntity entity, NetworkManagerBehaviour mgr) CreateSetup()
    {
        var world = new GameWorld();
        GameEntity entity = world.CreateEntity("NetworkManager");
        world.Update(CreateGameTime()); // flush so entity is registered
        var mgr = entity.AddComponent<NetworkManagerBehaviour>();
        return (world, entity, mgr);
    }

    private static GameTime CreateGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    [Fact]
    public void StartServer_SetsServerPropertyOnWorld()
    {
        (GameWorld world, _, NetworkManagerBehaviour mgr) = CreateSetup();
        try
        {
            mgr.StartServer(TestPort);
            Assert.NotNull(world.NetworkServer);
            Assert.Same(mgr.Server, world.NetworkServer);
        }
        finally
        {
            mgr.Stop();
        }
    }

    [Fact]
    public void StartClient_SetsClientPropertyOnWorld()
    {
        (GameWorld world, _, NetworkManagerBehaviour mgr) = CreateSetup();
        try
        {
            // Connect to a non-existent server — that is fine; we only check that
            // the property is assigned, not that the connection succeeds.
            mgr.StartClient("127.0.0.1", TestPort + 1);
            Assert.NotNull(world.NetworkClient);
            Assert.Same(mgr.Client, world.NetworkClient);
        }
        finally
        {
            mgr.Stop();
        }
    }

    [Fact]
    public void Stop_ClearsWorldProperties()
    {
        (GameWorld world, _, NetworkManagerBehaviour mgr) = CreateSetup();
        mgr.StartServer(TestPort + 2);
        Assert.NotNull(world.NetworkServer);

        mgr.Stop();

        Assert.Null(world.NetworkServer);
        Assert.Null(world.NetworkClient);
    }

    [Fact]
    public void Stop_WhenNotStarted_NoException()
    {
        (_, _, NetworkManagerBehaviour mgr) = CreateSetup();
        var ex = Record.Exception(() => mgr.Stop());
        Assert.Null(ex);
    }
}
