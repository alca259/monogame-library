using Alca.MonoGame.Kernel.ECS;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Base class for 2D physics colliders. Attach alongside a <see cref="RigidBody2D"/> on the same entity.
/// If no <see cref="RigidBody2D"/> is present, a static body is created implicitly.
/// Add <see cref="RigidBody2D"/> before adding colliders to ensure correct body ownership.
/// </summary>
public abstract class Collider2D : GameBehaviour
{
    private Fixture _fixture = null!;
    private Physics2DWorld _physicsWorld = null!;
    private bool _ownsBody;

    /// <summary>Gets or sets a value indicating whether this collider acts as a trigger (no physical response, events only).</summary>
    public bool IsTrigger { get; set; }

    /// <summary>Gets or sets the friction coefficient (0 = frictionless, 1 = very rough).</summary>
    public float Friction { get; set; } = 0.5f;

    /// <summary>Gets or sets the restitution (bounciness) coefficient (0 = no bounce, 1 = perfectly elastic).</summary>
    public float Restitution { get; set; }

    /// <summary>Gets or sets the shape density in kg/m².</summary>
    public float Density { get; set; } = 1f;

    /// <summary>Invoked when this collider begins touching another solid collider.</summary>
    public Action<Collider2D>? OnCollisionEnter { get; set; }

    /// <summary>Invoked when this collider stops touching another solid collider.</summary>
    public Action<Collider2D>? OnCollisionExit { get; set; }

    /// <summary>Invoked when another collider enters this trigger.</summary>
    public Action<Collider2D>? OnTriggerEnter { get; set; }

    /// <summary>Invoked when another collider exits this trigger.</summary>
    public Action<Collider2D>? OnTriggerExit { get; set; }

    /// <summary>Gets the underlying Aether fixture. Available after <see cref="GameBehaviour.Awake"/>.</summary>
    internal Fixture AetherFixture => _fixture;

    /// <inheritdoc/>
    public override void Awake()
    {
        _physicsWorld = Entity.World.PhysicsWorld
            ?? throw new InvalidOperationException(
                "Collider2D requires a Physics2DWorld on the owning GameWorld.");

        var rigidBody = Entity.GetComponent<RigidBody2D>();

        Body body;
        if (rigidBody is not null)
        {
            body = rigidBody.AetherBody;
        }
        else
        {
            body = _physicsWorld.AetherWorld.CreateBody(Entity.Transform.Position2d, Entity.Transform.Rotation2d, BodyType.Static);
            body.Tag = Entity;
            _ownsBody = true;
        }

        _fixture = CreateFixture(body);
        _fixture.IsSensor = IsTrigger;
        _fixture.Friction = Friction;
        _fixture.Restitution = Restitution;
        _fixture.Tag = this;

        _fixture.OnCollision += HandleCollision;
        _fixture.OnSeparation += HandleSeparation;
    }

    /// <summary>Creates the Aether fixture on the given body. Subclasses define the shape.</summary>
    protected abstract Fixture CreateFixture(Body body);

    private bool HandleCollision(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is Collider2D otherCollider)
        {
            if (IsTrigger || otherCollider.IsTrigger)
                OnTriggerEnter?.Invoke(otherCollider);
            else
                OnCollisionEnter?.Invoke(otherCollider);
        }

        return !IsTrigger;
    }

    private void HandleSeparation(Fixture sender, Fixture other, Contact contact)
    {
        if (other.Tag is Collider2D otherCollider)
        {
            if (IsTrigger || otherCollider.IsTrigger)
                OnTriggerExit?.Invoke(otherCollider);
            else
                OnCollisionExit?.Invoke(otherCollider);
        }
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        if (_fixture is not null)
        {
            _fixture.OnCollision -= HandleCollision;
            _fixture.OnSeparation -= HandleSeparation;

            if (_ownsBody)
                _physicsWorld?.AetherWorld.Remove(_fixture.Body);
        }
    }
}
