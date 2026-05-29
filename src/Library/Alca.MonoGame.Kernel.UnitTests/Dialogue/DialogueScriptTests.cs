using Alca.MonoGame.Kernel.Dialogue;

namespace Alca.MonoGame.Kernel.UnitTests.Dialogue;

/// <summary>Unit tests for <see cref="DialogueScript"/> and its nested <see cref="DialogueScript.Builder"/>.</summary>
public sealed class DialogueScriptTests
{
    private static DialogueLine MakeLine(string speaker = "NPC", string key = "greeting")
        => new DialogueLine(speaker, key, string.Empty, Array.Empty<DialogueChoice>());

    [Fact]
    public void Constructor_LineCount_MatchesInputArray()
    {
        DialogueLine[] lines = [MakeLine(), MakeLine()];
        DialogueScript sut = new DialogueScript(lines);

        Assert.Equal(2, sut.LineCount);
    }

    [Fact]
    public void Constructor_CopiesArray_ExternalMutationDoesNotAffectScript()
    {
        DialogueLine[] lines = [MakeLine("A", "k1")];
        DialogueScript sut = new DialogueScript(lines);
        lines[0] = MakeLine("Z", "kZ"); // mutate original

        Assert.Equal("A", sut.GetLine(0).SpeakerId);
    }

    [Fact]
    public void GetLine_ValidIndex_ReturnsCorrectLine()
    {
        DialogueScript sut = new DialogueScript([MakeLine("Alice", "hello")]);

        ref readonly DialogueLine line = ref sut.GetLine(0);

        Assert.Equal("Alice", line.SpeakerId);
        Assert.Equal("hello", line.LocalizationKey);
    }

    [Fact]
    public void GetLine_OutOfRange_ThrowsArgumentOutOfRange()
    {
        DialogueScript sut = new DialogueScript([MakeLine()]);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.GetLine(5));
    }

    [Fact]
    public void TryGetLine_ValidIndex_ReturnsTrueAndLine()
    {
        DialogueScript sut = new DialogueScript([MakeLine("Bob", "bye")]);

        bool found = sut.TryGetLine(0, out DialogueLine line);

        Assert.True(found);
        Assert.Equal("Bob", line.SpeakerId);
    }

    [Fact]
    public void TryGetLine_InvalidIndex_ReturnsFalse()
    {
        DialogueScript sut = new DialogueScript([MakeLine()]);

        bool found = sut.TryGetLine(99, out _);

        Assert.False(found);
    }

    [Fact]
    public void Builder_AddLine_BuildsCorrectLineCount()
    {
        DialogueScript sut = DialogueScript.Create()
            .AddLine("A", "k1")
            .AddLine("B", "k2")
            .AddLine("C", "k3")
            .Build();

        Assert.Equal(3, sut.LineCount);
    }

    [Fact]
    public void Builder_AddLine_WithChoices_BuildsChoicesCorrectly()
    {
        DialogueChoice choice = new DialogueChoice("Yes", 1);
        DialogueScript sut = DialogueScript.Create()
            .AddLine("NPC", "question", "", choice)
            .Build();

        Assert.True(sut.GetLine(0).HasChoices);
    }

    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        DialogueScript.Builder builder = DialogueScript.Create();
        Assert.NotNull(builder);
    }
}
