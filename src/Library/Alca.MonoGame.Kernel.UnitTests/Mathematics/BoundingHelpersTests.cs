namespace Alca.MonoGame.Kernel.UnitTests.Mathematics;

public sealed class BoundingHelpersTests
{
    [Fact]
    public void CreateBoundingSphere_SetsCorrectCenterAndRadius()
    {
        Vector3 center = new(1f, 2f, 3f);
        BoundingSphere sphere = BoundingHelpers.CreateBoundingSphere(center, 5f);
        Assert.Equal(center, sphere.Center);
        Assert.Equal(5f, sphere.Radius);
    }

    [Fact]
    public void CreateBoundingBox_SetsCorrectMinMax()
    {
        Vector3 min = new(-1f, -2f, -3f);
        Vector3 max = new(1f, 2f, 3f);
        BoundingBox box = BoundingHelpers.CreateBoundingBox(min, max);
        Assert.Equal(min, box.Min);
        Assert.Equal(max, box.Max);
    }

    [Fact]
    public void RayIntersectsPlane_ParallelRay_ReturnsFalse()
    {
        // Ray going along X, plane is XZ (normal Y), so they're parallel
        Ray ray = new(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f));
        Plane plane = new(Vector3.UnitY, 0f);
        bool intersects = BoundingHelpers.RayIntersectsPlane(ray, plane, out _);
        Assert.False(intersects);
    }

    [Fact]
    public void RayIntersectsPlane_PerpendicularRay_ReturnsTrue()
    {
        // Ray going down Y from height 5, hitting XZ plane (Y=0)
        Ray ray = new(new Vector3(0f, 5f, 0f), new Vector3(0f, -1f, 0f));
        Plane plane = new(Vector3.UnitY, 0f);
        bool intersects = BoundingHelpers.RayIntersectsPlane(ray, plane, out float distance);
        Assert.True(intersects);
        Assert.Equal(5f, distance, 0.001f);
    }

    [Fact]
    public void RayIntersectsSphere_RayMissing_ReturnsFalse()
    {
        Ray ray = new(new Vector3(10f, 10f, 0f), new Vector3(1f, 0f, 0f));
        BoundingSphere sphere = new(Vector3.Zero, 1f);
        bool intersects = BoundingHelpers.RayIntersectsSphere(ray, sphere, out _);
        Assert.False(intersects);
    }

    [Fact]
    public void RayIntersectsSphere_RayHitting_ReturnsTrue()
    {
        // Ray along -Z toward origin, sphere at origin r=1
        Ray ray = new(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -1f));
        BoundingSphere sphere = new(Vector3.Zero, 1f);
        bool intersects = BoundingHelpers.RayIntersectsSphere(ray, sphere, out float distance);
        Assert.True(intersects);
        Assert.Equal(9f, distance, 0.001f);
    }
}
