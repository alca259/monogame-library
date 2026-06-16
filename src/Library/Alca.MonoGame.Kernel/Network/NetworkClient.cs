using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Lightweight wrapper around <see cref="LiteNetLib.NetManager"/> that acts as a network client.
/// Connects to a <see cref="NetworkServer"/> and dispatches incoming typed messages.
/// Call <see cref="Poll"/> every frame (from <see cref="ECS.GameBehaviour.Update"/>) to pump the event queue.
/// </summary>
public sealed class NetworkClient : IDisposable, INetEventListener
{
    private readonly NetManager _netManager;
    private readonly string _connectionKey;
    private readonly byte[] _sendBuffer = new byte[4096];
    private readonly Dictionary<ushort, Action<byte[], int, int>> _handlers = new(16);
    private NetPeer? _serverPeer;

    /// <summary>Gets a value indicating whether the client is currently connected to a server.</summary>
    public bool IsConnected { get; private set; }

    /// <summary>Gets the round-trip latency in milliseconds. Returns 0 when not connected.</summary>
    public int Ping => _serverPeer?.Ping ?? 0;

    /// <summary>Gets the local peer ID assigned by LiteNetLib. Only valid while connected.</summary>
    public int LocalPeerId { get; private set; }

    /// <summary>Raised when the client successfully connects to the server.</summary>
    public event Action? OnConnected;

    /// <summary>Raised when the client disconnects. The argument is the disconnect reason string.</summary>
    public event Action<string>? OnDisconnected;

    /// <summary>Raised when the initial connection attempt fails.</summary>
    public event Action? OnConnectionFailed;

    /// <summary>
    /// Initializes a new <see cref="NetworkClient"/>.
    /// </summary>
    /// <param name="connectionKey">Shared password sent to the server during the handshake.</param>
    public NetworkClient(string connectionKey = "AlcaNet")
    {
        _connectionKey = connectionKey;
        _netManager = new NetManager(this) { MaxConnectAttempts = 10 };
        _netManager.Start();
    }

    /// <summary>Initiates an asynchronous connection to <paramref name="host"/>:<paramref name="port"/>.</summary>
    public void Connect(string host, int port) =>
        _netManager.Connect(host, port, _connectionKey);

    /// <summary>Disconnects from the server gracefully.</summary>
    public void Disconnect()
    {
        _serverPeer?.Disconnect();
        IsConnected = false;
    }

    /// <summary>
    /// Serializes <paramref name="message"/> and sends it to the server.
    /// No-op if not connected.
    /// </summary>
    public void Send<T>(ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage
    {
        if (_serverPeer is null) return;

        var span = new Span<byte>(_sendBuffer);
        var writer = new NetworkWriter(span);
        writer.Write(message.MessageId);
        message.Serialize(ref writer);
        _serverPeer.Send(_sendBuffer, 0, writer.Position, ToDeliveryMethod(channel));
    }

    /// <summary>
    /// Registers a typed handler for incoming messages of type <typeparamref name="T"/>.
    /// Replaces any previously registered handler for the same message ID.
    /// </summary>
    public void RegisterHandler<T>(Action<T> handler) where T : INetworkMessage, new()
    {
        ushort msgId = new T().MessageId;
        _handlers[msgId] = (data, offset, length) =>
        {
            T msg = new();
            var reader = new NetworkReader(new ReadOnlySpan<byte>(data, offset, length));
            msg.Deserialize(ref reader);
            handler(msg);
        };
    }

    /// <summary>Removes the handler registered for messages of type <typeparamref name="T"/>. No-op if none registered.</summary>
    public void UnregisterHandler<T>() where T : INetworkMessage, new()
    {
        ushort msgId = new T().MessageId;
        _handlers.Remove(msgId);
    }

    /// <summary>Pumps the underlying network event queue. Must be called every frame.</summary>
    public void Poll() => _netManager.PollEvents();

    /// <summary>Returns a snapshot of network statistics for the current connection, or a default struct if not connected.</summary>
    public NetworkStats GetStats()
    {
        if (_serverPeer is null) return default;
        return new NetworkStats { Ping = _serverPeer.Ping };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Disconnect();
        _netManager.Stop();
    }

    #region INetEventListener
    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        _serverPeer = peer;
        IsConnected = true;
        LocalPeerId = peer.Id;
        OnConnected?.Invoke();
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _serverPeer = null;
        IsConnected = false;
        OnDisconnected?.Invoke(disconnectInfo.Reason.ToString());
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        if (!IsConnected)
            OnConnectionFailed?.Invoke();
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (reader.UserDataSize < 2)
        {
            reader.Recycle();
            return;
        }

        byte[] data = reader.RawData;
        int offset = reader.UserDataOffset;
        int size = reader.UserDataSize;

        ushort msgId = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(data, offset, 2));

        if (_handlers.TryGetValue(msgId, out Action<byte[], int, int>? handler))
            handler(data, offset + 2, size - 2);

        reader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        // Clients do not accept incoming connections.
    }
    #endregion

    #region Helpers
    private static DeliveryMethod ToDeliveryMethod(NetworkChannel channel) => channel switch
    {
        NetworkChannel.Unreliable => DeliveryMethod.Unreliable,
        NetworkChannel.ReliableUnordered => DeliveryMethod.ReliableUnordered,
        NetworkChannel.ReliableOrdered => DeliveryMethod.ReliableOrdered,
        NetworkChannel.Sequenced => DeliveryMethod.Sequenced,
        _ => DeliveryMethod.ReliableOrdered
    };
    #endregion
}
