namespace MonoGame.Editor.Core.Commands;

/// <summary>Aplica un delta de posición a un conjunto de instancias de <see cref="EditorGameObject"/> en un solo paso de deshacer.</summary>
public sealed class BatchMoveCommand : IEditorCommand
{
    private readonly IReadOnlyList<EditorGameObject> _targets;
    private readonly EditorVector3 _delta;

    /// <param name="targets">Objetos a mover.</param>
    /// <param name="delta">Desplazamiento en espacio mundial que se suma a la posición actual de cada objeto.</param>
    public BatchMoveCommand(IReadOnlyList<EditorGameObject> targets, EditorVector3 delta)
    {
        _targets = targets;
        _delta = delta;
    }

    /// <inheritdoc/>
    public string Description => $"Move {_targets.Count} objects";

    /// <inheritdoc/>
    public void Execute()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EditorGameObject t = _targets[i];
            t.Position = new EditorVector3(t.Position.X + _delta.X, t.Position.Y + _delta.Y, t.Position.Z + _delta.Z);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EditorGameObject t = _targets[i];
            t.Position = new EditorVector3(t.Position.X - _delta.X, t.Position.Y - _delta.Y, t.Position.Z - _delta.Z);
        }
    }
}
