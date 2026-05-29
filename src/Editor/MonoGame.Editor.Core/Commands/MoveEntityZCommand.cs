namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia la profundidad Z de un <see cref="EditorGameObject"/> (usado en modo 2.5D).</summary>
public sealed class MoveEntityZCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly float _previousZ;
    private readonly float _newZ;

    /// <summary>Inicializa una nueva instancia de <see cref="MoveEntityZCommand"/>.</summary>
    public MoveEntityZCommand(EditorGameObject target, float previousZ, float newZ)
    {
        _target    = target;
        _previousZ = previousZ;
        _newZ      = newZ;
    }

    /// <inheritdoc/>
    public string Description => $"Set Z depth '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.PositionZ = _newZ;

    /// <inheritdoc/>
    public void Undo() => _target.PositionZ = _previousZ;
}
