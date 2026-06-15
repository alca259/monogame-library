namespace MonoGame.Editor.Core.Commands;

/// <summary>Duplica un <see cref="EditorGameObject"/> con todos sus hijos y behaviours, insertándolo a continuación del original.</summary>
public sealed class DuplicateEntityCommand : IEditorCommand
{
    private readonly EditorGameObject _source;
    private readonly EditorScene _scene;
    private EditorGameObject? _clone;

    /// <param name="source">Objeto a duplicar.</param>
    /// <param name="scene">Escena propietaria del objeto.</param>
    public DuplicateEntityCommand(EditorGameObject source, EditorScene scene)
    {
        _source = source;
        _scene = scene;
    }

    /// <inheritdoc/>
    public string Description => $"Duplicate '{_source.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _clone = DeepClone(_source, _source.Parent);
        if (_source.Parent is not null)
        {
            int index = _source.Parent.Children.IndexOf(_source);
            _source.Parent.Children.Insert(index + 1, _clone);
        }
        else
        {
            int index = _scene.RootGameObjects.IndexOf(_source);
            _scene.RootGameObjects.Insert(index + 1, _clone);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (_clone is null) return;
        if (_clone.Parent is not null)
            _clone.Parent.Children.Remove(_clone);
        else
            _scene.RootGameObjects.Remove(_clone);
        _clone.Parent = null;
    }

    /// <summary>Crea una copia profunda de <paramref name="source"/> con un nuevo <see cref="EditorGameObject.Id"/>.</summary>
    public static EditorGameObject DeepClone(EditorGameObject source, EditorGameObject? parent)
    {
        EditorGameObject clone = new()
        {
            Name = source.Name + " (copy)",
            Active = source.Active,
            Position = source.Position,
            Rotation = source.Rotation,
            Scale = source.Scale,
            PrefabPath = source.PrefabPath,
            Parent = parent,
        };

        foreach (string tag in source.Tags)
            clone.Tags.Add(tag);

        foreach (EditorBehaviour behaviour in source.Behaviours)
        {
            EditorBehaviour cloned = new()
            {
                TypeName = behaviour.TypeName,
                Enabled = behaviour.Enabled,
            };
            foreach (KeyValuePair<string, JsonElement> prop in behaviour.Properties)
                cloned.Properties[prop.Key] = prop.Value;
            clone.Behaviours.Add(cloned);
        }

        foreach (EditorGameObject child in source.Children)
            clone.Children.Add(DeepClone(child, clone));

        return clone;
    }
}
