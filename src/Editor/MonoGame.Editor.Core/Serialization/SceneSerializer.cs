namespace MonoGame.Editor.Core.Serialization;

/// <summary>
/// Serializes and deserializes <see cref="EditorScene"/> objects to/from JSON
/// using <see cref="System.Text.Json"/>. Parent links are excluded from JSON
/// and restored after deserialization.
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

    /// <summary>Serializes <paramref name="scene"/> to an indented JSON string.</summary>
    public static string Serialize(EditorScene scene)
        => JsonSerializer.Serialize(scene, _options);

    /// <summary>
    /// Deserializes a scene from <paramref name="json"/> and restores parent links on all objects.
    /// Returns <c>null</c> if the JSON is invalid or empty.
    /// </summary>
    public static EditorScene? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        EditorScene? scene = JsonSerializer.Deserialize<EditorScene>(json, _options);
        if (scene is not null)
            RestoreParentLinks(scene.RootGameObjects, null);
        return scene;
    }

    /// <summary>Saves <paramref name="scene"/> to the file at <paramref name="path"/>.</summary>
    public static async Task SaveAsync(EditorScene scene, string path)
    {
        string json = Serialize(scene);
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }

    /// <summary>Loads and deserializes a scene from the file at <paramref name="path"/>.</summary>
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
