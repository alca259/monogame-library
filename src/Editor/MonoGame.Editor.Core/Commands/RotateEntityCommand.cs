namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia la rotación Euler de un <see cref="EditorGameObject"/>.</summary>
public sealed class RotateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorVector3 _previousRotation;
    private readonly EditorVector3 _newRotation;

    /// <param name="target">Objeto a rotar.</param>
    /// <param name="newRotation">Rotación de destino en grados (X, Y, Z).</param>
    public RotateEntityCommand(EditorGameObject target, EditorVector3 newRotation)
    {
        _target = target;
        _previousRotation = target.Rotation;
        _newRotation = newRotation;
    }

    /// <param name="target">Objeto a rotar.</param>
    /// <param name="previousRotation">Rotación antes del giro (explícita, usada por el arrastre del gizmo).</param>
    /// <param name="newRotation">Rotación de destino en grados (X, Y, Z).</param>
    public RotateEntityCommand(EditorGameObject target, EditorVector3 previousRotation, EditorVector3 newRotation)
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
