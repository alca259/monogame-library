using Alca.MonoGame.Kernel.Dialogue;

namespace Alca.MonoGame.Kernel.UnitTests.Dialogue;

/// <summary>Unit tests for <see cref="DialogueManager"/>.</summary>
public sealed class DialogueManagerTests
{
    private static DialogueLine MakeLine(string key = "line", params DialogueChoice[] choices)
        => new DialogueLine("NPC", key, string.Empty, choices);

    private static DialogueScript MakeScript(params DialogueLine[] lines)
        => new DialogueScript(lines);

    // ── IsActive ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_BeforeStart_IsFalse()
    {
        DialogueManager sut = new();
        Assert.False(sut.IsActive);
    }

    [Fact]
    public void StartDialogue_SetsIsActiveTrue()
    {
        DialogueManager sut = new();
        sut.StartDialogue(MakeScript(MakeLine()));
        Assert.True(sut.IsActive);
    }

    [Fact]
    public void EndDialogue_SetsIsActiveFalse()
    {
        DialogueManager sut = new();
        sut.StartDialogue(MakeScript(MakeLine()));
        sut.EndDialogue();
        Assert.False(sut.IsActive);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    [Fact]
    public void StartDialogue_RaisesOnStarted()
    {
        DialogueManager sut = new();
        DialogueScript? received = null;
        sut.OnStarted = s => received = s;
        DialogueScript script = MakeScript(MakeLine());

        sut.StartDialogue(script);

        Assert.Same(script, received);
    }

    [Fact]
    public void StartDialogue_RaisesOnLineChanged_WithFirstLine()
    {
        DialogueManager sut = new();
        DialogueLine? receivedLine = null;
        sut.OnLineChanged = l => receivedLine = l;

        sut.StartDialogue(MakeScript(MakeLine("first")));

        Assert.Equal("first", receivedLine?.LocalizationKey);
    }

    [Fact]
    public void Advance_RaisesOnLineChanged_WithNextLine()
    {
        DialogueManager sut = new();
        string? lastKey = null;
        sut.OnLineChanged = l => lastKey = l.LocalizationKey;

        sut.StartDialogue(MakeScript(MakeLine("line0"), MakeLine("line1")));
        sut.Advance();

        Assert.Equal("line1", lastKey);
    }

    [Fact]
    public void Advance_OnLastLine_RaisesOnEnded()
    {
        DialogueManager sut = new();
        bool ended = false;
        sut.OnEnded = () => ended = true;

        sut.StartDialogue(MakeScript(MakeLine()));
        sut.Advance(); // last line → end

        Assert.True(ended);
    }

    [Fact]
    public void EndDialogue_RaisesOnEnded()
    {
        DialogueManager sut = new();
        bool ended = false;
        sut.OnEnded = () => ended = true;
        sut.StartDialogue(MakeScript(MakeLine()));

        sut.EndDialogue();

        Assert.True(ended);
    }

    // ── Advance guards ───────────────────────────────────────────────────────

    [Fact]
    public void Advance_WhenNotActive_DoesNotThrow()
    {
        DialogueManager sut = new();
        Exception? ex = Record.Exception(() => sut.Advance());
        Assert.Null(ex);
    }

    [Fact]
    public void Advance_WhenCurrentLineHasChoices_DoesNotAdvance()
    {
        DialogueManager sut = new();
        string? lastKey = null;
        sut.OnLineChanged = l => lastKey = l.LocalizationKey;

        DialogueChoice choice = new DialogueChoice("yes", 1);
        sut.StartDialogue(MakeScript(MakeLine("withChoice", choice), MakeLine("next")));
        sut.Advance(); // should be blocked

        Assert.Equal("withChoice", lastKey);
    }

    // ── SelectChoice ─────────────────────────────────────────────────────────

    [Fact]
    public void SelectChoice_ValidIndex_JumpsToTargetLine()
    {
        DialogueManager sut = new();
        string? lastKey = null;
        sut.OnLineChanged = l => lastKey = l.LocalizationKey;

        DialogueChoice choice = new DialogueChoice("yes", 2);
        sut.StartDialogue(MakeScript(
            MakeLine("start", choice),
            MakeLine("skipped"),
            MakeLine("target")));

        sut.SelectChoice(0); // jump to index 2

        Assert.Equal("target", lastKey);
    }

    [Fact]
    public void SelectChoice_RaisesOnChoiceMade_WithIndex()
    {
        DialogueManager sut = new();
        int? chosenIndex = null;
        sut.OnChoiceMade = i => chosenIndex = i;

        DialogueChoice choice = new DialogueChoice("ok", 1);
        sut.StartDialogue(MakeScript(MakeLine("q", choice), MakeLine("a")));
        sut.SelectChoice(0);

        Assert.Equal(0, chosenIndex);
    }

    [Fact]
    public void SelectChoice_OutOfRange_DoesNotThrow()
    {
        DialogueManager sut = new();
        DialogueChoice choice = new DialogueChoice("ok", 1);
        sut.StartDialogue(MakeScript(MakeLine("q", choice), MakeLine("a")));

        Exception? ex = Record.Exception(() => sut.SelectChoice(99));

        Assert.Null(ex);
    }

    // ── EvaluateCondition ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluateCondition_EmptyCondition_ReturnsTrue()
    {
        DialogueManager sut = new();
        Assert.True(sut.EvaluateCondition(DialogueCondition.None));
    }

    [Fact]
    public void EvaluateCondition_WithNullEvaluator_ReturnsTrue()
    {
        DialogueManager sut = new();
        DialogueCondition cond = new DialogueCondition("flag", "true");
        Assert.True(sut.EvaluateCondition(cond));
    }

    [Fact]
    public void EvaluateCondition_WithEvaluator_DelegatesToIt()
    {
        DialogueManager sut = new();
        sut.ConditionEvaluator = c => c.Key == "flag" && c.Value == "true";
        DialogueCondition cond = new DialogueCondition("flag", "true");

        Assert.True(sut.EvaluateCondition(cond));
    }

    [Fact]
    public void EvaluateCondition_WithEvaluator_ReturnsFalseWhenNotMet()
    {
        DialogueManager sut = new();
        sut.ConditionEvaluator = _ => false;
        DialogueCondition cond = new DialogueCondition("flag", "true");

        Assert.False(sut.EvaluateCondition(cond));
    }
}
