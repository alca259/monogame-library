namespace MonoGame.Editor.Core.Commands;

/// <summary>Cambia el padre de un <see cref="EditorGameObject"/> dentro de la jerarquía de la escena.</summary>
public sealed class ReparentEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorScene _scene;
    private readonly EditorGameObject? _newParent;
    private EditorGameObject? _previousParent;
    private int _previousIndex;

    /// <param name="target">Objeto a mover.</param>
    /// <param name="scene">Escena propietaria del objeto.</param>
    /// <param name="newParent">Nuevo padre, o <c>null</c> para mover a la raíz de la escena.</param>
    public ReparentEntityCommand(EditorGameObject target, EditorScene scene, EditorGameObject? newParent)
    {
        _target = target;
        _scene = scene;
        _newParent = newParent;
    }

    /// <inheritdoc/>
    public string Description => $"Reparent '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _previousParent = _target.Parent;
        RemoveFromCurrent();

        _target.Parent = _newParent;
        if (_newParent is not null)
            _newParent.Children.Add(_target);
        else
            _scene.RootGameObjects.Add(_target);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_newParent is not null)
            _newParent.Children.Remove(_target);
        else
            _scene.RootGameObjects.Remove(_target);

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

    private void RemoveFromCurrent()
    {
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
    }
}
