namespace MonoGame.Editor.Core.Commands;

/// <summary>Crea un nuevo <see cref="EditorGameObject"/> y lo añade a un padre o a la raíz de la escena.</summary>
public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _newObject;
    private readonly EditorScene _scene;
    private readonly EditorGameObject? _parent;

    /// <param name="newObject">El objeto a añadir. No debe formar parte ya de la escena.</param>
    /// <param name="scene">Escena de destino.</param>
    /// <param name="parent">Padre al que adjuntar, o <c>null</c> para añadir en la raíz.</param>
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
