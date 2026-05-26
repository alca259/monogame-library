namespace MonoGame.Editor.Core.Commands;

/// <summary>Creates a new <see cref="EditorGameObject"/> and adds it to a parent or the scene root.</summary>
public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _newObject;
    private readonly EditorScene _scene;
    private readonly EditorGameObject? _parent;

    /// <param name="newObject">The object to add. Must not already be part of the scene.</param>
    /// <param name="scene">Target scene.</param>
    /// <param name="parent">Parent to attach to, or <c>null</c> to add at the root.</param>
    public CreateEntityCommand(EditorGameObject newObject, EditorScene scene, EditorGameObject? parent = null)
    {
        _newObject = newObject;
        _scene = scene;
        _parent = parent;
    }

    /// <inheritdoc/>
    public string Description => $"Create '{_newObject.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _newObject.Parent = _parent;
        if (_parent is not null)
            _parent.Children.Add(_newObject);
        else
            _scene.RootGameObjects.Add(_newObject);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_parent is not null)
            _parent.Children.Remove(_newObject);
        else
            _scene.RootGameObjects.Remove(_newObject);
        _newObject.Parent = null;
    }
}
