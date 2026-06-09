namespace MonoGame.Editor.Core.Project;

/// <summary>Crea y carga proyectos del editor desde disco.</summary>
public static class ProjectManager
{
    /// <summary>Nombre de la subcarpeta oculta del editor dentro de la raíz del proyecto.</summary>
    public const string EditorFolderName = ".editor";

    /// <summary>Nombre del archivo descriptor del proyecto escrito en la raíz del proyecto.</summary>
    public const string ProjectFileName = "project.json";

    /// <summary>Devuelve la ruta absoluta al archivo descriptor del proyecto para una raíz dada.</summary>
    public static string GetProjectFilePath(string rootPath) =>
        Path.Combine(rootPath, ProjectFileName);

    /// <summary>
    /// Crea o inicializa un proyecto del editor llamado <paramref name="name"/> dentro de <paramref name="parentPath"/>.
    /// Si la carpeta destino ya existe (p. ej. un proyecto de juego existente), solo se genera el
    /// subarbol del editor sin tocar los archivos fuente existentes.
    /// </summary>
    /// <param name="name">Nombre del proyecto (se usa como nombre de la subcarpeta).</param>
    /// <param name="parentPath">Directorio padre donde vive o se creará la carpeta del proyecto.</param>
    /// <param name="gameCsprojPath">Ruta absoluta opcional al archivo .csproj principal del juego.</param>
    /// <param name="contentRelativePath">
    /// Ruta relativa a la carpeta de contenido. Cuando <paramref name="gameCsprojPath"/> está definido,
    /// se resuelve relativa a su directorio; de lo contrario, relativa a la raíz del proyecto.
    /// </param>
    /// <param name="localizationRelativePath">
    /// Ruta relativa a la carpeta de localización. Mismas reglas de resolución que el contenido.
    /// </param>
    /// <exception cref="ArgumentException">Se lanza cuando name o parentPath están vacíos.</exception>
    /// <exception cref="InvalidOperationException">
    /// Se lanza cuando la carpeta destino ya contiene un proyecto del editor (usar Load en su lugar).
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
    /// Carga un proyecto existente desde <paramref name="projectPath"/> leyendo <c>project.json</c> en la raíz.
    /// Devuelve <c>null</c> si la carpeta no contiene un archivo de proyecto del editor válido.
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
            string solutionAbs = ResolveAbsolutePath(projectPath, data.SolutionPath);

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
    /// Busca un archivo <c>.sln</c> o <c>.slnx</c> en <paramref name="projectPath"/>.
    /// Devuelve el nombre de la solución (nombre de archivo sin extensión), o <c>null</c> si no se encuentra ninguno.
    /// </summary>
    public static string? FindSolutionName(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string? sln = Directory.GetFiles(projectPath, "*.sln").FirstOrDefault()
                   ?? Directory.GetFiles(projectPath, "*.slnx").FirstOrDefault();

        return sln is null ? null : Path.GetFileNameWithoutExtension(sln);
    }

    /// <summary>
    /// Examina <paramref name="rootPath"/> hasta 3 niveles de profundidad buscando el primer archivo <c>.csproj</c>
    /// que contenga una <c>PackageReference</c> de MonoGame.
    /// Devuelve <c>null</c> si no se encuentra ninguno.
    /// </summary>
    public static string? FindGameCsproj(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        return SearchForCsproj(rootPath, maxDepth: 3);
    }

    /// <summary>
    /// Inicializa una carpeta de solución MonoGame existente como proyecto del editor:
    /// escribe <c>project.json</c> en la raíz (nombre inferido a partir de la solución) y crea
    /// las carpetas del editor que falten (<c>.editor/scenes/</c>, <c>.editor/prefabs/</c>) más
    /// las carpetas estándar del juego (<c>Content/</c>, <c>Localization/</c>) si no existen.
    /// </summary>
    /// <param name="projectPath">Ruta a la raíz de la solución existente.</param>
    /// <param name="gameCsprojPath">Ruta explícita opcional al .csproj del juego; se detecta automáticamente si está vacía.</param>
    /// <exception cref="InvalidOperationException">Se lanza cuando no se encuentra ningún archivo de solución.</exception>
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

    /// <summary>Persiste la ruta al .csproj del juego en <c>project.json</c> si aún no está definida.</summary>
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
            // no crítico — omitir persistencia
        }
    }

    /// <summary>Persiste la ruta de la última escena abierta en <c>project.json</c>.</summary>
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
            // no crítico — omitir persistencia
        }
    }

    /// <summary>
    /// Devuelve la ruta absoluta de la última escena abierta registrada en <c>project.json</c>,
    /// o <see cref="string.Empty"/> si no hay ninguna o no existe el archivo.
    /// </summary>
    public static string GetLastOpenedScene(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        string jsonPath = GetProjectFilePath(projectPath);
        if (!File.Exists(jsonPath)) return string.Empty;

        try
        {
            string json = File.ReadAllText(jsonPath);
            ProjectFileData? data = JsonSerializer.Deserialize<ProjectFileData>(json);
            if (data is null || string.IsNullOrWhiteSpace(data.LastOpenedScene)) return string.Empty;
            return Path.GetFullPath(Path.Combine(projectPath, data.LastOpenedScene));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return string.Empty;
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
            Name = project.Name,
            BaseNamespace = project.BaseNamespace,
            EngineVersion = "3.8.4",
            SolutionPath = solutionRelative,
            GameCsprojPath = gameCsprojRelative,
            ContentPath = Path.GetRelativePath(baseForPaths, project.ContentPath),
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
                // omitir archivos no legibles
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

        /// <summary>Mantenido por compatibilidad con versiones anteriores y configuración opcional.</summary>
        [JsonPropertyName("gameCsprojPath")]
        public string GameCsprojPath { get; set; } = string.Empty;

        /// <summary>Mantenido por compatibilidad con versiones anteriores y configuración opcional.</summary>
        [JsonPropertyName("contentPath")]
        public string ContentPath { get; set; } = "Content";

        /// <summary>Mantenido por compatibilidad con versiones anteriores y configuración opcional.</summary>
        [JsonPropertyName("localizationPath")]
        public string LocalizationPath { get; set; } = "Localization";
    }
}
