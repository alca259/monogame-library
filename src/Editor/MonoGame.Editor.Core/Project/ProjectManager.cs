namespace MonoGame.Editor.Core.Project;

/// <summary>Creates and loads editor projects from disk.</summary>
public static class ProjectManager
{
    /// <summary>Name of the editor hidden subfolder inside the project root.</summary>
    public const string EditorFolderName = ".editor";

    /// <summary>Name of the project descriptor file written at the project root.</summary>
    public const string ProjectFileName = "project.json";

    /// <summary>Returns the absolute path to the project descriptor file for a given root.</summary>
    public static string GetProjectFilePath(string rootPath) =>
        Path.Combine(rootPath, ProjectFileName);

    /// <summary>
    /// Creates or initializes an editor project named <paramref name="name"/> inside <paramref name="parentPath"/>.
    /// If the target folder already exists (e.g. an existing game project), only the editor
    /// sub-structure is scaffolded without touching existing source files.
    /// </summary>
    /// <param name="name">Project name (used as the subfolder name).</param>
    /// <param name="parentPath">Parent directory where the project folder lives or will be created.</param>
    /// <param name="gameCsprojPath">Optional absolute path to the main game .csproj file.</param>
    /// <param name="contentRelativePath">
    /// Relative path to the content folder. When <paramref name="gameCsprojPath"/> is set,
    /// resolved relative to its directory; otherwise relative to the project root.
    /// </param>
    /// <param name="localizationRelativePath">
    /// Relative path to the localization folder. Same resolution rules as content.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when name or parentPath is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the target folder already contains an editor project (use Load instead).
    /// </exception>
    public static EditorProject Create(
        string name,
        string parentPath,
        string gameCsprojPath = "",
        string contentRelativePath = "Content",
        string localizationRelativePath = "Localization")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentPath);

        string rootPath = Path.Combine(parentPath, name);

        if (Directory.Exists(rootPath) && File.Exists(GetProjectFilePath(rootPath)))
            throw new InvalidOperationException(
                $"'{rootPath}' is already an editor project. Use 'Open Project' to open it.");

        EditorProject project = new(name, rootPath, gameCsprojPath, contentRelativePath, localizationRelativePath);

        EnsureEditorDirectories(project);

        if (!Directory.Exists(project.ContentPath))
            Directory.CreateDirectory(project.ContentPath);

        if (!Directory.Exists(project.LocalizationPath))
            Directory.CreateDirectory(project.LocalizationPath);

        WriteProjectFile(project);
        ProjectScaffolder.Scaffold(project);

        return project;
    }

    /// <summary>
    /// Loads an existing project from <paramref name="projectPath"/> by reading <c>project.json</c> in the root.
    /// Returns <c>null</c> if the folder does not contain a valid editor project file.
    /// </summary>
    public static EditorProject? Load(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string jsonPath = GetProjectFilePath(projectPath);
        if (!File.Exists(jsonPath))
            return null;

        try
        {
            string json = File.ReadAllText(jsonPath);
            ProjectFileData? data = JsonSerializer.Deserialize<ProjectFileData>(json);

            if (data is null || string.IsNullOrWhiteSpace(data.Name))
                return null;

            string gameCsprojAbs = ResolveAbsolutePath(projectPath, data.GameCsprojPath);
            string solutionAbs   = ResolveAbsolutePath(projectPath, data.SolutionPath);

            return new EditorProject(
                data.Name,
                projectPath,
                gameCsprojAbs,
                string.IsNullOrWhiteSpace(data.ContentPath) ? "Content" : data.ContentPath,
                string.IsNullOrWhiteSpace(data.LocalizationPath) ? "Localization" : data.LocalizationPath,
                solutionPath: solutionAbs,
                baseNamespace: data.BaseNamespace);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Looks for a <c>.sln</c> or <c>.slnx</c> file in <paramref name="projectPath"/>.
    /// Returns the solution name (filename without extension), or <c>null</c> if none is found.
    /// </summary>
    public static string? FindSolutionName(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string? sln = Directory.GetFiles(projectPath, "*.sln").FirstOrDefault()
                   ?? Directory.GetFiles(projectPath, "*.slnx").FirstOrDefault();

        return sln is null ? null : Path.GetFileNameWithoutExtension(sln);
    }

    /// <summary>
    /// Scans <paramref name="rootPath"/> up to 3 levels deep for the first <c>.csproj</c> file
    /// that contains a MonoGame <c>PackageReference</c>.
    /// Returns <c>null</c> if none is found.
    /// </summary>
    public static string? FindGameCsproj(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        return SearchForCsproj(rootPath, maxDepth: 3);
    }

    /// <summary>
    /// Initializes an existing MonoGame solution folder as an editor project:
    /// writes <c>project.json</c> at the root (name inferred from the solution) and creates
    /// any missing editor folders (<c>.editor/scenes/</c>, <c>.editor/prefabs/</c>) plus
    /// standard game folders (<c>Content/</c>, <c>Localization/</c>) if absent.
    /// </summary>
    /// <param name="projectPath">Path to the existing solution root.</param>
    /// <param name="gameCsprojPath">Optional explicit path to the game .csproj; auto-detected if empty.</param>
    /// <exception cref="InvalidOperationException">Thrown when no solution file is found.</exception>
    public static EditorProject Initialize(string projectPath, string gameCsprojPath = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string name = FindSolutionName(projectPath)
            ?? throw new InvalidOperationException($"No .sln or .slnx file found in '{projectPath}'.");

        if (string.IsNullOrWhiteSpace(gameCsprojPath))
            gameCsprojPath = FindGameCsproj(projectPath) ?? string.Empty;

        EditorProject project = new(name, projectPath, gameCsprojPath);

        EnsureEditorDirectories(project);

        if (!Directory.Exists(project.ContentPath))
            Directory.CreateDirectory(project.ContentPath);

        if (!Directory.Exists(project.LocalizationPath))
            Directory.CreateDirectory(project.LocalizationPath);

        WriteProjectFile(project);
        ProjectScaffolder.Scaffold(project);

        return project;
    }

    /// <summary>Persists the game .csproj path to <c>project.json</c> if it is not already set.</summary>
    public static void SaveGameCsprojPath(EditorProject project, string csprojPath)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrWhiteSpace(csprojPath)) return;

        string jsonPath = GetProjectFilePath(project.RootPath);
        if (!File.Exists(jsonPath)) return;

        try
        {
            string json = File.ReadAllText(jsonPath);
            ProjectFileData? data = JsonSerializer.Deserialize<ProjectFileData>(json);
            if (data is null) return;

            string relative = Path.GetRelativePath(project.RootPath, csprojPath);
            data.GameCsprojPath = relative;

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // non-fatal — skip persisting
        }
    }

    /// <summary>Persists the last opened scene path to <c>project.json</c>.</summary>
    public static void SaveLastOpenedScene(EditorProject project, string scenePath)
    {
        ArgumentNullException.ThrowIfNull(project);

        string jsonPath = GetProjectFilePath(project.RootPath);
        if (!File.Exists(jsonPath)) return;

        try
        {
            string json = File.ReadAllText(jsonPath);
            ProjectFileData? data = JsonSerializer.Deserialize<ProjectFileData>(json);
            if (data is null) return;

            data.LastOpenedScene = string.IsNullOrWhiteSpace(scenePath)
                ? string.Empty
                : Path.GetRelativePath(project.RootPath, scenePath);

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // non-fatal — skip persisting
        }
    }

    private static void EnsureEditorDirectories(EditorProject project)
    {
        Directory.CreateDirectory(project.ConfigPath);
        Directory.CreateDirectory(project.LogsPath);
        Directory.CreateDirectory(project.ScenesPath);
        Directory.CreateDirectory(project.PrefabsPath);
    }

    private static void WriteProjectFile(EditorProject project)
    {
        string jsonPath = GetProjectFilePath(project.RootPath);

        string gameCsprojRelative = string.IsNullOrWhiteSpace(project.GameCsprojPath)
            ? string.Empty
            : Path.GetRelativePath(project.RootPath, project.GameCsprojPath);

        string solutionRelative = string.IsNullOrWhiteSpace(project.SolutionPath)
            ? string.Empty
            : Path.GetRelativePath(project.RootPath, project.SolutionPath);

        string baseForPaths = string.IsNullOrWhiteSpace(project.GameSourcePath) ? project.RootPath : project.GameSourcePath;

        ProjectFileData data = new()
        {
            Name             = project.Name,
            BaseNamespace    = project.BaseNamespace,
            EngineVersion    = "3.8.4",
            SolutionPath     = solutionRelative,
            GameCsprojPath   = gameCsprojRelative,
            ContentPath      = Path.GetRelativePath(baseForPaths, project.ContentPath),
            LocalizationPath = Path.GetRelativePath(baseForPaths, project.LocalizationPath),
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json);
    }

    private static string ResolveAbsolutePath(string rootPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        if (Path.IsPathRooted(relativePath))
            return relativePath;

        return Path.GetFullPath(Path.Combine(rootPath, relativePath));
    }

    private static string? SearchForCsproj(string directory, int maxDepth)
    {
        if (maxDepth < 0 || !Directory.Exists(directory))
            return null;

        foreach (string file in Directory.GetFiles(directory, "*.csproj"))
        {
            try
            {
                string content = File.ReadAllText(file);
                if (content.Contains("MonoGame", StringComparison.OrdinalIgnoreCase))
                    return file;
            }
            catch (IOException)
            {
                // skip unreadable files
            }
        }

        foreach (string subDir in Directory.GetDirectories(directory))
        {
            string? found = SearchForCsproj(subDir, maxDepth - 1);
            if (found is not null)
                return found;
        }

        return null;
    }

    private sealed class ProjectFileData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("baseNamespace")]
        public string BaseNamespace { get; set; } = string.Empty;

        [JsonPropertyName("engineVersion")]
        public string EngineVersion { get; set; } = "3.8.4";

        [JsonPropertyName("solutionPath")]
        public string SolutionPath { get; set; } = string.Empty;

        [JsonPropertyName("lastOpenedScene")]
        public string LastOpenedScene { get; set; } = string.Empty;

        /// <summary>Kept for backward compatibility and optional configuration.</summary>
        [JsonPropertyName("gameCsprojPath")]
        public string GameCsprojPath { get; set; } = string.Empty;

        /// <summary>Kept for backward compatibility and optional configuration.</summary>
        [JsonPropertyName("contentPath")]
        public string ContentPath { get; set; } = "Content";

        /// <summary>Kept for backward compatibility and optional configuration.</summary>
        [JsonPropertyName("localizationPath")]
        public string LocalizationPath { get; set; } = "Localization";
    }
}
