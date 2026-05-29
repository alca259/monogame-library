namespace MonoGame.Editor.Core.Prefabs;

/// <summary>
/// Serializa y deserializa un único <see cref="EditorGameObject"/> a/desde JSON
/// para su uso como prefab (<c>.prefab.json</c>).
/// El enlace <see cref="EditorGameObject.Parent"/> queda excluido del JSON y debe ser
/// establecido por el llamador tras la instanciación.
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

    /// <summary>Serializa <paramref name="obj"/> en una cadena JSON con sangría.</summary>
    public static string Serialize(EditorGameObject obj)
        => JsonSerializer.Serialize(obj, _options);

    /// <summary>
    /// Deserializa un objeto de juego desde <paramref name="json"/> y restaura los enlaces padre-hijo.
    /// Devuelve <c>null</c> si el JSON es inválido o está vacío.
    /// </summary>
    public static EditorGameObject? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        EditorGameObject? obj = JsonSerializer.Deserialize<EditorGameObject>(json, _options);
        if (obj is not null)
            RestoreParentLinks(obj.Children, obj);
        return obj;
    }

    /// <summary>Guarda <paramref name="obj"/> en el archivo en <paramref name="path"/>.</summary>
    public static void Save(EditorGameObject obj, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, Serialize(obj));
    }

    /// <summary>Carga y deserializa un objeto de juego desde el archivo en <paramref name="path"/>.</summary>
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
