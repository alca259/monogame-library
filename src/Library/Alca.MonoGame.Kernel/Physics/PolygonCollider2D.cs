using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Polygon-shaped 2D physics collider. Set vertices with <see cref="SetPath"/> before adding to an entity.</summary>
public sealed class PolygonCollider2D : Collider2D
{
    private Vector2[] _vertices = [];

    /// <summary>
    /// Defines the polygon shape from the given vertex array.
    /// Must be called before the component is added to a <see cref="ECS.GameEntity"/>.
    /// Requires at least 3 vertices wound counter-clockwise.
    /// </summary>
    public void SetPath(ReadOnlySpan<Vector2> vertices)
    {
        _vertices = vertices.ToArray();
    }

    /// <inheritdoc/>
    protected override Fixture CreateFixture(Body body)
    {
        if (_vertices.Length < 3)
            throw new InvalidOperationException(
                "PolygonCollider2D requires at least 3 vertices. Call SetPath before adding the component to an entity.");

        var verts = new Vertices(_vertices.Length);
        for (int i = 0; i < _vertices.Length; i++)
            verts.Add(_vertices[i]);

        return body.CreatePolygon(verts, Density);
    }
}
