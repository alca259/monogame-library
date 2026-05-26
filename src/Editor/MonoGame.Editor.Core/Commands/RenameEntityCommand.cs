namespace MonoGame.Editor.Core.Commands;

/// <summary>Renames a <see cref="EditorGameObject"/>.</summary>
public sealed class RenameEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly string _previousName;
    private readonly string _newName;

    /// <param name="target">Object to rename.</param>
    /// <param name="newName">New display name.</param>
    public RenameEntityCommand(EditorGameObject target, string newName)
    {
        _target = target;
        _previousName = target.Name;
        _newName = newName;
    }

    /// <inheritdoc/>
    public string Description => $"Rename '{_previousName}' to '{_newName}'";

    /// <inheritdoc/>
    public void Execute() => _target.Name = _newName;

    /// <inheritdoc/>
    public void Undo() => _target.Name = _previousName;
}
