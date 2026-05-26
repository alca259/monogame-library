using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network.Messages;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Synchronizes a physics rigid body's velocity and position over the network.
/// Requires <see cref="NetworkIdentity"/> and <see cref="RigidBody2D"/> on the same entity.
/// Owners send at <see cref="SyncInterval"/> when velocity exceeds thresholds; non-owners
/// optionally interpolate received velocity values.
/// </summary>
public sealed class NetworkPhysicsSync : GameBehaviour
{
    private NetworkIdentity? _identity;
    private RigidBody2D? _rigidBody;
    private NetworkServer? _server;
    private NetworkClient? _client;
    private PhysicsSyncMessage _outMsg = new();
    private float _syncTimer;

    // Interpolation targets (non-owner)
    private Vector2 _targetVelocity;
    private bool _hasTarget;

    /// <summary>Gets or sets how often (in seconds) the physics state is sampled and sent. Default 0.05 s (20 Hz).</summary>
    public float SyncInterval { get; set; } = 0.05f;

    /// <summary>Gets or sets the minimum position delta that triggers a send. Default 0.05.</summary>
    public float PositionThreshold { get; set; } = 0.05f;

    /// <summary>Gets or sets the minimum velocity delta that triggers a send. Default 0.1.</summary>
    public float VelocityThreshold { get; set; } = 0.1f;

    /// <summary>Gets or sets whether received velocity values are interpolated toward the target on non-owners. Default true.</summary>
    public bool InterpolateVelocity { get; set; } = true;

    /// <inheritdoc/>
    public override void Awake()
    {
        _identity = Entity.GetComponent<NetworkIdentity>()
            ?? throw new InvalidOperationException("NetworkPhysicsSync requires a NetworkIdentity on the same entity.");

        _rigidBody = Entity.GetComponent<RigidBody2D>()
            ?? throw new InvalidOperationException("NetworkPhysicsSync requires a RigidBody2D on the same entity.");

        _server = Entity.World.NetworkServer;
        _client = Entity.World.NetworkClient;

        if (_client is not null)
            _client.RegisterHandler<PhysicsSyncMessage>(OnPhysicsSyncReceived);
        if (_server is not null)
            _server.RegisterHandler<PhysicsSyncMessage>(OnPhysicsSyncReceivedFromClient);
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (_identity is null || _identity.NetworkId == 0 || _rigidBody is null) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_hasTarget && !_identity.IsOwner && InterpolateVelocity)
        {
            _rigidBody.LinearVelocity = Vector2.Lerp(_rigidBody.LinearVelocity, _targetVelocity, dt * 15f);
        }

        if (!_identity.IsOwner) return;

        _syncTimer += dt;
        if (_syncTimer < SyncInterval) return;
        _syncTimer = 0f;

        Vector2 vel = _rigidBody.LinearVelocity;
        float velLen = vel.Length();
        if (velLen < VelocityThreshold) return;

        _outMsg.NetworkId = _identity.NetworkId;
        _outMsg.LinearVelocity = vel;
        _outMsg.AngularVelocity = _rigidBody.AngularVelocity;
        _outMsg.Position = Entity.Transform.Position2d;
        _outMsg.Rotation = Entity.Transform.Rotation2d;

        if (_identity.IsServer && _server is not null)
            _server.Broadcast(ref _outMsg, NetworkChannel.Sequenced);
        else if (_client is not null)
            _client.Send(ref _outMsg, NetworkChannel.Sequenced);
    }

    private void OnPhysicsSyncReceived(PhysicsSyncMessage msg)
    {
        if (_identity is null || msg.NetworkId != _identity.NetworkId) return;
        if (_identity.IsOwner) return;

        if (InterpolateVelocity)
        {
            _targetVelocity = msg.LinearVelocity;
            _hasTarget = true;
        }
        else
        {
            _rigidBody!.LinearVelocity = msg.LinearVelocity;
            _rigidBody.AngularVelocity = msg.AngularVelocity;
        }

        Entity.Transform.Position2d = msg.Position;
        Entity.Transform.Rotation2d = msg.Rotation;
    }

    private void OnPhysicsSyncReceivedFromClient(int peerId, PhysicsSyncMessage msg)
    {
        if (_identity is null || msg.NetworkId != _identity.NetworkId) return;

        _rigidBody!.LinearVelocity = msg.LinearVelocity;
        _rigidBody.AngularVelocity = msg.AngularVelocity;
        Entity.Transform.Position2d = msg.Position;
        Entity.Transform.Rotation2d = msg.Rotation;

        if (_server is not null)
            _server.BroadcastExcept(peerId, ref msg, NetworkChannel.Sequenced);
    }
}
