namespace MonoGame.Editor.Core.Commands;

/// <summary>Applies a position delta to a set of <see cref="EditorGameObject"/> instances in one undo step.</summary>
public sealed class BatchMoveCommand : IEditorCommand
{
    private readonly IReadOnlyList<EditorGameObject> _targets;
    private readonly EditorVector2 _delta;

    /// <param name="targets">Objects to move.</param>
    /// <param name="delta">World-space offset to add to each object's current position.</param>
    public BatchMoveCommand(IReadOnlyList<EditorGameObject> targets, EditorVector2 delta)
    {
        _targets = targets;
        _delta   = delta;
    }

    /// <inheritdoc/>
    public string Description => $"Move {_targets.Count} objects";

    /// <inheritdoc/>
    public void Execute()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EditorGameObject t = _targets[i];
            t.Position = new EditorVector2(t.Position.X + _delta.X, t.Position.Y + _delta.Y);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            EditorGameObject t = _targets[i];
            t.Position = new EditorVector2(t.Position.X - _delta.X, t.Position.Y - _delta.Y);
        }
    }
}
