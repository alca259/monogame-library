namespace MonoGame.Editor.Core.Preferences;

/// <summary>Editor layout and session preferences, persisted to disk between sessions.</summary>
public sealed class EditorPreferences
{
    private const int MaxRecentProjects = 10;

    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MonoGameEditor",
        "preferences.json");

    private readonly string _preferencesPath;

    /// <summary>Initializes preferences using the default <c>%APPDATA%/MonoGameEditor/</c> path.</summary>
    public EditorPreferences() : this(DefaultPath) { }

    /// <summary>Initializes preferences using a custom path (used in tests).</summary>
    internal EditorPreferences(string preferencesPath)
    {
        _preferencesPath = preferencesPath;
    }

    /// <summary>Width of the left (hierarchy/asset browser) panel in pixels.</summary>
    public int LeftPanelWidth { get; set; } = 220;

    /// <summary>Width of the right (inspector) panel in pixels.</summary>
    public int RightPanelWidth { get; set; } = 280;

    /// <summary>Height of the bottom console panel in pixels.</summary>
    public int ConsolePanelHeight { get; set; } = 150;

    /// <summary>Whether the hierarchy panel is visible.</summary>
    public bool HierarchyVisible { get; set; } = true;

    /// <summary>Whether the inspector panel is visible.</summary>
    public bool InspectorVisible { get; set; } = true;

    /// <summary>Whether the asset browser panel is visible.</summary>
    public bool AssetBrowserVisible { get; set; } = true;

    /// <summary>Whether the console panel is visible.</summary>
    public bool ConsoleVisible { get; set; } = true;

    /// <summary>Whether the scene manager panel is visible.</summary>
    public bool SceneManagerVisible { get; set; } = true;

    /// <summary>Whether the localization browser panel is visible.</summary>
    public bool LocalizationBrowserVisible { get; set; } = false;

    /// <summary>Whether the input map editor panel is visible.</summary>
    public bool InputMapEditorVisible { get; set; } = false;

    /// <summary>Whether the tilemap palette panel is visible.</summary>
    public bool TilemapPaletteVisible { get; set; } = false;

    /// <summary>Whether the undo history panel is visible.</summary>
    public bool UndoHistoryVisible { get; set; } = false;

    /// <summary>Absolute path of the last project opened, or empty if none.</summary>
    public string LastProjectPath { get; set; } = string.Empty;

    /// <summary>Ordered list of recently opened project paths (newest first, max 10 entries).</summary>
    public List<string> RecentProjects { get; set; } = [];

    /// <summary>Width of the folder tree inside the asset browser panel in pixels.</summary>
    public int AssetBrowserSplitterDistance { get; set; } = 180;

    /// <summary>Persists which behaviour sections in the inspector are collapsed. Key = section name.</summary>
    public Dictionary<string, bool> BehaviourSectionCollapsed { get; set; } = [];

    /// <summary>
    /// Adds <paramref name="path"/> to the front of <see cref="RecentProjects"/>, removes any
    /// duplicate entry, and trims the list to <see cref="MaxRecentProjects"/> items. Persists immediately.
    /// </summary>
    public void AddRecentProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentProjects.Remove(path);
        RecentProjects.Insert(0, path);
        if (RecentProjects.Count > MaxRecentProjects)
            RecentProjects.RemoveRange(MaxRecentProjects, RecentProjects.Count - MaxRecentProjects);
        Save();
    }

    /// <summary>Serializes current preferences to disk.</summary>
    public void Save()
    {
        string dir = Path.GetDirectoryName(_preferencesPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_preferencesPath, JsonSerializer.Serialize(this, EditorPreferencesJsonContext.Default.EditorPreferences));
    }

    /// <summary>Loads preferences from disk into this instance. Does nothing if the file does not exist.</summary>
    public void Load()
    {
        if (!File.Exists(_preferencesPath))
            return;

        try
        {
            EditorPreferences? loaded = JsonSerializer.Deserialize(
                File.ReadAllText(_preferencesPath),
                EditorPreferencesJsonContext.Default.EditorPreferences);

            if (loaded is null)
                return;

            LeftPanelWidth = loaded.LeftPanelWidth;
            RightPanelWidth = loaded.RightPanelWidth;
            ConsolePanelHeight = loaded.ConsolePanelHeight;
            HierarchyVisible = loaded.HierarchyVisible;
            InspectorVisible = loaded.InspectorVisible;
            AssetBrowserVisible = loaded.AssetBrowserVisible;
            ConsoleVisible = loaded.ConsoleVisible;
            SceneManagerVisible = loaded.SceneManagerVisible;
            LocalizationBrowserVisible = loaded.LocalizationBrowserVisible;
            InputMapEditorVisible = loaded.InputMapEditorVisible;
            TilemapPaletteVisible = loaded.TilemapPaletteVisible;
            UndoHistoryVisible = loaded.UndoHistoryVisible;
            LastProjectPath = loaded.LastProjectPath;
            AssetBrowserSplitterDistance = loaded.AssetBrowserSplitterDistance;
            BehaviourSectionCollapsed.Clear();
            foreach (System.Collections.Generic.KeyValuePair<string, bool> kv in loaded.BehaviourSectionCollapsed)
                BehaviourSectionCollapsed[kv.Key] = kv.Value;
            RecentProjects.Clear();
            RecentProjects.AddRange(loaded.RecentProjects);
        }
        catch (JsonException) { }
    }
}

[JsonSerializable(typeof(EditorPreferences))]
internal sealed partial class EditorPreferencesJsonContext : JsonSerializerContext { }
