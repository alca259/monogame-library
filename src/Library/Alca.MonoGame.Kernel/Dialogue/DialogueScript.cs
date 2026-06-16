namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// An immutable, ordered collection of <see cref="DialogueLine"/> entries that form a complete conversation.
/// Build instances via <see cref="Builder"/> or <see cref="Create"/>.
/// </summary>
public sealed class DialogueScript
{
    private readonly DialogueLine[] _lines;

    /// <summary>Gets the total number of lines in this script.</summary>
    public int LineCount => _lines.Length;

    /// <summary>Creates a new <see cref="DialogueScript"/> from the provided lines array. The array is copied internally.</summary>
    public DialogueScript(DialogueLine[] lines)
    {
        _lines = new DialogueLine[lines.Length];
        Array.Copy(lines, _lines, lines.Length);
    }

    /// <summary>
    /// Returns a read-only reference to the <see cref="DialogueLine"/> at <paramref name="index"/>.
    /// Avoids copying the struct off the heap.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside [0, <see cref="LineCount"/>).</exception>
    public ref readonly DialogueLine GetLine(int index)
    {
        if ((uint)index >= (uint)_lines.Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be in [0, {_lines.Length}).");
        return ref _lines[index];
    }

    /// <summary>
    /// Tries to retrieve the <see cref="DialogueLine"/> at <paramref name="index"/>.
    /// Returns <see langword="false"/> without throwing when the index is out of range.
    /// </summary>
    public bool TryGetLine(int index, out DialogueLine line)
    {
        if ((uint)index >= (uint)_lines.Length)
        {
            line = default;
            return false;
        }

        line = _lines[index];
        return true;
    }

    /// <summary>Returns a new <see cref="Builder"/> to construct a <see cref="DialogueScript"/> fluently.</summary>
    public static Builder Create() => new();

    #region Nested Builder
    /// <summary>Fluent builder for <see cref="DialogueScript"/>.</summary>
    public sealed class Builder
    {
        private readonly List<DialogueLine> _lines = [];

        /// <summary>Appends a new <see cref="DialogueLine"/> to the script.</summary>
        /// <param name="speakerId">Character identifier for the speaker.</param>
        /// <param name="locKey">Localization key for the dialogue text.</param>
        /// <param name="portraitKey">Portrait texture key. Defaults to empty string.</param>
        /// <param name="choices">Optional branching choices.</param>
        public Builder AddLine(string speakerId, string locKey, string portraitKey = "", params DialogueChoice[] choices)
        {
            _lines.Add(new DialogueLine(speakerId, locKey, portraitKey, choices));
            return this;
        }

        /// <summary>Builds and returns the completed <see cref="DialogueScript"/>.</summary>
        public DialogueScript Build() => new(_lines.ToArray());
    }
    #endregion
}
