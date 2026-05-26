using Alca.MonoGame.Kernel.ECS;
using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Simulates 2D rigid body physics on the owning <see cref="GameEntity"/>.
/// Requires a <see cref="Physics2DWorld"/> to be set on the <see cref="ECS.GameWorld"/>
/// before this component is added to the entity.
/// </summary>
public sealed class RigidBody2D : GameBehaviour
{
    private Body _body = null!;
    private Physics2DWorld _physicsWorld = null!;
    private bool _isStatic;
    private float _mass = 1f;
    private bool _useGravity = true;
    private float _linearDamping;
    private float _angularDamping;

    #region Properties (pre-Awake configurable)

    /// <summary>Gets or sets a value indicating whether the body is static (immovable). Must be set before <see cref="GameBehaviour.Awake"/>.</summary>
    public bool IsStatic
    {
        get => _body is not null ? _body.BodyType == BodyType.Static : _isStatic;
        set
        {
            _isStatic = value;
            if (_body is not null) _body.BodyType = value ? BodyType.Static : BodyType.Dynamic;
        }
    }

    /// <summary>Gets or sets the mass of the body in kilograms.</summary>
    public float Mass
    {
        get => _body is not null ? _body.Mass : _mass;
        set
        {
            _mass = value;
            if (_body is not null) _body.Mass = value;
        }
    }

    #endregion

    #region Runtime properties (require Awake to have run)

    /// <summary>Gets or sets the linear velocity of the body in units per second.</summary>
    public Vector2 LinearVelocity
    {
        get => _body?.LinearVelocity ?? Vector2.Zero;
        set { if (_body is not null) _body.LinearVelocity = value; }
    }

    /// <summary>Gets or sets the angular velocity of the body in radians per second.</summary>
    public float AngularVelocity
    {
        get => _body?.AngularVelocity ?? 0f;
        set { if (_body is not null) _body.AngularVelocity = value; }
    }

    /// <summary>Gets or sets the linear damping coefficient (0 = no drag).</summary>
    public float LinearDamping
    {
        get => _body is not null ? _body.LinearDamping : _linearDamping;
        set
        {
            _linearDamping = value;
            if (_body is not null) _body.LinearDamping = value;
        }
    }

    /// <summary>Gets or sets the angular damping coefficient (0 = no rotational drag).</summary>
    public float AngularDamping
    {
        get => _body is not null ? _body.AngularDamping : _angularDamping;
        set
        {
            _angularDamping = value;
            if (_body is not null) _body.AngularDamping = value;
        }
    }

    /// <summary>Gets or sets a value indicating whether gravity affects this body. Default true.</summary>
    public bool UseGravity
    {
        get => _body is not null ? !_body.IgnoreGravity : _useGravity;
        set
        {
            _useGravity = value;
            if (_body is not null) _body.IgnoreGravity = !value;
        }
    }

    #endregion

    /// <summary>Gets the underlying Aether physics body. Available after <see cref="GameBehaviour.Awake"/>.</summary>
    internal Body AetherBody => _body;

    /// <inheritdoc/>
    public override void Awake()
    {
        _physicsWorld = Entity.World.PhysicsWorld
            ?? throw new InvalidOperationException(
                "RigidBody2D requires a Physics2DWorld registered on the owning GameWorld. " +
                "Set GameWorld.PhysicsWorld before adding this component.");

        var bodyType = _isStatic ? BodyType.Static : BodyType.Dynamic;
        _body = _physicsWorld.AetherWorld.CreateBody(Entity.Transform.Position2d, Entity.Transform.Rotation2d, bodyType);
        _body.Mass = _mass;
        _body.LinearDamping = _linearDamping;
        _body.AngularDamping = _angularDamping;
        _body.IgnoreGravity = !_useGravity;
        _body.Tag = Entity;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        // Sync physics body result → transform each frame.
        Entity.Transform.Position2d = _body.Position;
        Entity.Transform.Rotation2d = _body.Rotation;
    }

    /// <summary>Applies a continuous force to the body's center of mass.</summary>
    public void ApplyForce(Vector2 force) => _body.ApplyForce(force);

    /// <summary>Applies an instantaneous linear impulse to the body's center of mass, immediately changing velocity.</summary>
    public void ApplyImpulse(Vector2 impulse) => _body.ApplyLinearImpulse(ref impulse);

    /// <summary>Applies a torque to the body, causing angular acceleration.</summary>
    public void ApplyTorque(float torque) => _body.ApplyTorque(torque);

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        if (_body is not null)
            _physicsWorld?.AetherWorld.Remove(_body);
    }
}
