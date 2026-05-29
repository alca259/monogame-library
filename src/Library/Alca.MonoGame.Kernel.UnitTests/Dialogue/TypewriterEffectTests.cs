using Alca.MonoGame.Kernel.Dialogue;

namespace Alca.MonoGame.Kernel.UnitTests.Dialogue;

/// <summary>Unit tests for <see cref="TypewriterEffect"/>.</summary>
public sealed class TypewriterEffectTests
{
    [Fact]
    public void Initial_IsComplete_IsFalse()
    {
        TypewriterEffect sut = new TypewriterEffect();
        Assert.False(sut.IsComplete);
    }

    [Fact]
    public void Initial_CurrentText_IsEmpty()
    {
        TypewriterEffect sut = new TypewriterEffect();
        Assert.Equal(string.Empty, sut.CurrentText);
    }

    [Fact]
    public void SetText_ResetsCurrentText_ToEmpty()
    {
        TypewriterEffect sut = new TypewriterEffect();
        sut.SetText("hello");

        Assert.Equal(string.Empty, sut.CurrentText);
    }

    [Fact]
    public void SetText_IsComplete_IsFalse_WhenTextIsNonEmpty()
    {
        TypewriterEffect sut = new TypewriterEffect();
        sut.SetText("hello");

        Assert.False(sut.IsComplete);
    }

    [Fact]
    public void Advance_SufficientTime_RevealsAllCharacters()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 10f };
        sut.SetText("hi");

        sut.Advance(1f); // 10 chars/s * 1s = 10 chars → covers 2 chars

        Assert.Equal("hi", sut.CurrentText);
        Assert.True(sut.IsComplete);
    }

    [Fact]
    public void Advance_PartialTime_RevealsSubstring()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 10f };
        sut.SetText("hello");

        sut.Advance(0.1f); // 1 char

        Assert.Equal("h", sut.CurrentText);
    }

    [Fact]
    public void Advance_WhenComplete_DoesNotChangeText()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 100f };
        sut.SetText("hi");
        sut.Advance(1f); // complete

        string before = sut.CurrentText;
        sut.Advance(1f); // extra advance

        Assert.Equal(before, sut.CurrentText);
    }

    [Fact]
    public void CompleteInstantly_RevealsAllText()
    {
        TypewriterEffect sut = new TypewriterEffect();
        sut.SetText("hello world");

        sut.CompleteInstantly();

        Assert.Equal("hello world", sut.CurrentText);
        Assert.True(sut.IsComplete);
    }

    [Fact]
    public void CompleteInstantly_RaisesOnComplete()
    {
        TypewriterEffect sut = new TypewriterEffect();
        sut.SetText("test");
        bool raised = false;
        sut.OnComplete = () => raised = true;

        sut.CompleteInstantly();

        Assert.True(raised);
    }

    [Fact]
    public void CompleteInstantly_WhenAlreadyComplete_DoesNotFireOnCompleteAgain()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 100f };
        sut.SetText("x");
        int callCount = 0;
        sut.OnComplete = () => callCount++;
        sut.Advance(1f); // first complete → fires once

        sut.CompleteInstantly(); // already complete → should not fire again

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Advance_WhenComplete_RaisesOnComplete()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 10f };
        sut.SetText("ok");
        bool raised = false;
        sut.OnComplete = () => raised = true;

        sut.Advance(1f); // completes

        Assert.True(raised);
    }

    [Fact]
    public void Reset_ClearsCurrentTextAndCompletion()
    {
        TypewriterEffect sut = new TypewriterEffect { CharsPerSecond = 100f };
        sut.SetText("abc");
        sut.Advance(1f); // complete

        sut.Reset();

        Assert.Equal(string.Empty, sut.CurrentText);
        Assert.False(sut.IsComplete);
    }

    [Fact]
    public void SetText_TruncatesTextBeyondMaxCapacity()
    {
        TypewriterEffect sut = new TypewriterEffect(maxCapacity: 3);
        sut.SetText("hello"); // 5 chars but capacity is 3

        sut.CompleteInstantly();

        Assert.Equal("hel", sut.CurrentText);
    }
}
