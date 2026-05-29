namespace MonoGame.Editor.Core.Commands;

/// <summary>Elimina un <see cref="EditorGameObject"/> (y todos sus hijos) de la jerarquía de la escena.</summary>
public sealed class DeleteEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorScene _scene;
    private EditorGameObject? _previousParent;
    private int _previousIndex;

    /// <param name="target">Objeto a eliminar.</param>
    /// <param name="scene">Escena propietaria del objeto.</param>
    public DeleteEntityCommand(EditorGameObject target, EditorScene scene)
    {
        _target = target;
        _scene = scene;
    }

    /// <inheritdoc/>
    public string Description => $"Delete '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _previousParent = _target.Parent;
        if (_previousParent is not null)
        {
            _previousIndex = _previousParent.Children.IndexOf(_target);
            _previousParent.Children.RemoveAt(_previousIndex);
        }
        else
        {
            _previousIndex = _scene.RootGameObjects.IndexOf(_target);
            _scene.RootGameObjects.RemoveAt(_previousIndex);
        }

        _target.Parent = null;
    }

    /// <inheritdoc/>
    public void Undo()
    {
        _target.Parent = _previousParent;
        if (_previousParent is not null)
        {
            int index = Math.Min(_previousIndex, _previousParent.Children.Count);
            _previousParent.Children.Insert(index, _target);
        }
        else
        {
            int index = Math.Min(_previousIndex, _scene.RootGameObjects.Count);
            _scene.RootGameObjects.Insert(index, _target);
        }
    }
}
