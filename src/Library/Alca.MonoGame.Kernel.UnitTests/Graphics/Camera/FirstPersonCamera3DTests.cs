namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class FirstPersonCamera3DTests
{
    [Fact]
    public void Constructor_DefaultYawAndPitchAreZero()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Assert.Equal(0f, cam.Yaw);
        Assert.Equal(0f, cam.Pitch);
    }

    [Fact]
    public void Look_AppliesYawDelta()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        cam.Look(1f, 0f);
        Assert.Equal(1f, cam.Yaw, 0.001f);
    }

    [Fact]
    public void Look_AccumulatesMultipleYawDeltas()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        cam.Look(0.5f, 0f);
        cam.Look(0.5f, 0f);
        Assert.Equal(1f, cam.Yaw, 0.001f);
    }

    [Fact]
    public void Pitch_ClampedAtPositiveMax()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        cam.Look(0f, 999f);
        Assert.Equal(MathHelper.ToRadians(89f), cam.Pitch, 0.001f);
    }

    [Fact]
    public void Pitch_ClampedAtNegativeMax()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        cam.Look(0f, -999f);
        Assert.Equal(MathHelper.ToRadians(-89f), cam.Pitch, 0.001f);
    }

    [Fact]
    public void MoveForward_ChangesPosition()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 before = cam.Position;
        cam.MoveForward(10f);
        Assert.NotEqual(before, cam.Position);
    }

    [Fact]
    public void Strafe_ChangesPosition()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 before = cam.Position;
        cam.Strafe(10f);
        Assert.NotEqual(before, cam.Position);
    }

    [Fact]
    public void SetPosition_UpdatesPosition()
    {
        FirstPersonCamera3D cam = new(Vector3.Zero, 60f, 1.333f);
        Vector3 newPos = new(50f, 10f, 0f);
        cam.SetPosition(newPos);
        Assert.Equal(newPos, cam.Position);
    }

    [Fact]
    public void View_ChangesAfterLook()
    {
        FirstPersonCamera3D cam = new(new Vector3(0f, 0f, 100f), 60f, 1.333f);
        Matrix viewBefore = cam.View;
        cam.Look(1f, 0f);
        Assert.NotEqual(viewBefore, cam.View);
    }
}
