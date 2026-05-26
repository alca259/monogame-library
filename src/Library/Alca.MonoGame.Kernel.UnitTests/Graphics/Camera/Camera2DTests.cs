namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class Camera2DTests
{
    private static Viewport MakeViewport(int w = 800, int h = 600) =>
        new(0, 0, w, h);

    [Fact]
    public void DefaultZoom_IsOne()
    {
        Camera2D cam = new();
        Assert.Equal(1f, cam.Zoom);
    }

    [Fact]
    public void Zoom_ClampedToMin()
    {
        Camera2D cam = new() { MinZoom = 0.5f };
        cam.Zoom = 0.01f;
        Assert.Equal(0.5f, cam.Zoom);
    }

    [Fact]
    public void Zoom_ClampedToMax()
    {
        Camera2D cam = new() { MaxZoom = 5f };
        cam.Zoom = 100f;
        Assert.Equal(5f, cam.Zoom);
    }

    [Fact]
    public void Position_SetAndGet_Roundtrips()
    {
        Camera2D cam = new();
        Vector2 expected = new(100f, 200f);
        cam.Position = expected;
        Assert.Equal(expected, cam.Position);
    }

    [Fact]
    public void Rotation_SetAndGet_Roundtrips()
    {
        Camera2D cam = new();
        cam.Rotation = 1.2f;
        Assert.Equal(1.2f, cam.Rotation);
    }

    [Fact]
    public void GetTransformMatrix_DefaultCamera_IsCenteredOnViewport()
    {
        Camera2D cam = new();
        Viewport vp = MakeViewport(800, 600);
        Matrix m = cam.GetTransformMatrix(vp);
        // Default camera (pos=0,0, zoom=1) should translate by half-viewport
        Vector3 translation = m.Translation;
        Assert.Equal(400f, translation.X, 0.001f);
        Assert.Equal(300f, translation.Y, 0.001f);
    }

    [Fact]
    public void WorldToScreen_ThenScreenToWorld_Roundtrips()
    {
        Camera2D cam = new();
        cam.Position = new Vector2(50f, 80f);
        Viewport vp = MakeViewport(800, 600);

        Vector2 world = new(120f, 240f);
        Vector2 screen = cam.WorldToScreen(world, vp);
        Vector2 backToWorld = cam.ScreenToWorld(screen, vp);

        Assert.Equal(world.X, backToWorld.X, 0.01f);
        Assert.Equal(world.Y, backToWorld.Y, 0.01f);
    }

    [Fact]
    public void Follow_LerpFactor1_SnapsToTarget()
    {
        Camera2D cam = new();
        cam.Position = Vector2.Zero;
        Vector2 target = new(100f, 200f);
        cam.Follow(target, 1f);
        Assert.Equal(target.X, cam.Position.X, 0.001f);
        Assert.Equal(target.Y, cam.Position.Y, 0.001f);
    }

    [Fact]
    public void Follow_LerpFactor0_StaysAtCurrent()
    {
        Camera2D cam = new();
        cam.Position = new Vector2(10f, 20f);
        cam.Follow(new Vector2(999f, 999f), 0f);
        Assert.Equal(10f, cam.Position.X, 0.001f);
        Assert.Equal(20f, cam.Position.Y, 0.001f);
    }

    [Fact]
    public void ClampToBounds_PositionOutsideBounds_GetsClampedToEdge()
    {
        Camera2D cam = new();
        cam.Position = new Vector2(5000f, 5000f);
        Rectangle bounds = new(0, 0, 1000, 800);
        cam.ClampToBounds(bounds);
        Assert.True(cam.Position.X <= bounds.Right);
        Assert.True(cam.Position.Y <= bounds.Bottom);
    }
}
