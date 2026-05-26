namespace Alca.MonoGame.Kernel.Mathematics;

/// <summary>
/// Utility class for common geometric functions.
/// Equivalent to <c>UnityEngine.GeometryUtility</c>.
/// </summary>
public static class GeometryUtility
{
    /// <summary>
    /// Calculates the axis-aligned bounding box that encloses all <paramref name="positions"/>
    /// after applying the given <paramref name="transform"/> matrix.
    /// </summary>
    public static BoundingBox CalculateBounds(ReadOnlySpan<Vector3> positions, Matrix transform)
    {
        if (positions.IsEmpty)
            return new BoundingBox(Vector3.Zero, Vector3.Zero);

        var first = Vector3.Transform(positions[0], transform);
        var min = first;
        var max = first;

        for (int i = 1; i < positions.Length; i++)
        {
            var p = Vector3.Transform(positions[i], transform);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Extracts the six frustum planes from <paramref name="frustum"/> into <paramref name="planes"/>.
    /// <paramref name="planes"/> must have at least 6 elements.
    /// Order: Near, Far, Left, Right, Top, Bottom.
    /// </summary>
    public static void CalculateFrustumPlanes(BoundingFrustum frustum, Plane[] planes)
    {
        if (planes is null || planes.Length < 6)
            throw new ArgumentException("planes array must have at least 6 elements.", nameof(planes));

        planes[0] = frustum.Near;
        planes[1] = frustum.Far;
        planes[2] = frustum.Left;
        planes[3] = frustum.Right;
        planes[4] = frustum.Top;
        planes[5] = frustum.Bottom;
    }

    /// <summary>
    /// Returns true if <paramref name="bounds"/> is inside or intersects all planes in <paramref name="planes"/>.
    /// Useful for frustum culling: pass the planes from <see cref="CalculateFrustumPlanes"/>.
    /// </summary>
    public static bool TestPlanesAABB(ReadOnlySpan<Plane> planes, BoundingBox bounds)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            // Frustum planes are outward-facing; a box entirely on the Front side is outside.
            if (bounds.Intersects(planes[i]) == PlaneIntersectionType.Front)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Creates a <see cref="Plane"/> from the given polygon vertices.
    /// Returns false if the polygon is degenerate (collinear vertices or fewer than 3 points).
    /// </summary>
    public static bool TryCreatePlaneFromPolygon(ReadOnlySpan<Vector3> vertices, out Plane plane)
    {
        if (vertices.Length < 3)
        {
            plane = default;
            return false;
        }

        // Find two edge vectors and compute the normal via cross product
        for (int i = 0; i < vertices.Length - 2; i++)
        {
            var edge1 = vertices[i + 1] - vertices[i];
            var edge2 = vertices[i + 2] - vertices[i];
            var normal = Vector3.Cross(edge1, edge2);

            if (normal.LengthSquared() > float.Epsilon)
            {
                plane = new Plane(Vector3.Normalize(normal), -Vector3.Dot(Vector3.Normalize(normal), vertices[i]));
                return true;
            }
        }

        plane = default;
        return false;
    }
}
