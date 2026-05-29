namespace MonoGame.Editor.Core.Serialization;

/// <summary>
/// Serializa y deserializa objetos <see cref="EditorScene"/> a/desde JSON
/// usando <see cref="System.Text.Json"/>. Los enlaces padre quedan excluidos del JSON
/// y se restauran tras la deserialización.
/// </summary>
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    };

    /// <summary>Serializa <paramref name="scene"/> en una cadena JSON con sangría.</summary>
    public static string Serialize(EditorScene scene)
        => JsonSerializer.Serialize(scene, _options);

    /// <summary>
    /// Deserializa una escena desde <paramref name="json"/> y restaura los enlaces padre en todos los objetos.
    /// Devuelve <c>null</c> si el JSON es inválido o está vacío.
    /// </summary>
    public static EditorScene? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        EditorScene? scene = JsonSerializer.Deserialize<EditorScene>(json, _options);
        if (scene is not null)
            RestoreParentLinks(scene.RootGameObjects, null);
        return scene;
    }

    /// <summary>Guarda <paramref name="scene"/> en el archivo en <paramref name="path"/>.</summary>
    public static async Task SaveAsync(EditorScene scene, string path)
    {
        string json = Serialize(scene);
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }

    /// <summary>Carga y deserializa una escena desde el archivo en <paramref name="path"/>.</summary>
    public static async Task<EditorScene?> LoadAsync(string path)
    {
        if (!File.Exists(path)) return null;
        string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return Deserialize(json);
    }

    private static void RestoreParentLinks(List<EditorGameObject> objects, EditorGameObject? parent)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].Parent = parent;
            RestoreParentLinks(objects[i].Children, objects[i]);
        }
    }
}
