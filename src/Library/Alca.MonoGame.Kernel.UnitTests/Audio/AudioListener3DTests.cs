using Alca.MonoGame.Kernel.Audio;
using Microsoft.Xna.Framework.Audio;

namespace Alca.MonoGame.Kernel.UnitTests.Audio;

public sealed class AudioListener3DTests
{
    [Fact]
    public void DefaultPosition_IsZero()
    {
        AudioListener3D listener = new();
        Assert.Equal(Vector3.Zero, listener.Position);
    }

    [Fact]
    public void DefaultForward_IsForwardVector()
    {
        AudioListener3D listener = new();
        Assert.Equal(Vector3.Forward, listener.Forward);
    }

    [Fact]
    public void DefaultUp_IsUpVector()
    {
        AudioListener3D listener = new();
        Assert.Equal(Vector3.Up, listener.Up);
    }

    [Fact]
    public void DefaultVelocity_IsZero()
    {
        AudioListener3D listener = new();
        Assert.Equal(Vector3.Zero, listener.Velocity);
    }

    [Fact]
    public void Position_CanBeSet()
    {
        AudioListener3D listener = new();
        Vector3 expected = new(1f, 2f, 3f);
        listener.Position = expected;
        Assert.Equal(expected, listener.Position);
    }

    [Fact]
    public void Forward_CanBeSet()
    {
        AudioListener3D listener = new();
        Vector3 expected = new(0f, 0f, -1f);
        listener.Forward = expected;
        Assert.Equal(expected, listener.Forward);
    }

    [Fact]
    public void Up_CanBeSet()
    {
        AudioListener3D listener = new();
        listener.Up = Vector3.Right;
        Assert.Equal(Vector3.Right, listener.Up);
    }

    [Fact]
    public void Velocity_CanBeSet()
    {
        AudioListener3D listener = new();
        Vector3 expected = new(5f, 0f, 0f);
        listener.Velocity = expected;
        Assert.Equal(expected, listener.Velocity);
    }

    [Fact]
    public void Update_SetsPositionAndForward()
    {
        AudioListener3D listener = new();
        Vector3 pos = new(10f, 5f, 3f);
        Vector3 fwd = new(0f, 0f, -1f);

        listener.Update(pos, fwd);

        Assert.Equal(pos, listener.Position);
        Assert.Equal(fwd, listener.Forward);
    }

    [Fact]
    public void Update_DoesNotChangeUp()
    {
        AudioListener3D listener = new();
        Vector3 upBefore = listener.Up;

        listener.Update(new Vector3(1f, 0f, 0f), Vector3.Forward);

        Assert.Equal(upBefore, listener.Up);
    }
}
