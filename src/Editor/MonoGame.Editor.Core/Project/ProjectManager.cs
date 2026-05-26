namespace MonoGame.Editor.Core.Project;

/// <summary>Creates and loads editor projects from disk.</summary>
public static class ProjectManager
{
    /// <summary>Name of the editor subdirectory inside the project root.</summary>
    public const string EditorFolderName = "Editor";

    /// <summary>Name of the project descriptor file written inside the <c>Editor/</c> subfolder.</summary>
    public const string ProjectFileName = "project.json";

    /// <summary>Returns the absolute path to the editor project descriptor file for a given root.</summary>
    public static string GetProjectFilePath(string rootPath) =>
        Path.Combine(rootPath, EditorFolderName, ProjectFileName);

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

        Directory.CreateDirectory(project.EditorPath);
        Directory.CreateDirectory(project.ScenesPath);
        Directory.CreateDirectory(project.PrefabsPath);

        if (!Directory.Exists(project.ContentPath))
            Directory.CreateDirectory(project.ContentPath);

        if (!Directory.Exists(project.LocalizationPath))
            Directory.CreateDirectory(project.LocalizationPath);

        WriteProjectFile(project);

        return project;
    }

    /// <summary>
    /// Loads an existing project from <paramref name="projectPath"/> by reading <c>Editor/project.json</c>.
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

            return new EditorProject(
                data.Name,
                projectPath,
                gameCsprojAbs,
                string.IsNullOrWhiteSpace(data.ContentPath) ? "Content" : data.ContentPath,
                string.IsNullOrWhiteSpace(data.LocalizationPath) ? "Localization" : data.LocalizationPath);
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
    /// writes <c>Editor/project.json</c> (name inferred from the solution) and creates
    /// any missing editor folders (<c>Editor/Scenes/</c>, <c>Editor/Prefabs/</c>) plus
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

        Directory.CreateDirectory(project.EditorPath);
        Directory.CreateDirectory(project.ScenesPath);
        Directory.CreateDirectory(project.PrefabsPath);

        if (!Directory.Exists(project.ContentPath))
            Directory.CreateDirectory(project.ContentPath);

        if (!Directory.Exists(project.LocalizationPath))
            Directory.CreateDirectory(project.LocalizationPath);

        WriteProjectFile(project);

        return project;
    }

    private static void WriteProjectFile(EditorProject project)
    {
        string jsonPath = GetProjectFilePath(project.RootPath);

        string gameCsprojRelative = string.IsNullOrWhiteSpace(project.GameCsprojPath)
            ? string.Empty
            : Path.GetRelativePath(project.RootPath, project.GameCsprojPath);

        ProjectFileData data = new()
        {
            Name             = project.Name,
            GameCsprojPath   = gameCsprojRelative,
            ContentPath      = Path.GetRelativePath(
                                   string.IsNullOrWhiteSpace(project.GameSourcePath) ? project.RootPath : project.GameSourcePath,
                                   project.ContentPath),
            LocalizationPath = Path.GetRelativePath(
                                   string.IsNullOrWhiteSpace(project.GameSourcePath) ? project.RootPath : project.GameSourcePath,
                                   project.LocalizationPath),
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

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("gameCsprojPath")]
        public string GameCsprojPath { get; set; } = string.Empty;

        [JsonPropertyName("contentPath")]
        public string ContentPath { get; set; } = "Content";

        [JsonPropertyName("localizationPath")]
        public string LocalizationPath { get; set; } = "Localization";
    }
}
