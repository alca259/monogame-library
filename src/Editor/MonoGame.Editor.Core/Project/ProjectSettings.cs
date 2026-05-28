namespace MonoGame.Editor.Core.Project;

/// <summary>Per-project editor settings persisted to <c>{EditorPath}/settings.json</c>.</summary>
public sealed class ProjectSettings
{
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions s_opts = new() { WriteIndented = true };

    /// <summary>Root C# namespace used for code generation.</summary>
    [JsonPropertyName("rootNamespace")]
    public string RootNamespace { get; set; } = string.Empty;

    /// <summary>Folder name (relative to GameSourcePath) where generated code is written.</summary>
    [JsonPropertyName("generatedCodeFolder")]
    public string GeneratedCodeFolder { get; set; } = "Generated";

    /// <summary>When <c>true</c>, scene code is automatically generated on every scene save.</summary>
    [JsonPropertyName("generateOnSave")]
    public bool GenerateOnSave { get; set; }

    /// <summary>Default locale identifier (e.g. "en-US").</summary>
    [JsonPropertyName("defaultLocale")]
    public string DefaultLocale { get; set; } = "en-US";

    /// <summary>All locales the game supports.</summary>
    [JsonPropertyName("supportedLocales")]
    public List<string> SupportedLocales { get; set; } = ["en-US"];

    /// <summary>MSBuild configuration passed when building the game (Debug / Release).</summary>
    [JsonPropertyName("buildConfiguration")]
    public string BuildConfiguration { get; set; } = "Debug";

    /// <summary>Virtual (game) resolution width in pixels used for pillarbox/letterbox preview.</summary>
    [JsonPropertyName("virtualWidth")]
    public int VirtualWidth { get; set; } = 1920;

    /// <summary>Virtual (game) resolution height in pixels used for pillarbox/letterbox preview.</summary>
    [JsonPropertyName("virtualHeight")]
    public int VirtualHeight { get; set; } = 1080;

    /// <summary>Relative path to the main game .csproj (relative to project root). Stored here for portability.</summary>
    [JsonPropertyName("gameAppCsprojRelPath")]
    public string GameAppCsprojRelPath { get; set; } = string.Empty;

    /// <summary>Relative path to the GameScripts .csproj (relative to project root).</summary>
    [JsonPropertyName("gameScriptsCsprojRelPath")]
    public string GameScriptsCsprojRelPath { get; set; } = string.Empty;

    /// <summary>Relative path to the content folder (relative to GameAppCsproj directory or project root).</summary>
    [JsonPropertyName("contentRelPath")]
    public string ContentRelPath { get; set; } = "Content";

    /// <summary>Relative path to the localization folder (relative to GameAppCsproj directory or project root).</summary>
    [JsonPropertyName("localizationRelPath")]
    public string LocalizationRelPath { get; set; } = "Localization";

    private static string GetPath(EditorProject project)
        => Path.Combine(project.ConfigPath, FileName);

    /// <summary>
    /// Loads settings from <c>{EditorPath}/settings.json</c>.
    /// Returns a default instance when the file does not exist.
    /// </summary>
    public static async Task<ProjectSettings> LoadAsync(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string path = GetPath(project);
        if (!File.Exists(path)) return new ProjectSettings();

        await using FileStream fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ProjectSettings>(fs, s_opts).ConfigureAwait(false)
               ?? new ProjectSettings();
    }

    /// <summary>Persists the current instance to <c>{EditorPath}/settings.json</c>.</summary>
    public async Task SaveAsync(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string path = GetPath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(fs, this, s_opts).ConfigureAwait(false);
    }
}
