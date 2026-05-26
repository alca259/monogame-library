namespace Alca.MonoGame.Kernel.Mathematics;

/// <summary>Factory methods and intersection tests for 3D bounding volumes.</summary>
public static class BoundingHelpers
{
    /// <summary>Creates a <see cref="BoundingSphere"/> at the given center with the given radius.</summary>
    public static BoundingSphere CreateBoundingSphere(Vector3 center, float radius) =>
        new(center, radius);

    /// <summary>Creates an axis-aligned <see cref="BoundingBox"/> from minimum and maximum corners.</summary>
    public static BoundingBox CreateBoundingBox(Vector3 min, Vector3 max) =>
        new(min, max);

    /// <summary>Tests a ray against a plane. Returns <see langword="true"/> and sets <paramref name="distance"/> if they intersect.</summary>
    public static bool RayIntersectsPlane(Ray ray, Plane plane, out float distance)
    {
        float denom = Vector3.Dot(plane.Normal, ray.Direction);

        if (MathF.Abs(denom) < 1e-6f)
        {
            distance = 0f;
            return false;
        }

        distance = -(Vector3.Dot(plane.Normal, ray.Position) + plane.D) / denom;
        return distance >= 0f;
    }

    /// <summary>Tests a ray against a sphere. Returns <see langword="true"/> and sets <paramref name="distance"/> if they intersect.</summary>
    public static bool RayIntersectsSphere(Ray ray, BoundingSphere sphere, out float distance)
    {
        float? d = ray.Intersects(sphere);
        distance = d ?? 0f;
        return d.HasValue;
    }

    /// <summary>Constructs a world-space picking ray from a screen-space position.</summary>
    public static Ray ScreenToWorldRay(Vector2 screenPos, Matrix view, Matrix projection, Viewport viewport)
    {
        Vector3 near = viewport.Unproject(new Vector3(screenPos, 0f), projection, view, Matrix.Identity);
        Vector3 far  = viewport.Unproject(new Vector3(screenPos, 1f), projection, view, Matrix.Identity);
        return new Ray(near, Vector3.Normalize(far - near));
    }
}
