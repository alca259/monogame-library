using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network.Messages;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Automatically synchronizes an entity's world-space transform over the network.
/// Requires that the same entity also has a <see cref="NetworkIdentity"/> component.
/// Owners send at <see cref="SyncInterval"/> only when position or rotation exceeds the
/// configured thresholds; non-owners optionally interpolate received values.
/// </summary>
public sealed class NetworkTransformSync : GameBehaviour
{
    private NetworkIdentity? _identity;
    private NetworkServer? _server;
    private NetworkClient? _client;
    private TransformSyncMessage _outMsg = new();
    private Vector3 _lastPosition;
    private float _lastRotation;
    private float _syncTimer;

    // Interpolation targets (client-side)
    private Vector3 _targetPosition;
    private float _targetRotation;
    private bool _hasTarget;

    /// <summary>Gets or sets the minimum position delta (in world units) that triggers a sync. Default 0.01.</summary>
    public float PositionThreshold { get; set; } = 0.01f;

    /// <summary>Gets or sets the minimum rotation delta (in radians) that triggers a sync. Default 0.01.</summary>
    public float RotationThreshold { get; set; } = 0.01f;

    /// <summary>Gets or sets how often (in seconds) the transform is checked and sent. Default ~30 Hz (0.033 s).</summary>
    public float SyncInterval { get; set; } = 0.033f;

    /// <summary>
    /// Gets or sets whether received transform values are interpolated toward the target (client-side only).
    /// Default true.
    /// </summary>
    public bool Interpolate { get; set; } = true;

    /// <inheritdoc/>
    public override void Awake()
    {
        _identity = Entity.GetComponent<NetworkIdentity>();
        _server = Entity.World.NetworkServer;
        _client = Entity.World.NetworkClient;

        if (_client is not null)
            _client.RegisterHandler<TransformSyncMessage>(OnTransformReceived);
        if (_server is not null)
            _server.RegisterHandler<TransformSyncMessage>(OnTransformReceivedFromClient);

        _lastPosition = Entity.Transform.Position;
        _lastRotation = Entity.Transform.Rotation2d;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (_identity is null || _identity.NetworkId == 0) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Interpolate on non-owner clients.
        if (_hasTarget && !_identity.IsOwner && Interpolate)
        {
            Entity.Transform.Position = Vector3.Lerp(Entity.Transform.Position, _targetPosition, dt * 15f);
            float currentRot = Entity.Transform.Rotation2d;
            Entity.Transform.Rotation2d = MathHelper.Lerp(currentRot, _targetRotation, dt * 15f);
        }

        if (!_identity.IsOwner) return;

        _syncTimer += dt;
        if (_syncTimer < SyncInterval) return;
        _syncTimer = 0f;

        Vector3 pos = Entity.Transform.Position;
        float rot = Entity.Transform.Rotation2d;

        float posDelta = Vector3.Distance(pos, _lastPosition);
        float rotDelta = Math.Abs(rot - _lastRotation);

        if (posDelta < PositionThreshold && rotDelta < RotationThreshold) return;

        _lastPosition = pos;
        _lastRotation = rot;

        _outMsg.NetworkId = _identity.NetworkId;
        _outMsg.Position = pos;
        _outMsg.Rotation = rot;
        _outMsg.LocalScale = Entity.Transform.LocalScale2d;

        if (_identity.IsServer && _server is not null)
            _server.Broadcast(ref _outMsg, NetworkChannel.Sequenced);
        else if (_client is not null)
            _client.Send(ref _outMsg, NetworkChannel.Sequenced);
    }

    private void OnTransformReceived(TransformSyncMessage msg)
    {
        if (_identity is null || msg.NetworkId != _identity.NetworkId) return;
        if (_identity.IsOwner) return; // Ignore echo of our own data.

        if (Interpolate)
        {
            _targetPosition = msg.Position;
            _targetRotation = msg.Rotation;
            _hasTarget = true;
        }
        else
        {
            Entity.Transform.Position = msg.Position;
            Entity.Transform.Rotation2d = msg.Rotation;
        }

        Entity.Transform.LocalScale2d = msg.LocalScale;
    }

    private void OnTransformReceivedFromClient(int peerId, TransformSyncMessage msg)
    {
        if (_identity is null || msg.NetworkId != _identity.NetworkId) return;

        // Server applies authoritatively and re-broadcasts to all other clients.
        Entity.Transform.Position = msg.Position;
        Entity.Transform.Rotation2d = msg.Rotation;
        Entity.Transform.LocalScale2d = msg.LocalScale;

        if (_server is not null)
            _server.BroadcastExcept(peerId, ref msg, NetworkChannel.Sequenced);
    }
}
