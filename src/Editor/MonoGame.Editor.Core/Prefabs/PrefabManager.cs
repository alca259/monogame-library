using MonoGame.Editor.Core.Commands;

namespace MonoGame.Editor.Core.Prefabs;

/// <summary>
/// Manages prefab serialization and instantiation. Implements <see cref="IPrefabProvider"/>
/// so it can be passed directly to <see cref="ApplyPrefabCommand"/> and <see cref="RevertPrefabCommand"/>.
/// </summary>
public sealed class PrefabManager : IPrefabProvider
{
    #region IPrefabProvider

    /// <inheritdoc/>
    public EditorGameObject? LoadPrefab(string prefabPath)
        => PrefabSerializer.Load(prefabPath);

    /// <inheritdoc/>
    public void SavePrefab(EditorGameObject obj, string prefabPath)
        => PrefabSerializer.Save(obj, prefabPath);

    #endregion

    #region Public API

    /// <summary>
    /// Serializes <paramref name="source"/> (without its <see cref="EditorGameObject.PrefabPath"/>
    /// tracking property) to <paramref name="prefabPath"/>.
    /// </summary>
    public void Save(EditorGameObject source, string prefabPath)
    {
        // Strip the PrefabPath so the stored definition never references itself.
        string? savedPrefabPath = source.PrefabPath;
        source.PrefabPath = null;
        try
        {
            PrefabSerializer.Save(source, prefabPath);
        }
        finally
        {
            source.PrefabPath = savedPrefabPath;
        }
    }

    /// <summary>
    /// Loads the prefab at <paramref name="prefabPath"/>, performs a deep copy, assigns
    /// <see cref="EditorGameObject.PrefabPath"/> on the copy, and returns it.
    /// Returns <c>null</c> if the prefab file does not exist.
    /// </summary>
    public EditorGameObject? Instantiate(string prefabPath)
    {
        EditorGameObject? template = PrefabSerializer.Load(prefabPath);
        if (template is null) return null;
        EditorGameObject instance = DeepCopy(template);
        instance.PrefabPath = prefabPath;
        return instance;
    }

    #endregion

    #region Helpers

    private static EditorGameObject DeepCopy(EditorGameObject source)
    {
        EditorGameObject copy = new()
        {
            Name       = source.Name,
            Active     = source.Active,
            Position   = source.Position,
            Rotation   = source.Rotation,
            Scale      = source.Scale,
            PrefabPath = source.PrefabPath,
        };

        for (int i = 0; i < source.Behaviours.Count; i++)
        {
            EditorBehaviour b    = source.Behaviours[i];
            EditorBehaviour bCopy = new() { TypeName = b.TypeName, Enabled = b.Enabled };
            foreach (System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonElement> kv in b.Properties)
                bCopy.Properties[kv.Key] = kv.Value;
            copy.Behaviours.Add(bCopy);
        }

        for (int i = 0; i < source.Children.Count; i++)
        {
            EditorGameObject childCopy = DeepCopy(source.Children[i]);
            childCopy.Parent = copy;
            copy.Children.Add(childCopy);
        }

        return copy;
    }

    #endregion
}
