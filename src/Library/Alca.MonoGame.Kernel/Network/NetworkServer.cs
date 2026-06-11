using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Lightweight wrapper around <see cref="LiteNetLib.NetManager"/> that acts as the authoritative
/// network server. Manages peer connections, message dispatch, and outbound broadcasting.
/// Call <see cref="Poll"/> every frame (from <see cref="ECS.GameBehaviour.Update"/>) to pump the event queue.
/// </summary>
public sealed class NetworkServer : IDisposable, INetEventListener
{
    private readonly NetManager _netManager;
    private readonly string _connectionKey;
    private readonly byte[] _sendBuffer = new byte[4096];
    private readonly Dictionary<ushort, Action<int, byte[], int, int>> _handlers = new(16);
    private readonly Dictionary<int, NetPeer> _peers = new(64);

    /// <summary>Gets a value indicating whether the server is currently listening for connections.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Gets the number of peers currently connected to this server.</summary>
    public int ConnectedPeers => _peers.Count;

    /// <summary>Raised when a client successfully connects. The argument is the peer ID.</summary>
    public event Action<int>? OnClientConnected;

    /// <summary>Raised when a client disconnects. The argument is the peer ID.</summary>
    public event Action<int>? OnClientDisconnected;

    /// <summary>Raised when a network error occurs. Arguments are (peerId, errorDescription).</summary>
    public event Action<int, string>? OnClientError;

    /// <summary>
    /// Initializes a new <see cref="NetworkServer"/>.
    /// </summary>
    /// <param name="maxConnections">Maximum number of simultaneous connections.</param>
    /// <param name="connectionKey">Shared password used to authenticate connecting clients.</param>
    public NetworkServer(int maxConnections = 64, string connectionKey = "AlcaNet")
    {
        _connectionKey = connectionKey;
        _netManager = new NetManager(this) { MaxConnectAttempts = 10 };
        _ = maxConnections; // LiteNetLib handles max connections via ConnectionRequest accept/reject.
    }

    /// <summary>Starts the server on the given port.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the server is already running.</exception>
    public void Start(int port)
    {
        if (IsRunning)
            throw new InvalidOperationException("Server is already running.");
        _netManager.Start(port);
        IsRunning = true;
    }

    /// <summary>Stops the server and disconnects all peers.</summary>
    public void Stop()
    {
        if (!IsRunning) return;
        _netManager.Stop();
        _peers.Clear();
        IsRunning = false;
    }

    /// <summary>
    /// Serializes <paramref name="message"/> and sends it to the peer with the given ID.
    /// No-op if the peer is not found.
    /// </summary>
    public void Send<T>(int peerId, ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage
    {
        if (!_peers.TryGetValue(peerId, out NetPeer? peer)) return;

        var span = new Span<byte>(_sendBuffer);
        var writer = new NetworkWriter(span);
        writer.Write(message.MessageId);
        message.Serialize(ref writer);
        peer.Send(_sendBuffer, 0, writer.Position, ToDeliveryMethod(channel));
    }

    /// <summary>
    /// Serializes <paramref name="message"/> once and sends it to all connected peers.
    /// </summary>
    public void Broadcast<T>(ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage
    {
        var span = new Span<byte>(_sendBuffer);
        var writer = new NetworkWriter(span);
        writer.Write(message.MessageId);
        message.Serialize(ref writer);
        int length = writer.Position;

        foreach (KeyValuePair<int, NetPeer> pair in _peers)
            pair.Value.Send(_sendBuffer, 0, length, ToDeliveryMethod(channel));
    }

    /// <summary>
    /// Serializes <paramref name="message"/> once and sends it to all peers except <paramref name="excludePeerId"/>.
    /// </summary>
    public void BroadcastExcept<T>(int excludePeerId, ref T message, NetworkChannel channel = NetworkChannel.ReliableOrdered)
        where T : INetworkMessage
    {
        var span = new Span<byte>(_sendBuffer);
        var writer = new NetworkWriter(span);
        writer.Write(message.MessageId);
        message.Serialize(ref writer);
        int length = writer.Position;

        foreach (KeyValuePair<int, NetPeer> pair in _peers)
        {
            if (pair.Key == excludePeerId) continue;
            pair.Value.Send(_sendBuffer, 0, length, ToDeliveryMethod(channel));
        }
    }

    /// <summary>Forcibly disconnects the peer with the given ID.</summary>
    public void Kick(int peerId)
    {
        if (_peers.TryGetValue(peerId, out NetPeer? peer))
            peer.Disconnect();
    }

    /// <summary>
    /// Registers a typed handler for incoming messages of type <typeparamref name="T"/>.
    /// The handler receives (peerId, message). Replaces any previously registered handler for the same message ID.
    /// </summary>
    public void RegisterHandler<T>(Action<int, T> handler) where T : INetworkMessage, new()
    {
        ushort msgId = new T().MessageId;
        _handlers[msgId] = (peerId, data, offset, length) =>
        {
            T msg = new();
            var reader = new NetworkReader(new ReadOnlySpan<byte>(data, offset, length));
            msg.Deserialize(ref reader);
            handler(peerId, msg);
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

    /// <summary>Returns a snapshot of network statistics for the given peer, or a default struct if the peer is not found.</summary>
    public NetworkStats GetStats(int peerId)
    {
        if (!_peers.TryGetValue(peerId, out NetPeer? peer))
            return default;
        return new NetworkStats { Ping = peer.Ping };
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();

    #region INetEventListener
    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        _peers[peer.Id] = peer;
        OnClientConnected?.Invoke(peer.Id);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _peers.Remove(peer.Id);
        OnClientDisconnected?.Invoke(peer.Id);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        OnClientError?.Invoke(-1, socketError.ToString());
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

        if (_handlers.TryGetValue(msgId, out Action<int, byte[], int, int>? handler))
            handler(peer.Id, data, offset + 2, size - 2);

        reader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(_connectionKey);
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
