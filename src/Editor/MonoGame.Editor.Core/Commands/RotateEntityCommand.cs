namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia la rotación (en grados) de un <see cref="EditorGameObject"/>.</summary>
public sealed class RotateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly float _previousRotation;
    private readonly float _newRotation;

    /// <param name="target">Objeto a rotar.</param>
    /// <param name="newRotation">Rotación de destino en grados.</param>
    public RotateEntityCommand(EditorGameObject target, float newRotation)
    {
        _target = target;
        _previousRotation = target.Rotation;
        _newRotation = newRotation;
    }

    /// <param name="target">Objeto a rotar.</param>
    /// <param name="previousRotation">Rotación anterior al cambio en grados (explícita, usada por el arrastre del gizmo).</param>
    /// <param name="newRotation">Rotación de destino en grados.</param>
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
