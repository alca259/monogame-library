using Alca.MonoGame.Kernel.ECS;
using AetherJoint = nkast.Aether.Physics2D.Dynamics.Joints.Joint;
using AetherBody = nkast.Aether.Physics2D.Dynamics.Body;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Base class for 2D physics joints that constrain the motion between two rigid bodies.
/// Requires a <see cref="RigidBody2D"/> on the same entity.
/// Set <see cref="ConnectedBody"/> before adding to a <see cref="GameEntity"/>.
/// </summary>
public abstract class Joint2D : GameBehaviour
{
    private AetherJoint? _aetherJoint;
    private Physics2DWorld _physicsWorld = null!;

    /// <summary>Gets or sets the second rigid body to connect to. Null means "anchor to world".</summary>
    public RigidBody2D? ConnectedBody { get; set; }

    /// <summary>Gets or sets a value indicating whether the connected bodies can collide with each other.</summary>
    public bool EnableCollision { get; set; }

    /// <summary>Gets the underlying Aether joint. Available after <see cref="GameBehaviour.Awake"/>.</summary>
    protected AetherJoint? AetherJoint => _aetherJoint;

    /// <inheritdoc/>
    public override void Awake()
    {
        _physicsWorld = Entity.World.PhysicsWorld
            ?? throw new InvalidOperationException(
                "Joint2D requires a Physics2DWorld on the owning GameWorld.");

        var bodyA = Entity.GetComponent<RigidBody2D>()?.AetherBody
            ?? throw new InvalidOperationException(
                "Joint2D requires a RigidBody2D on the same entity.");

        var bodyB = ConnectedBody?.AetherBody;

        _aetherJoint = CreateJoint(_physicsWorld, bodyA, bodyB);
        _aetherJoint.CollideConnected = EnableCollision;
    }

    /// <summary>Creates the Aether joint. Return the created joint.</summary>
    protected abstract AetherJoint CreateJoint(Physics2DWorld physicsWorld, AetherBody bodyA, AetherBody? bodyB);

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        if (_aetherJoint is not null)
            _physicsWorld?.AetherWorld.Remove(_aetherJoint);
    }
}
