using Alca.MonoGame.Kernel.Scenes.Transitions;

namespace Alca.MonoGame.Kernel.UnitTests.Scenes.Transitions;

/// <summary>Unit tests for <see cref="SlideTransition"/> state-machine logic (no GPU required).</summary>
public sealed class SlideTransitionTests
{
    private static SlideTransition MakeSut(SlideDirection direction = SlideDirection.Left)
        => new SlideTransition(null!, direction);

    [Fact]
    public void Initial_IsTransitionOutComplete_IsFalse()
    {
        SlideTransition sut = MakeSut();
        Assert.False(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void Initial_IsTransitionInComplete_IsFalse()
    {
        SlideTransition sut = MakeSut();
        Assert.False(sut.IsTransitionInComplete);
    }

    [Fact]
    public void BeginTransitionOut_Update_FullDuration_IsComplete()
    {
        SlideTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(1f);

        Assert.True(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void BeginTransitionIn_Update_FullDuration_IsComplete()
    {
        SlideTransition sut = MakeSut();
        sut.BeginTransitionIn(1f);
        sut.Update(1f);

        Assert.True(sut.IsTransitionInComplete);
    }

    [Fact]
    public void Reset_AfterComplete_ResetsState()
    {
        SlideTransition sut = MakeSut();
        sut.BeginTransitionOut(0.1f);
        sut.Update(1f);

        sut.Reset();

        Assert.False(sut.IsTransitionOutComplete);
    }

    [Theory]
    [InlineData(SlideDirection.Left)]
    [InlineData(SlideDirection.Right)]
    [InlineData(SlideDirection.Up)]
    [InlineData(SlideDirection.Down)]
    public void AllDirections_CanCompleteTransitionOut(SlideDirection direction)
    {
        SlideTransition sut = MakeSut(direction);
        sut.BeginTransitionOut(1f);
        sut.Update(1f);

        Assert.True(sut.IsTransitionOutComplete);
    }

    [Fact]
    public void BeginTransitionOut_PartialUpdate_NotComplete()
    {
        SlideTransition sut = MakeSut();
        sut.BeginTransitionOut(1f);
        sut.Update(0.4f);

        Assert.False(sut.IsTransitionOutComplete);
    }
}
