namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// Controls the progression of an active <see cref="DialogueScript"/>.
/// Raises events on line changes, choices, and conversation end so that UI components
/// can react without coupling directly to the script data.
/// </summary>
public sealed class DialogueManager
{
    private DialogueScript? _activeScript;
    private int _currentLineIndex;

    /// <summary>Gets a value indicating whether a dialogue is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a read-only reference to the current <see cref="DialogueLine"/>.
    /// Only valid when <see cref="IsActive"/> is <see langword="true"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed outside of an active dialogue.</exception>
    public ref readonly DialogueLine CurrentLine
    {
        get
        {
            if (!IsActive || _activeScript is null)
                throw new InvalidOperationException("No dialogue is currently active.");
            return ref _activeScript.GetLine(_currentLineIndex);
        }
    }

    /// <summary>
    /// Gets or sets an optional delegate that evaluates a <see cref="DialogueCondition"/>.
    /// When <see langword="null"/> all conditions are treated as satisfied.
    /// </summary>
    public Func<DialogueCondition, bool>? ConditionEvaluator { get; set; }

    /// <summary>Raised when a new dialogue script is started. The script is passed as argument.</summary>
    public Action<DialogueScript>? OnStarted;

    /// <summary>Raised whenever the active line changes. The new <see cref="DialogueLine"/> is passed as argument.</summary>
    public Action<DialogueLine>? OnLineChanged;

    /// <summary>Raised when the player selects a choice. The zero-based choice index is passed as argument.</summary>
    public Action<int>? OnChoiceMade;

    /// <summary>Raised when the dialogue has ended (either naturally or via <see cref="EndDialogue"/>).</summary>
    public Action? OnEnded;

    /// <summary>
    /// Starts playback of <paramref name="script"/> from the first line.
    /// If another dialogue is already active it is ended first.
    /// </summary>
    public void StartDialogue(DialogueScript script)
    {
        if (IsActive)
            EndDialogue();

        _activeScript = script;
        _currentLineIndex = 0;
        IsActive = true;

        OnStarted?.Invoke(script);
        OnLineChanged?.Invoke(script.GetLine(0));
    }

    /// <summary>
    /// Advances to the next sequential line.
    /// Does nothing when the current line has choices — call <see cref="SelectChoice"/> instead.
    /// Ends the dialogue when the last line has been reached.
    /// </summary>
    public void Advance()
    {
        if (!IsActive || _activeScript is null) return;
        if (CurrentLine.HasChoices) return;

        int next = _currentLineIndex + 1;
        if (_activeScript.TryGetLine(next, out DialogueLine nextLine))
        {
            _currentLineIndex = next;
            OnLineChanged?.Invoke(nextLine);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// Selects the choice at <paramref name="choiceIndex"/> within the current line's choices array
    /// and jumps to the line it points to.
    /// Does nothing if the index is out of range or its condition is not satisfied.
    /// </summary>
    public void SelectChoice(int choiceIndex)
    {
        if (!IsActive || _activeScript is null) return;

        ref readonly DialogueLine line = ref CurrentLine;
        if (!line.HasChoices) return;
        if ((uint)choiceIndex >= (uint)line.Choices.Length) return;

        DialogueChoice choice = line.Choices[choiceIndex];
        if (!EvaluateCondition(choice.Condition)) return;

        OnChoiceMade?.Invoke(choiceIndex);

        _currentLineIndex = choice.NextLineIndex;
        if (_activeScript.TryGetLine(_currentLineIndex, out DialogueLine nextLine))
            OnLineChanged?.Invoke(nextLine);
        else
            EndDialogue();
    }

    /// <summary>Ends the active dialogue and raises <see cref="OnEnded"/>.</summary>
    public void EndDialogue()
    {
        IsActive = false;
        _activeScript = null;
        OnEnded?.Invoke();
    }

    /// <summary>
    /// Evaluates <paramref name="condition"/> using <see cref="ConditionEvaluator"/> when set.
    /// Returns <see langword="true"/> for empty conditions or when no evaluator is registered.
    /// </summary>
    public bool EvaluateCondition(DialogueCondition condition)
    {
        if (condition.IsEmpty) return true;
        return ConditionEvaluator?.Invoke(condition) ?? true;
    }
}
