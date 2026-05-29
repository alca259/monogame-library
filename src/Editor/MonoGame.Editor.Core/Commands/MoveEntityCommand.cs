namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia la posición en espacio mundial de un <see cref="EditorGameObject"/>.</summary>
public sealed class MoveEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorVector2 _previousPosition;
    private readonly EditorVector2 _newPosition;

    /// <param name="target">Objeto a mover.</param>
    /// <param name="newPosition">Posición de destino.</param>
    public MoveEntityCommand(EditorGameObject target, EditorVector2 newPosition)
    {
        _target = target;
        _previousPosition = target.Position;
        _newPosition = newPosition;
    }

    /// <param name="target">Objeto a mover.</param>
    /// <param name="previousPosition">Posición antes del movimiento (explícita, usada por el arrastre del gizmo).</param>
    /// <param name="newPosition">Posición de destino.</param>
    public MoveEntityCommand(EditorGameObject target, EditorVector2 previousPosition, EditorVector2 newPosition)
    {
        _target = target;
        _previousPosition = previousPosition;
        _newPosition = newPosition;
    }

    /// <inheritdoc/>
    public string Description => $"Move '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.Position = _newPosition;

    /// <inheritdoc/>
    public void Undo() => _target.Position = _previousPosition;
}
