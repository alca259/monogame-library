namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class FixedCamera3DTests
{
    [Fact]
    public void Constructor_SetsPosition()
    {
        Vector3 pos = new(0f, 100f, 500f);
        FixedCamera3D cam = new(pos, Vector3.Zero, 45f, 1.333f);
        Assert.Equal(pos, cam.Position);
    }

    [Fact]
    public void Constructor_SetsTarget()
    {
        Vector3 target = new(10f, 0f, 0f);
        FixedCamera3D cam = new(Vector3.Zero, target, 45f, 1.333f);
        Assert.Equal(target, cam.Target);
    }

    [Fact]
    public void View_IsNotIdentity_WhenPositionIsOffOrigin()
    {
        FixedCamera3D cam = new(new Vector3(0f, 10f, 100f), Vector3.Zero, 45f, 1.333f);
        Assert.NotEqual(Matrix.Identity, cam.View);
    }

    [Fact]
    public void GetFrustum_ReturnsSameInstance_WhenCalledTwice()
    {
        FixedCamera3D cam = new(new Vector3(0f, 10f, 100f), Vector3.Zero, 45f, 1.333f);
        BoundingFrustum first = cam.GetFrustum();
        BoundingFrustum second = cam.GetFrustum();
        Assert.Same(first, second);
    }

    [Fact]
    public void GetFrustum_ReturnsSameInstance_AfterInvalidation()
    {
        FixedCamera3D cam = new(new Vector3(0f, 10f, 100f), Vector3.Zero, 45f, 1.333f);
        BoundingFrustum before = cam.GetFrustum();
        cam.SetPositionAndTarget(new Vector3(0f, 20f, 200f), Vector3.Zero);
        BoundingFrustum after = cam.GetFrustum();
        Assert.Same(before, after);
    }

    [Fact]
    public void SetPositionAndTarget_UpdatesPosition()
    {
        FixedCamera3D cam = new(Vector3.Zero, Vector3.Forward, 45f, 1.333f);
        Vector3 newPos = new(100f, 200f, 300f);
        cam.SetPositionAndTarget(newPos, Vector3.Zero);
        Assert.Equal(newPos, cam.Position);
    }

    [Fact]
    public void SetPositionAndTarget_UpdatesView()
    {
        FixedCamera3D cam = new(Vector3.Zero, Vector3.Forward, 45f, 1.333f);
        Matrix viewBefore = cam.View;
        cam.SetPositionAndTarget(new Vector3(100f, 200f, 300f), Vector3.Zero);
        Assert.NotEqual(viewBefore, cam.View);
    }
}
