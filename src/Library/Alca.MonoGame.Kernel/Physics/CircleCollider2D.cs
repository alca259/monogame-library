using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Circle-shaped 2D physics collider.</summary>
public sealed class CircleCollider2D : Collider2D
{
    /// <summary>Gets or sets the radius of the circle in world units. Default is 0.5.</summary>
    public float Radius { get; set; } = 0.5f;

    /// <summary>Gets or sets the local offset of the circle center from the entity's origin.</summary>
    public Vector2 Offset { get; set; }

    /// <inheritdoc/>
    protected override Fixture CreateFixture(Body body)
        => body.CreateCircle(Radius, Density, Offset);
}
