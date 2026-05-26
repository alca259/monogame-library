namespace MonoGame.Editor.Core.Prefabs;

/// <summary>
/// Serializes and deserializes a single <see cref="EditorGameObject"/> to/from JSON
/// for use as a prefab (<c>.prefab.json</c>).
/// The <see cref="EditorGameObject.Parent"/> link is excluded from JSON and must be
/// set by the caller after instantiation.
/// </summary>
public static class PrefabSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    };

    /// <summary>Serializes <paramref name="obj"/> to an indented JSON string.</summary>
    public static string Serialize(EditorGameObject obj)
        => JsonSerializer.Serialize(obj, _options);

    /// <summary>
    /// Deserializes a game object from <paramref name="json"/> and restores child parent links.
    /// Returns <c>null</c> if the JSON is invalid or empty.
    /// </summary>
    public static EditorGameObject? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        EditorGameObject? obj = JsonSerializer.Deserialize<EditorGameObject>(json, _options);
        if (obj is not null)
            RestoreParentLinks(obj.Children, obj);
        return obj;
    }

    /// <summary>Saves <paramref name="obj"/> to the file at <paramref name="path"/>.</summary>
    public static void Save(EditorGameObject obj, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(obj));
    }

    /// <summary>Loads and deserializes a game object from the file at <paramref name="path"/>.</summary>
    public static EditorGameObject? Load(string path)
    {
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        return Deserialize(json);
    }

    private static void RestoreParentLinks(List<EditorGameObject> children, EditorGameObject parent)
    {
        for (int i = 0; i < children.Count; i++)
        {
            children[i].Parent = parent;
            RestoreParentLinks(children[i].Children, children[i]);
        }
    }
}
