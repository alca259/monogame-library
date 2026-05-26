using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// <see cref="GameBehaviour"/> that bootstraps a network session. Add it to any entity and call
/// <see cref="StartServer"/>, <see cref="StartClient"/>, or <see cref="StartHost"/> to begin a session.
/// The behaviour polls the underlying transport every frame automatically.
/// </summary>
public sealed class NetworkManagerBehaviour : GameBehaviour
{
    /// <summary>Gets the active server instance, or null when not in server/host mode.</summary>
    public NetworkServer? Server { get; private set; }

    /// <summary>Gets the active client instance, or null when not in client/host mode.</summary>
    public NetworkClient? Client { get; private set; }

    /// <summary>Gets the current network role of this peer.</summary>
    public NetworkMode Mode { get; private set; }

    /// <summary>
    /// Starts a dedicated server on <paramref name="port"/> and registers it with the world.
    /// </summary>
    public void StartServer(int port, int maxConnections = 64)
    {
        Server = new NetworkServer(maxConnections);
        Mode = NetworkMode.Server;
        Server.Start(port);
        Entity.World.NetworkServer = Server;
    }

    /// <summary>
    /// Starts a pure client and connects it to <paramref name="host"/>:<paramref name="port"/>.
    /// Registers the client with the world.
    /// </summary>
    public void StartClient(string host, int port)
    {
        Client = new NetworkClient();
        Mode = NetworkMode.Client;
        Client.Connect(host, port);
        Entity.World.NetworkClient = Client;
    }

    /// <summary>
    /// Starts a listen-server (host + client on the same peer) on <paramref name="port"/>.
    /// Both server and client are registered with the world.
    /// </summary>
    public void StartHost(int port)
    {
        Server = new NetworkServer();
        Client = new NetworkClient();
        Mode = NetworkMode.Host;
        Server.Start(port);
        Client.Connect("127.0.0.1", port);
        Entity.World.NetworkServer = Server;
        Entity.World.NetworkClient = Client;
    }

    /// <summary>Stops all active connections and clears the world references.</summary>
    public void Stop()
    {
        Server?.Stop();
        Client?.Disconnect();

        if (Entity.World.NetworkServer == Server)
            Entity.World.NetworkServer = null;
        if (Entity.World.NetworkClient == Client)
            Entity.World.NetworkClient = null;

        Server = null;
        Client = null;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        Server?.Poll();
        Client?.Poll();
    }

    /// <inheritdoc/>
    public override void OnDestroy() => Stop();
}
