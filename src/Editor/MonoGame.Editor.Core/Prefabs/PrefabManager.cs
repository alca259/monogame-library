namespace MonoGame.Editor.Core.Prefabs;

/// <summary>
/// Gestiona la serialización e instanciación de prefabs. Implementa <see cref="IPrefabProvider"/>
/// para poder pasarse directamente a <see cref="ApplyPrefabCommand"/> y <see cref="RevertPrefabCommand"/>.
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
    /// Serializa <paramref name="source"/> (sin su propiedad de seguimiento <see cref="EditorGameObject.PrefabPath"/>)
    /// en <paramref name="prefabPath"/>.
    /// </summary>
    public void Save(EditorGameObject source, string prefabPath)
    {
        // Elimina el PrefabPath para que la definición almacenada nunca se referencie a sí misma.
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
    /// Carga el prefab en <paramref name="prefabPath"/>, realiza una copia profunda, asigna
    /// <see cref="EditorGameObject.PrefabPath"/> en la copia y la devuelve.
    /// Devuelve <c>null</c> si el archivo de prefab no existe.
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
