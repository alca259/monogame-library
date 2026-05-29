namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// Represents a branching choice shown to the player at the end of a <see cref="DialogueLine"/>.
/// Selecting this choice jumps to a specific line index in the active <see cref="DialogueScript"/>.
/// </summary>
public readonly struct DialogueChoice
{
    /// <summary>Gets the localization key for the button text displayed to the player.</summary>
    public string LocalizationKey { get; }

    /// <summary>Gets the zero-based index of the <see cref="DialogueLine"/> to jump to when this choice is selected.</summary>
    public int NextLineIndex { get; }

    /// <summary>Gets the condition that must be satisfied for this choice to be shown. Use <see cref="DialogueCondition.None"/> for unconditional choices.</summary>
    public DialogueCondition Condition { get; }

    /// <summary>
    /// Creates a new <see cref="DialogueChoice"/>.
    /// </summary>
    /// <param name="localizationKey">The localization key for the choice text.</param>
    /// <param name="nextLineIndex">Zero-based index of the next line to jump to.</param>
    /// <param name="condition">Optional condition that must be met for this choice to appear. Defaults to <see cref="DialogueCondition.None"/>.</param>
    public DialogueChoice(string localizationKey, int nextLineIndex, DialogueCondition condition = default)
    {
        LocalizationKey = localizationKey;
        NextLineIndex = nextLineIndex;
        Condition = condition;
    }
}
