using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Box-shaped 2D physics collider.</summary>
public sealed class BoxCollider2D : Collider2D
{
    /// <summary>Gets or sets the width and height of the box in world units. Default is (1, 1).</summary>
    public Vector2 Size { get; set; } = Vector2.One;

    /// <summary>Gets or sets the local offset of the box center from the entity's origin.</summary>
    public Vector2 Offset { get; set; }

    /// <inheritdoc/>
    protected override Fixture CreateFixture(Body body)
        => body.CreateRectangle(Size.X, Size.Y, Density, Offset);
}
