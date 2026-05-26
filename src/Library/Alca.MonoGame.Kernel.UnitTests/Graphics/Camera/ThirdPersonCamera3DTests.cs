namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class ThirdPersonCamera3DTests
{
    [Fact]
    public void Constructor_InitializesPositionAtOffset()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 expected = Vector3.Zero + cam.Offset;
        Assert.Equal(expected.X, cam.Position.X, 0.001f);
        Assert.Equal(expected.Y, cam.Position.Y, 0.001f);
        Assert.Equal(expected.Z, cam.Position.Z, 0.001f);
    }

    [Fact]
    public void Constructor_SetsTargetToInitialPosition()
    {
        Vector3 startTarget = new(10f, 0f, 5f);
        ThirdPersonCamera3D cam = new(startTarget, 60f, 1.333f);
        Assert.Equal(startTarget, cam.Target);
    }

    [Fact]
    public void Update_ElapsedZero_PositionDoesNotChange()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 posBefore = cam.Position;
        cam.Update(new Vector3(1000f, 0f, 0f), 0f, 0f);
        Assert.Equal(posBefore.X, cam.Position.X, 0.001f);
        Assert.Equal(posBefore.Y, cam.Position.Y, 0.001f);
        Assert.Equal(posBefore.Z, cam.Position.Z, 0.001f);
    }

    [Fact]
    public void Update_UpdatesTarget()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 newTarget = new(100f, 0f, 0f);
        cam.Update(newTarget, 0f, 0.016f);
        Assert.Equal(newTarget, cam.Target);
    }

    [Fact]
    public void Update_SpringConvergesOverTime()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 newTarget = new(1000f, 0f, 0f);
        Vector3 desired = newTarget + cam.Offset;

        for (int i = 0; i < 300; i++)
            cam.Update(newTarget, 0f, 0.016f);

        Assert.True(Math.Abs(cam.Position.X - desired.X) < 10f);
        Assert.True(Math.Abs(cam.Position.Y - desired.Y) < 10f);
    }

    [Fact]
    public void SnapTo_InstantlyMovesToDesiredPosition()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 newTarget = new(100f, 0f, 0f);
        cam.SnapTo(newTarget, 0f);
        Vector3 expected = newTarget + cam.Offset;
        Assert.Equal(expected.X, cam.Position.X, 0.001f);
        Assert.Equal(expected.Y, cam.Position.Y, 0.001f);
        Assert.Equal(expected.Z, cam.Position.Z, 0.001f);
    }

    [Fact]
    public void SnapTo_ResetsVelocity()
    {
        ThirdPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        // Build up velocity with a few frames
        for (int i = 0; i < 10; i++)
            cam.Update(new Vector3(1000f, 0f, 0f), 0f, 0.016f);

        cam.SnapTo(Vector3.Zero, 0f);
        Vector3 posAfterSnap = cam.Position;
        cam.Update(Vector3.Zero, 0f, 0f);
        Assert.Equal(posAfterSnap.X, cam.Position.X, 0.001f);
    }
}
