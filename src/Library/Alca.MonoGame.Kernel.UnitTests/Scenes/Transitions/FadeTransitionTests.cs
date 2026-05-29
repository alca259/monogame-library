using Alca.MonoGame.Kernel.Scenes.Transitions;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes.Transitions;

/// <summary>Unit tests for <see cref="FadeTransition"/> state-machine logic (no GPU required).</summary>
public sealed class FadeTransitionTests
{
    // We pass null! for the Texture2D because Draw() is never called in these tests.
    private static FadeTransition MakeSut() => new FadeTransition(null!);

    [Fact]
    public void Initial_IsTransitionOutComplete_IsFalse()
    {
        FadeTransition sut = MakeSut();

        Assert.False(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void Initial_IsTransitionInComplete_IsFalse()
    {
        FadeTransition sut = MakeSut();

        Assert.False(sut.IsTransitionInComplete);
    }

    [Fact]
    public void BeginTransitionOut_ThenUpdate_FullDuration_IsComplete()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(1f);

        Assert.True(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void BeginTransitionOut_ThenUpdate_HalfDuration_NotComplete()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(0.5f);

        Assert.False(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void BeginTransitionIn_ThenUpdate_FullDuration_IsComplete()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionIn(1f);
        sut.Update(1f);

        Assert.True(sut.IsTransitionInComplete);
    }

    [Fact]
    public void BeginTransitionIn_ThenUpdate_HalfDuration_NotComplete()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionIn(1f);
        sut.Update(0.5f);

        Assert.False(sut.IsTransitionInComplete);
    }

    [Fact]
    public void Reset_AfterCompletedOut_ClearsComplete()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionOut(0.1f);
        sut.Update(1f); // complete

        sut.Reset();

        Assert.False(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void BeginTransitionOut_ThenBeginTransitionIn_InPhaseIsActive()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(1f); // out complete

        sut.BeginTransitionIn(1f);
        sut.Update(1f); // in complete

        Assert.True(sut.IsTransitionInComplete);
    }

    [Fact]
    public void Update_Beyond_FullDuration_DoesNotExceedCompletion()
    {
        FadeTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(100f); // way beyond duration

        Assert.True(sut.IsTransitionOutComplete);
    }
}
