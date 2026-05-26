namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class TopDownCamera3DTests
{
    [Fact]
    public void Constructor_SetsHeight()
    {
        TopDownCamera3D cam = new(500f, 45f, 1.333f);
        Assert.Equal(500f, cam.Height);
    }

    [Fact]
    public void Constructor_PositionYEqualsHeight()
    {
        TopDownCamera3D cam = new(300f, 45f, 1.333f);
        Assert.Equal(300f, cam.Position.Y, 0.001f);
    }

    [Fact]
    public void Height_Set_UpdatesPositionY()
    {
        TopDownCamera3D cam = new(500f, 45f, 1.333f);
        cam.Height = 1000f;
        Assert.Equal(1000f, cam.Position.Y, 0.001f);
    }

    [Fact]
    public void Height_Set_UpdatesView()
    {
        TopDownCamera3D cam = new(500f, 45f, 1.333f);
        Matrix viewBefore = cam.View;
        cam.Height = 2000f;
        Assert.NotEqual(viewBefore, cam.View);
    }

    [Fact]
    public void LookAt_PositionsXAboveTarget()
    {
        TopDownCamera3D cam = new(200f, 45f, 1.333f);
        cam.LookAt(new Vector3(50f, 0f, 50f));
        Assert.Equal(50f, cam.Position.X, 0.001f);
        Assert.Equal(200f, cam.Position.Y, 0.001f);
    }

    [Fact]
    public void LookAt_SetsTargetToWorldCenter()
    {
        TopDownCamera3D cam = new(200f, 45f, 1.333f);
        Vector3 center = new(30f, 0f, 40f);
        cam.LookAt(center);
        Assert.Equal(center, cam.Target);
    }

    [Fact]
    public void Follow_LerpFactor1_SnapsXToTarget()
    {
        TopDownCamera3D cam = new(500f, 45f, 1.333f);
        Vector3 target = new(100f, 0f, 100f);
        cam.Follow(target, 1f);
        Assert.Equal(target.X, cam.Position.X, 0.001f);
    }

    [Fact]
    public void Follow_LerpFactor0_StaysAtCurrentXZ()
    {
        TopDownCamera3D cam = new(500f, 45f, 1.333f);
        float initialX = cam.Position.X;
        float initialZ = cam.Position.Z;
        cam.Follow(new Vector3(999f, 0f, 999f), 0f);
        Assert.Equal(initialX, cam.Position.X, 0.001f);
        Assert.Equal(initialZ, cam.Position.Z, 0.001f);
    }

    [Fact]
    public void Follow_HeightRemainsSameAfterFollow()
    {
        TopDownCamera3D cam = new(750f, 45f, 1.333f);
        cam.Follow(new Vector3(100f, 50f, 100f), 0.5f);
        Assert.Equal(750f, cam.Position.Y, 0.001f);
    }
}
