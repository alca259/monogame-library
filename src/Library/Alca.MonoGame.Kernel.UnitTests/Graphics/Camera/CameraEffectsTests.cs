namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Camera;

public sealed class CameraEffectsTests
{
    private static GameTime MakeGameTime(double totalSeconds, double deltaSeconds)
        => new(TimeSpan.FromSeconds(totalSeconds), TimeSpan.FromSeconds(deltaSeconds));

    [Fact]
    public void Shake_SetsIsShakingTrue()
    {
        var effects = new CameraEffects();
        var camera = new Camera2D();

        effects.Shake(camera, magnitude: 10f, duration: 1f);

        Assert.True(effects.IsShaking);
    }

    [Fact]
    public void Shake_WhenDurationExpires_IsShakingFalse()
    {
        var effects = new CameraEffects();
        var camera = new Camera2D();

        effects.Shake(camera, magnitude: 10f, duration: 0.5f);

        effects.Update(MakeGameTime(0.5, 0.5));
        effects.Update(MakeGameTime(1.0, 0.5));

        Assert.False(effects.IsShaking);
    }

    [Fact]
    public void Shake_OffsetIsNonZeroDuringShake()
    {
        var effects = new CameraEffects();
        var camera = new Camera2D { Position = Vector2.Zero };
        float originalX = camera.Position.X;
        float originalY = camera.Position.Y;

        effects.Shake(camera, magnitude: 20f, duration: 1f);
        effects.Update(MakeGameTime(0.016, 0.016));

        bool positionMoved = camera.Position.X != originalX || camera.Position.Y != originalY;
        Assert.True(positionMoved);
    }

    [Fact]
    public void Update_WhenNotShaking_DoesNotMutatePosition()
    {
        var effects = new CameraEffects();
        var camera = new Camera2D { Position = new Vector2(100f, 200f) };

        effects.Update(MakeGameTime(0.016, 0.016));

        Assert.Equal(100f, camera.Position.X, 5);
        Assert.Equal(200f, camera.Position.Y, 5);
    }

    [Fact]
    public void IsPanning_DefaultsFalse()
    {
        var effects = new CameraEffects();
        Assert.False(effects.IsPanning);
    }
}
