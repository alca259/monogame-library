namespace MonoGame.Editor.Core.Commands;

/// <summary>Changes the world-space position of a <see cref="EditorGameObject"/>.</summary>
public sealed class MoveEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorVector2 _previousPosition;
    private readonly EditorVector2 _newPosition;

    /// <param name="target">Object to move.</param>
    /// <param name="newPosition">Target position.</param>
    public MoveEntityCommand(EditorGameObject target, EditorVector2 newPosition)
    {
        _target = target;
        _previousPosition = target.Position;
        _newPosition = newPosition;
    }

    /// <param name="target">Object to move.</param>
    /// <param name="previousPosition">Position before the move (explicit, used by gizmo drag).</param>
    /// <param name="newPosition">Target position.</param>
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
