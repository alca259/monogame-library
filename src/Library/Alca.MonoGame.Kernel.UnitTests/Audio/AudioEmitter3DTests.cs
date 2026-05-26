using Alca.MonoGame.Kernel.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class AudioEmitter3DTests
{
    [Fact]
    public void DefaultPosition_IsZero()
    {
        AudioEmitter3D emitter = new();
        Assert.Equal(Vector3.Zero, emitter.Position);
    }

    [Fact]
    public void DefaultForward_IsForwardVector()
    {
        AudioEmitter3D emitter = new();
        Assert.Equal(Vector3.Forward, emitter.Forward);
    }

    [Fact]
    public void DefaultVelocity_IsZero()
    {
        AudioEmitter3D emitter = new();
        Assert.Equal(Vector3.Zero, emitter.Velocity);
    }

    [Fact]
    public void Position_CanBeSet()
    {
        AudioEmitter3D emitter = new();
        Vector3 expected = new(4f, 0f, -2f);
        emitter.Position = expected;
        Assert.Equal(expected, emitter.Position);
    }

    [Fact]
    public void Forward_CanBeSet()
    {
        AudioEmitter3D emitter = new();
        emitter.Forward = Vector3.Right;
        Assert.Equal(Vector3.Right, emitter.Forward);
    }

    [Fact]
    public void Velocity_CanBeSet()
    {
        AudioEmitter3D emitter = new();
        Vector3 expected = new(-1f, 0f, 3f);
        emitter.Velocity = expected;
        Assert.Equal(expected, emitter.Velocity);
    }
}
