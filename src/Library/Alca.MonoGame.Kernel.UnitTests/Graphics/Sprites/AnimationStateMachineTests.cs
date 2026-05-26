using Alca.MonoGame.Kernel.Graphics.Sprites;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Sprites;

[Collection(GraphicsCollection.Name)]
public sealed class AnimationStateMachineTests
{
    private readonly GraphicsDeviceFixture _fx;

    public AnimationStateMachineTests(GraphicsDeviceFixture fx) => _fx = fx;

    private Animation MakeAnimation(int frameCount = 2, int delayMs = 100)
    {
        Texture2D texture = new(_fx.GraphicsDevice, frameCount * 16, 16);
        List<TextureRegion> frames = new(frameCount);
        for (int i = 0; i < frameCount; i++)
            frames.Add(new TextureRegion(texture, i * 16, 0, 16, 16));
        return new Animation(frames, TimeSpan.FromMilliseconds(delayMs));
    }

    private static GameTime Tick(double ms) => new(TimeSpan.Zero, TimeSpan.FromMilliseconds(ms));

    [Fact]
    public void Play_SwitchesCurrentState()
    {
        AnimationStateMachine sm = new();
        sm.Register("idle", MakeAnimation());
        sm.Register("walk", MakeAnimation());

        sm.Play("idle");
        Assert.Equal("idle", sm.CurrentState);

        sm.Play("walk");
        Assert.Equal("walk", sm.CurrentState);
    }

    [Fact]
    public void Play_SameState_DoesNotThrowAndKeepsCurrentState()
    {
        AnimationStateMachine sm = new();
        sm.Register("idle", MakeAnimation());

        sm.Play("idle");
        sm.Play("idle"); // no-op

        Assert.Equal("idle", sm.CurrentState);
    }

    [Fact]
    public void Register_DuplicateName_ThrowsArgumentException()
    {
        AnimationStateMachine sm = new();
        sm.Register("idle", MakeAnimation());

        ArgumentException ex = Assert.Throws<ArgumentException>(() => sm.Register("idle", MakeAnimation()));
        Assert.Contains("idle", ex.Message);
    }

    [Fact]
    public void Play_UnknownName_ThrowsKeyNotFoundException()
    {
        AnimationStateMachine sm = new();
        Assert.Throws<KeyNotFoundException>(() => sm.Play("ghost"));
    }

    [Fact]
    public void Update_WithNoCurrentState_DoesNotThrow()
    {
        AnimationStateMachine sm = new();
        Exception? ex = Record.Exception(() => sm.Update(Tick(100)));
        Assert.Null(ex);
    }

    [Fact]
    public void Unregister_ExistingState_RemovesIt()
    {
        AnimationStateMachine sm = new();
        sm.Register("idle", MakeAnimation());
        sm.Unregister("idle");

        Assert.Throws<KeyNotFoundException>(() => sm.Play("idle"));
    }

    [Fact]
    public void Unregister_NonExistentState_DoesNotThrow()
    {
        AnimationStateMachine sm = new();
        Exception? ex = Record.Exception(() => sm.Unregister("ghost"));
        Assert.Null(ex);
    }

    [Fact]
    public void CurrentState_IsNullBeforeFirstPlay()
    {
        AnimationStateMachine sm = new();
        Assert.Null(sm.CurrentState);
    }
}
