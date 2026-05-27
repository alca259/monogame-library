namespace MonoGame.Editor.Core.Commands;

/// <summary>Changes the Z-depth of a <see cref="EditorGameObject"/> (used in 2.5D mode).</summary>
public sealed class MoveEntityZCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly float _previousZ;
    private readonly float _newZ;

    /// <summary>Initializes a new instance of <see cref="MoveEntityZCommand"/>.</summary>
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
