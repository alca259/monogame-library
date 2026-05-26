using nkast.Aether.Physics2D.Collision;
using nkast.Aether.Physics2D.Dynamics;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>
/// Provides allocation-efficient 2D physics queries (raycasts and overlap tests) against a
/// <see cref="Physics2DWorld"/>. Obtain an instance from <see cref="Physics2DWorld.Query"/>.
/// </summary>
public sealed class Physics2DQuery
{
    private readonly World _world;

    internal Physics2DQuery(Physics2DWorld physicsWorld)
    {
        _world = physicsWorld.AetherWorld;
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Casts a ray and returns the closest hit that matches the collision <paramref name="mask"/>.
    /// </summary>
    /// <param name="origin">Ray start position in world space.</param>
    /// <param name="direction">Normalized ray direction.</param>
    /// <param name="maxDistance">Maximum ray length in world units.</param>
    /// <param name="mask">Only fixtures whose <see cref="Collider2D.Layer"/> overlaps this mask are tested.</param>
    /// <param name="hit">The closest hit result, or a default (non-hit) struct when the method returns <c>false</c>.</param>
    /// <returns><c>true</c> if the ray hit at least one fixture.</returns>
    public bool Raycast(Vector2 origin, Vector2 direction, float maxDistance, CollisionCategory mask, out RaycastHit2D hit)
    {
        Vector2 point2 = origin + direction * maxDistance;

        float closestFraction = float.MaxValue;
        Vector2 closestPoint = Vector2.Zero;
        Vector2 closestNormal = Vector2.Zero;
        Collider2D? closestCollider = null;

        _world.RayCast((fixture, point, normal, fraction) =>
        {
            if (((int)fixture.CollisionCategories & (ushort)mask) == 0)
                return -1f;

            if (fraction < closestFraction)
            {
                closestFraction = fraction;
                closestPoint = point;
                closestNormal = normal;
                closestCollider = fixture.Tag as Collider2D;
            }

            return fraction;
        }, origin, point2);

        if (closestCollider is null)
        {
            hit = default;
            return false;
        }

        hit = new RaycastHit2D
        {
            Point = closestPoint,
            Normal = closestNormal,
            Distance = closestFraction * maxDistance,
            Collider = closestCollider,
        };
        return true;
    }

    /// <summary>
    /// Casts a ray and collects all hits that match the collision <paramref name="mask"/> into <paramref name="results"/>.
    /// Results are unsorted. Caller is responsible for clearing <paramref name="results"/> before the call if desired.
    /// </summary>
    public void RaycastAll(Vector2 origin, Vector2 direction, float maxDistance, CollisionCategory mask, List<RaycastHit2D> results)
    {
        Vector2 point2 = origin + direction * maxDistance;

        _world.RayCast((fixture, point, normal, fraction) =>
        {
            if (((int)fixture.CollisionCategories & (ushort)mask) == 0)
                return -1f;

            var collider = fixture.Tag as Collider2D;
            results.Add(new RaycastHit2D
            {
                Point = point,
                Normal = normal,
                Distance = fraction * maxDistance,
                Collider = collider,
            });

            return 1f;
        }, origin, point2);
    }

    // ── Overlap queries ───────────────────────────────────────────────────────

    /// <summary>
    /// Tests whether any fixture at the given world-space <paramref name="point"/> matches <paramref name="mask"/>.
    /// </summary>
    /// <param name="point">World-space point to test.</param>
    /// <param name="mask">Category filter.</param>
    /// <param name="collider">The first matching collider found, or <c>null</c>.</param>
    /// <returns><c>true</c> if at least one matching fixture contains the point.</returns>
    public bool OverlapPoint(Vector2 point, CollisionCategory mask, out Collider2D? collider)
    {
        var aabb = new AABB { LowerBound = point, UpperBound = point };
        Collider2D? found = null;

        _world.QueryAABB(fixture =>
        {
            if (((int)fixture.CollisionCategories & (ushort)mask) == 0)
                return true;

            if (!fixture.TestPoint(ref point))
                return true;

            found = fixture.Tag as Collider2D;
            return false;
        }, ref aabb);

        collider = found;
        return found is not null;
    }

    /// <summary>
    /// Finds all colliders whose fixtures overlap the specified circle.
    /// Caller is responsible for clearing <paramref name="results"/> before the call if desired.
    /// </summary>
    /// <param name="center">Circle center in world space.</param>
    /// <param name="radius">Circle radius in world units.</param>
    /// <param name="mask">Category filter.</param>
    /// <param name="results">Pre-allocated list to receive the matching colliders.</param>
    public void OverlapCircle(Vector2 center, float radius, CollisionCategory mask, List<Collider2D> results)
    {
        var aabb = new AABB
        {
            LowerBound = center - new Vector2(radius),
            UpperBound = center + new Vector2(radius),
        };

        float radiusSq = radius * radius;

        _world.QueryAABB(fixture =>
        {
            if (((int)fixture.CollisionCategories & (ushort)mask) == 0)
                return true;

            fixture.GetAABB(out var fixtureAabb, 0);
            Vector2 clampedX = new(
                Math.Clamp(center.X, fixtureAabb.LowerBound.X, fixtureAabb.UpperBound.X),
                Math.Clamp(center.Y, fixtureAabb.LowerBound.Y, fixtureAabb.UpperBound.Y));
            Vector2 diff = center - clampedX;

            if (diff.X * diff.X + diff.Y * diff.Y <= radiusSq)
            {
                var c = fixture.Tag as Collider2D;
                if (c is not null)
                    results.Add(c);
            }

            return true;
        }, ref aabb);
    }

    /// <summary>
    /// Finds all colliders whose fixtures overlap the specified oriented box.
    /// Uses the AABB of the rotated box for the broad-phase test.
    /// Caller is responsible for clearing <paramref name="results"/> before the call if desired.
    /// </summary>
    /// <param name="center">Box center in world space.</param>
    /// <param name="halfSize">Half-extents of the box (width/2, height/2) in world units.</param>
    /// <param name="angle">Rotation angle of the box in radians.</param>
    /// <param name="mask">Category filter.</param>
    /// <param name="results">Pre-allocated list to receive the matching colliders.</param>
    public void OverlapBox(Vector2 center, Vector2 halfSize, float angle, CollisionCategory mask, List<Collider2D> results)
    {
        float cos = MathF.Abs(MathF.Cos(angle));
        float sin = MathF.Abs(MathF.Sin(angle));
        float extentX = halfSize.X * cos + halfSize.Y * sin;
        float extentY = halfSize.X * sin + halfSize.Y * cos;

        var aabb = new AABB
        {
            LowerBound = center - new Vector2(extentX, extentY),
            UpperBound = center + new Vector2(extentX, extentY),
        };

        _world.QueryAABB(fixture =>
        {
            if (((int)fixture.CollisionCategories & (ushort)mask) == 0)
                return true;

            var c = fixture.Tag as Collider2D;
            if (c is not null)
                results.Add(c);

            return true;
        }, ref aabb);
    }
}
