namespace Alca.MonoGame.Kernel.UnitTests.Mathematics;

public sealed class GeometryUtilityTests
{
    // ── CalculateBounds ────────────────────────────────────────────────────────

    [Fact]
    public void CalculateBounds_ReturnsEmptyBox_ForEmptySpan()
    {
        var result = GeometryUtility.CalculateBounds(ReadOnlySpan<Vector3>.Empty, Matrix.Identity);

        Assert.Equal(Vector3.Zero, result.Min);
        Assert.Equal(Vector3.Zero, result.Max);
    }

    [Fact]
    public void CalculateBounds_ReturnsCorrectAABB_WithIdentityMatrix()
    {
        ReadOnlySpan<Vector3> positions = [
            new Vector3(-1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 2, 0)
        ];

        var result = GeometryUtility.CalculateBounds(positions, Matrix.Identity);

        Assert.Equal(new Vector3(-1, -1, 0), result.Min);
        Assert.Equal(new Vector3(1, 2, 0), result.Max);
    }

    [Fact]
    public void CalculateBounds_AppliesTranslationMatrix()
    {
        ReadOnlySpan<Vector3> positions = [new Vector3(0, 0, 0), new Vector3(1, 1, 0)];
        var translate = Matrix.CreateTranslation(10, 20, 0);

        var result = GeometryUtility.CalculateBounds(positions, translate);

        Assert.Equal(new Vector3(10, 20, 0), result.Min);
        Assert.Equal(new Vector3(11, 21, 0), result.Max);
    }

    // ── CalculateFrustumPlanes ─────────────────────────────────────────────────

    [Fact]
    public void CalculateFrustumPlanes_FillsSixPlanes()
    {
        var view = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);
        var frustum = new BoundingFrustum(view * proj);
        var planes = new Microsoft.Xna.Framework.Plane[6];

        GeometryUtility.CalculateFrustumPlanes(frustum, planes);

        for (int i = 0; i < 6; i++)
            Assert.NotEqual(default, planes[i]);
    }

    [Fact]
    public void CalculateFrustumPlanes_ThrowsWhenArrayTooSmall()
    {
        var view = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 100f);
        var frustum = new BoundingFrustum(view * proj);
        var planes = new Microsoft.Xna.Framework.Plane[5];

        Assert.Throws<ArgumentException>(() => GeometryUtility.CalculateFrustumPlanes(frustum, planes));
    }

    // ── TestPlanesAABB ─────────────────────────────────────────────────────────

    [Fact]
    public void TestPlanesAABB_ReturnsTrue_WhenBoundsInsideFrustum()
    {
        var view = Matrix.CreateLookAt(new Vector3(0, 0, 50), Vector3.Zero, Vector3.Up);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 1000f);
        var frustum = new BoundingFrustum(view * proj);
        var planes = new Microsoft.Xna.Framework.Plane[6];
        GeometryUtility.CalculateFrustumPlanes(frustum, planes);

        var bounds = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var result = GeometryUtility.TestPlanesAABB(planes, bounds);

        Assert.True(result);
    }

    [Fact]
    public void TestPlanesAABB_ReturnsFalse_WhenBoundsBehindFrustum()
    {
        var view = Matrix.CreateLookAt(new Vector3(0, 0, 50), Vector3.Zero, Vector3.Up);
        var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 0.1f, 10f);
        var frustum = new BoundingFrustum(view * proj);
        var planes = new Microsoft.Xna.Framework.Plane[6];
        GeometryUtility.CalculateFrustumPlanes(frustum, planes);

        // Place box far behind camera
        var bounds = new BoundingBox(new Vector3(-1, -1, 100), new Vector3(1, 1, 200));
        var result = GeometryUtility.TestPlanesAABB(planes, bounds);

        Assert.False(result);
    }

    // ── TryCreatePlaneFromPolygon ──────────────────────────────────────────────

    [Fact]
    public void TryCreatePlaneFromPolygon_ReturnsFalse_ForFewerThan3Vertices()
    {
        ReadOnlySpan<Vector3> vertices = [new Vector3(0, 0, 0), new Vector3(1, 0, 0)];

        bool result = GeometryUtility.TryCreatePlaneFromPolygon(vertices, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreatePlaneFromPolygon_ReturnsFalse_ForCollinearVertices()
    {
        ReadOnlySpan<Vector3> vertices = [
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0)
        ];

        bool result = GeometryUtility.TryCreatePlaneFromPolygon(vertices, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreatePlaneFromPolygon_ReturnsTrue_ForValidXYPlaneTriangle()
    {
        ReadOnlySpan<Vector3> vertices = [
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
        ];

        bool result = GeometryUtility.TryCreatePlaneFromPolygon(vertices, out var plane);

        Assert.True(result);
        // Normal should point along Z axis
        Assert.Equal(0f, plane.Normal.X, 3);
        Assert.Equal(0f, plane.Normal.Y, 3);
        Assert.Equal(1f, MathF.Abs(plane.Normal.Z), 3);
    }
}
