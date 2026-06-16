namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Obsoleto: la profundidad Z está integrada en <see cref="EditorGameObject.Position"/>.
/// Usa <see cref="MoveEntityCommand"/> con <see cref="EditorVector3"/> para todos los cambios de posición.
/// </summary>
[Obsolete("Use MoveEntityCommand with EditorVector3. This class will be removed in a future version.")]
public sealed class MoveEntityZCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly float _previousZ;
    private readonly float _newZ;

    /// <summary>Inicializa una nueva instancia de <see cref="MoveEntityZCommand"/>.</summary>
    public MoveEntityZCommand(EditorGameObject target, float previousZ, float newZ)
    {
        _target = target;
        _previousZ = previousZ;
        _newZ = newZ;
    }

    /// <inheritdoc/>
    public string Description => $"Set Z depth '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.Position = new EditorVector3(_target.Position.X, _target.Position.Y, _newZ);

    /// <inheritdoc/>
    public void Undo() => _target.Position = new EditorVector3(_target.Position.X, _target.Position.Y, _previousZ);
}
