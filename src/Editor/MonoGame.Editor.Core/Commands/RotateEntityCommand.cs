namespace MonoGame.Editor.Core.Commands;

/// <summary>Changes the rotation (in degrees) of a <see cref="EditorGameObject"/>.</summary>
public sealed class RotateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly float _previousRotation;
    private readonly float _newRotation;

    /// <param name="target">Object to rotate.</param>
    /// <param name="newRotation">Target rotation in degrees.</param>
    public RotateEntityCommand(EditorGameObject target, float newRotation)
    {
        _target = target;
        _previousRotation = target.Rotation;
        _newRotation = newRotation;
    }

    /// <param name="target">Object to rotate.</param>
    /// <param name="previousRotation">Rotation before the change in degrees (explicit, used by gizmo drag).</param>
    /// <param name="newRotation">Target rotation in degrees.</param>
    public RotateEntityCommand(EditorGameObject target, float previousRotation, float newRotation)
    {
        _target = target;
        _previousRotation = previousRotation;
        _newRotation = newRotation;
    }

    /// <inheritdoc/>
    public string Description => $"Rotate '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.Rotation = _newRotation;

    /// <inheritdoc/>
    public void Undo() => _target.Rotation = _previousRotation;
}
