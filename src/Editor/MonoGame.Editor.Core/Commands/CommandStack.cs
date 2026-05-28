namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Maintains an undo/redo history of <see cref="IEditorCommand"/> operations.
/// Publishes <see cref="UndoPerformedEvent"/> and <see cref="RedoPerformedEvent"/> on the optional event bus.
/// </summary>
public sealed class CommandStack
{
    private readonly LinkedList<IEditorCommand> _undoHistory = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private readonly IEditorEventBus? _eventBus;

    /// <summary>Maximum number of operations retained in the undo history.</summary>
    public int MaxHistory { get; }

    /// <summary>Description of the command that would be undone next, or <c>null</c> if the history is empty.</summary>
    public string? UndoDescription => _undoHistory.Count > 0 ? _undoHistory.Last!.Value.Description : null;

    /// <summary>Description of the command that would be redone next, or <c>null</c> if the redo stack is empty.</summary>
    public string? RedoDescription => _redoStack.TryPeek(out IEditorCommand? cmd) ? cmd.Description : null;

    /// <summary>Returns all undo history descriptions from most-recent to oldest.</summary>
    public IReadOnlyList<string> GetUndoDescriptions()
    {
        var result = new List<string>(_undoHistory.Count);
        for (LinkedListNode<IEditorCommand>? node = _undoHistory.Last; node is not null; node = node.Previous)
            result.Add(node.Value.Description);
        return result;
    }

    /// <summary>Returns all redo stack descriptions from next-to-redo to furthest.</summary>
    public IReadOnlyList<string> GetRedoDescriptions()
    {
        var result = new List<string>(_redoStack.Count);
        foreach (IEditorCommand cmd in _redoStack)
            result.Add(cmd.Description);
        return result;
    }

    /// <param name="maxHistory">Maximum number of commands to keep. Defaults to 100.</param>
    /// <param name="eventBus">Optional bus; if provided, publishes undo/redo events.</param>
    public CommandStack(int maxHistory = 100, IEditorEventBus? eventBus = null)
    {
        MaxHistory = maxHistory;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Executes <paramref name="command"/>, pushes it onto the undo history, and clears the redo stack.
    /// If history exceeds <see cref="MaxHistory"/>, the oldest entry is discarded.
    /// Marks the active scene dirty when a scene is open.
    /// </summary>
    public void Execute(IEditorCommand command)
    {
        command.Execute();
        _undoHistory.AddLast(command);
        _redoStack.Clear();
        if (_undoHistory.Count > MaxHistory)
            _undoHistory.RemoveFirst();

        if (EditorContext.Instance.ActiveScene is not null)
            EditorContext.Instance.MarkSceneDirty();
    }

    /// <summary>Undoes the most recent command and pushes it onto the redo stack.</summary>
    public void Undo()
    {
        if (_undoHistory.Count == 0)
            return;

        IEditorCommand command = _undoHistory.Last!.Value;
        _undoHistory.RemoveLast();
        command.Undo();
        _redoStack.Push(command);
        _eventBus?.Publish(new UndoPerformedEvent(command.Description));
    }

    /// <summary>Re-executes the most recently undone command and pushes it back onto the undo history.</summary>
    public void Redo()
    {
        if (!_redoStack.TryPop(out IEditorCommand? command))
            return;

        command.Execute();
        _undoHistory.AddLast(command);
        if (_undoHistory.Count > MaxHistory)
            _undoHistory.RemoveFirst();
        _eventBus?.Publish(new RedoPerformedEvent(command.Description));
    }

    /// <summary>Clears both the undo history and the redo stack.</summary>
    public void Clear()
    {
        _undoHistory.Clear();
        _redoStack.Clear();
    }
}
