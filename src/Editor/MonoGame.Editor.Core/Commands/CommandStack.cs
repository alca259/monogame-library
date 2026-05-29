namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Mantiene un historial de deshacer/rehacer de operaciones <see cref="IEditorCommand"/>.
/// Publica <see cref="UndoPerformedEvent"/> y <see cref="RedoPerformedEvent"/> en el bus de eventos opcional.
/// </summary>
public sealed class CommandStack
{
    private readonly LinkedList<IEditorCommand> _undoHistory = new();
    private readonly Stack<IEditorCommand> _redoStack = new();
    private readonly IEditorEventBus? _eventBus;

    /// <summary>Número máximo de operaciones conservadas en el historial de deshacer.</summary>
    public int MaxHistory { get; }

    /// <summary>Descripción del comando que se desharía a continuación, o <c>null</c> si el historial está vacío.</summary>
    public string? UndoDescription => _undoHistory.Count > 0 ? _undoHistory.Last!.Value.Description : null;

    /// <summary>Descripción del comando que se reharía a continuación, o <c>null</c> si la pila de rehacer está vacía.</summary>
    public string? RedoDescription => _redoStack.TryPeek(out IEditorCommand? cmd) ? cmd.Description : null;

    /// <summary>Devuelve todas las descripciones del historial de deshacer, de la más reciente a la más antigua.</summary>
    public IReadOnlyList<string> GetUndoDescriptions()
    {
        var result = new List<string>(_undoHistory.Count);
        for (LinkedListNode<IEditorCommand>? node = _undoHistory.Last; node is not null; node = node.Previous)
            result.Add(node.Value.Description);
        return result;
    }

    /// <summary>Devuelve todas las descripciones de la pila de rehacer, desde la próxima a rehacer hasta la más lejana.</summary>
    public IReadOnlyList<string> GetRedoDescriptions()
    {
        var result = new List<string>(_redoStack.Count);
        foreach (IEditorCommand cmd in _redoStack)
            result.Add(cmd.Description);
        return result;
    }

    /// <param name="maxHistory">Número máximo de comandos a conservar. El valor predeterminado es 100.</param>
    /// <param name="eventBus">Bus opcional; si se proporciona, publica eventos de deshacer/rehacer.</param>
    public CommandStack(int maxHistory = 100, IEditorEventBus? eventBus = null)
    {
        MaxHistory = maxHistory;
        _eventBus = eventBus;
    }

    /// <summary>
    /// Ejecuta <paramref name="command"/>, lo apila en el historial de deshacer y vacía la pila de rehacer.
    /// Si el historial supera <see cref="MaxHistory"/>, se descarta la entrada más antigua.
    /// Marca la escena activa como modificada cuando hay una escena abierta.
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

    /// <summary>Deshace el comando más reciente y lo apila en la pila de rehacer.</summary>
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

    /// <summary>Vuelve a ejecutar el comando deshecho más recientemente y lo devuelve al historial de deshacer.</summary>
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

    /// <summary>Vacía tanto el historial de deshacer como la pila de rehacer.</summary>
    public void Clear()
    {
        _undoHistory.Clear();
        _redoStack.Clear();
    }
}
