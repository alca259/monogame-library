using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network.Messages;
using Alca.MonoGame.Kernel.Network.NetFields;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Marks a <see cref="GameEntity"/> as a networked object and manages automatic delta-sync of
/// registered <see cref="NetField"/> instances. Attach this component alongside the behaviours
/// that own the fields and call <see cref="RegisterField"/> from their <c>Awake</c> methods.
/// </summary>
public sealed class NetworkIdentity : GameBehaviour
{
    private const int MaxFields = 64;
    private const float DefaultSyncInterval = 0.05f; // 20 Hz

    private readonly NetField[] _fields = new NetField[MaxFields];
    private int _fieldCount;
    private NetworkServer? _server;
    private NetworkClient? _client;
    private float _syncTimer;
    private FieldsSyncMessage _outboundFieldsMsg = new();

    /// <summary>Gets the network-unique identifier assigned by the server. 0 means not yet assigned.</summary>
    public uint NetworkId { get; internal set; }

    /// <summary>Gets a value indicating whether the local peer is the authoritative owner of this entity.</summary>
    public bool IsOwner { get; internal set; }

    /// <summary>Gets a value indicating whether the local peer is the server.</summary>
    public bool IsServer { get; internal set; }

    /// <summary>
    /// Raised after an incoming <see cref="FieldsSyncMessage"/> has been applied to this identity's fields.
    /// Subscribe in <see cref="GameBehaviour.Awake"/> to copy net-field values back to source properties.
    /// </summary>
    public event Action? OnFieldsApplied;

    /// <summary>
    /// Gets or sets how often (in seconds) dirty fields are flushed to the network.
    /// Default is 0.05 s (20 Hz).
    /// </summary>
    public float SyncInterval { get; set; } = DefaultSyncInterval;

    /// <summary>
    /// Registers a <see cref="NetField"/> for automatic delta-sync.
    /// Call from the owning component's <c>Awake</c> method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when more than <c>64</c> fields are registered.</exception>
    public void RegisterField(NetField field)
    {
        if (_fieldCount >= MaxFields)
            throw new InvalidOperationException($"Cannot register more than {MaxFields} fields per NetworkIdentity.");
        _fields[_fieldCount++] = field;
    }

    /// <inheritdoc/>
    public override void Awake()
    {
        _server = Entity.World.NetworkServer;
        _client = Entity.World.NetworkClient;
        IsServer = _server is not null;

        if (_client is not null && !IsServer)
            _client.RegisterHandler<FieldsSyncMessage>(OnFieldsSyncReceived);

        if (_server is not null)
            _server.RegisterHandler<FieldsSyncMessage>(OnFieldsSyncReceivedFromClient);
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (NetworkId == 0) return;
        if (!IsOwner) return;

        _syncTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_syncTimer < SyncInterval) return;
        _syncTimer = 0f;

        bool hasDirty = false;
        for (int i = 0; i < _fieldCount; i++)
        {
            if (_fields[i].IsDirty)
            {
                hasDirty = true;
                break;
            }
        }

        if (!hasDirty) return;

        _outboundFieldsMsg.PrepareForSend(NetworkId, _fields, _fieldCount);

        if (IsServer && _server is not null)
            _server.Broadcast(ref _outboundFieldsMsg, NetworkChannel.ReliableOrdered);
        else if (_client is not null)
            _client.Send(ref _outboundFieldsMsg, NetworkChannel.ReliableOrdered);

        for (int i = 0; i < _fieldCount; i++)
            _fields[i].ClearDirty();
    }

    /// <summary>Returns a read-only span over the registered fields.</summary>
    internal ReadOnlySpan<NetField> GetFields() => new(_fields, 0, _fieldCount);

    private void OnFieldsSyncReceived(FieldsSyncMessage msg)
    {
        if (msg.NetworkId != NetworkId) return;
        msg.ApplyTo(_fields, _fieldCount);
        OnFieldsApplied?.Invoke();
    }

    private void OnFieldsSyncReceivedFromClient(int peerId, FieldsSyncMessage msg)
    {
        if (msg.NetworkId != NetworkId) return;
        msg.ApplyTo(_fields, _fieldCount);
        OnFieldsApplied?.Invoke();

        if (_server is not null)
            _server.BroadcastExcept(peerId, ref msg, NetworkChannel.ReliableOrdered);
    }
}
