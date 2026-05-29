namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// A single line of dialogue spoken by a character. Lines may have branching <see cref="DialogueChoice"/> options.
/// </summary>
public readonly struct DialogueLine
{
    /// <summary>Gets the identifier of the character speaking this line (e.g. a character name or ID).</summary>
    public string SpeakerId { get; }

    /// <summary>Gets the localization key for the spoken text. Resolve against an <c>IStringLocalizer</c> for display.</summary>
    public string LocalizationKey { get; }

    /// <summary>Gets the key used to look up the speaker's portrait texture. May be empty.</summary>
    public string PortraitKey { get; }

    /// <summary>Gets the array of choices presented after this line. Empty when the conversation flows linearly.</summary>
    public DialogueChoice[] Choices { get; }

    /// <summary>Gets a value indicating whether this line has one or more branching choices.</summary>
    public bool HasChoices => Choices is { Length: > 0 };

    /// <summary>
    /// Creates a new <see cref="DialogueLine"/>.
    /// </summary>
    /// <param name="speakerId">Character identifier for the speaker.</param>
    /// <param name="localizationKey">Localization key for the dialogue text.</param>
    /// <param name="portraitKey">Portrait texture key. Pass an empty string when no portrait is needed.</param>
    /// <param name="choices">Optional branching choices. Pass an empty array or omit for linear flow.</param>
    public DialogueLine(string speakerId, string localizationKey, string portraitKey, DialogueChoice[] choices)
    {
        SpeakerId = speakerId;
        LocalizationKey = localizationKey;
        PortraitKey = portraitKey;
        Choices = choices;
    }
}
