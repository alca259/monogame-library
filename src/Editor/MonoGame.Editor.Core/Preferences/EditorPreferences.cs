namespace MonoGame.Editor.Core.Preferences;

/// <summary>Preferencias de diseño y sesión del editor, persistidas en disco entre sesiones.</summary>
public sealed class EditorPreferences
{
    private const int MaxRecentProjects = 10;

    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MonoGameEditor",
        "preferences.json");

    private readonly string _preferencesPath;

    /// <summary>Inicializa las preferencias usando la ruta predeterminada <c>%APPDATA%/MonoGameEditor/</c>.</summary>
    public EditorPreferences() : this(DefaultPath) { }

    /// <summary>Inicializa las preferencias usando una ruta personalizada (utilizada en pruebas).</summary>
    internal EditorPreferences(string preferencesPath)
    {
        _preferencesPath = preferencesPath;
    }

    /// <summary>Ancho del panel izquierdo (jerarquía/explorador de assets) en píxeles.</summary>
    public int LeftPanelWidth { get; set; } = 220;

    /// <summary>Ancho del panel derecho (inspector) en píxeles.</summary>
    public int RightPanelWidth { get; set; } = 280;

    /// <summary>Alto del panel de consola inferior en píxeles.</summary>
    public int ConsolePanelHeight { get; set; } = 150;

    /// <summary>Indica si el panel de jerarquía está visible.</summary>
    public bool HierarchyVisible { get; set; } = true;

    /// <summary>Indica si el panel del inspector está visible.</summary>
    public bool InspectorVisible { get; set; } = true;

    /// <summary>Indica si el panel del explorador de assets está visible.</summary>
    public bool AssetBrowserVisible { get; set; } = true;

    /// <summary>Indica si el panel de consola está visible.</summary>
    public bool ConsoleVisible { get; set; } = true;

    /// <summary>Indica si el panel del gestor de escenas está visible.</summary>
    public bool SceneManagerVisible { get; set; } = true;

    /// <summary>Indica si el panel del explorador de localización está visible.</summary>
    public bool LocalizationBrowserVisible { get; set; } = false;

    /// <summary>Indica si el panel del editor de mapas de entrada está visible.</summary>
    public bool InputMapEditorVisible { get; set; } = false;

    /// <summary>Indica si el panel de la paleta de tilemaps está visible.</summary>
    public bool TilemapPaletteVisible { get; set; } = false;

    /// <summary>Indica si el panel del historial de deshacer está visible.</summary>
    public bool UndoHistoryVisible { get; set; } = false;

    /// <summary>Indica si el panel del explorador de scripts está visible.</summary>
    public bool ScriptsBrowserVisible { get; set; } = false;

    /// <summary>Ruta absoluta del último proyecto abierto, o vacía si no hay ninguno.</summary>
    public string LastProjectPath { get; set; } = string.Empty;

    /// <summary>Lista ordenada de rutas de proyectos abiertos recientemente (el más reciente primero, máximo 10 entradas).</summary>
    public List<string> RecentProjects { get; set; } = [];

    /// <summary>Ancho del árbol de carpetas dentro del panel del explorador de assets en píxeles.</summary>
    public int AssetBrowserSplitterDistance { get; set; } = 180;

    /// <summary>Persiste qué secciones de comportamiento en el inspector están contraídas. Clave = nombre de sección.</summary>
    public Dictionary<string, bool> BehaviourSectionCollapsed { get; set; } = [];

    /// <summary>Tamaño de celda del grid visual en unidades de mundo. Debe coincidir con el paso de snap al mover.</summary>
    public int GridCellSize { get; set; } = 26;

    /// <summary>Ángulo de snap para la herramienta Rotar (en grados).</summary>
    public float SnapRotationDegrees { get; set; } = 15f;

    /// <summary>Paso de snap para la herramienta Escalar.</summary>
    public float SnapScaleStep { get; set; } = 0.1f;

    /// <summary>
    /// Agrega <paramref name="path"/> al inicio de <see cref="RecentProjects"/>, elimina cualquier
    /// entrada duplicada y recorta la lista a <see cref="MaxRecentProjects"/> elementos. Persiste inmediatamente.
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

    /// <summary>Elimina <paramref name="path"/> de <see cref="RecentProjects"/> y persiste inmediatamente.</summary>
    public void RemoveRecentProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!RecentProjects.Remove(path)) return;
        Save();
    }

    /// <summary>Serializa las preferencias actuales en disco.</summary>
    public void Save()
    {
        string dir = Path.GetDirectoryName(_preferencesPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(_preferencesPath, JsonSerializer.Serialize(this, EditorPreferencesJsonContext.Default.EditorPreferences));
    }

    /// <summary>Carga las preferencias desde disco en esta instancia. No hace nada si el archivo no existe.</summary>
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
            ScriptsBrowserVisible = loaded.ScriptsBrowserVisible;
            LastProjectPath = loaded.LastProjectPath;
            AssetBrowserSplitterDistance = loaded.AssetBrowserSplitterDistance;
            GridCellSize         = loaded.GridCellSize;
            SnapRotationDegrees  = loaded.SnapRotationDegrees;
            SnapScaleStep        = loaded.SnapScaleStep;
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
