namespace MonoGame.Editor.Core.Project;

/// <summary>Configuración del editor por proyecto, persistida en <c>{EditorPath}/settings.json</c>.</summary>
public sealed class ProjectSettings
{
    private const string FileName = "settings.json";

    private static readonly JsonSerializerOptions s_opts = new() { WriteIndented = true };

    /// <summary>Espacio de nombres raíz de C# utilizado para la generación de código.</summary>
    [JsonPropertyName("rootNamespace")]
    public string RootNamespace { get; set; } = string.Empty;

    /// <summary>Nombre de la carpeta (relativa a GameSourcePath) donde se escribe el código generado.</summary>
    [JsonPropertyName("generatedCodeFolder")]
    public string GeneratedCodeFolder { get; set; } = "Generated";

    /// <summary>Cuando es <c>true</c>, el código de escena se genera automáticamente en cada guardado de escena.</summary>
    [JsonPropertyName("generateOnSave")]
    public bool GenerateOnSave { get; set; }

    /// <summary>Identificador de la configuración regional predeterminada (p. ej. "en-US").</summary>
    [JsonPropertyName("defaultLocale")]
    public string DefaultLocale { get; set; } = "en-US";

    /// <summary>Todas las configuraciones regionales que admite el juego.</summary>
    [JsonPropertyName("supportedLocales")]
    public List<string> SupportedLocales { get; set; } = ["en-US"];

    /// <summary>Configuración de MSBuild utilizada al compilar el juego (Debug / Release).</summary>
    [JsonPropertyName("buildConfiguration")]
    public string BuildConfiguration { get; set; } = "Debug";

    /// <summary>Ancho de la resolución virtual (del juego) en píxeles, usado para la previsualización con pillarbox/letterbox.</summary>
    [JsonPropertyName("virtualWidth")]
    public int VirtualWidth { get; set; } = 1920;

    /// <summary>Alto de la resolución virtual (del juego) en píxeles, usado para la previsualización con pillarbox/letterbox.</summary>
    [JsonPropertyName("virtualHeight")]
    public int VirtualHeight { get; set; } = 1080;

    /// <summary>Ruta relativa al .csproj principal del juego (relativa a la raíz del proyecto). Se almacena aquí por portabilidad.</summary>
    [JsonPropertyName("gameAppCsprojRelPath")]
    public string GameAppCsprojRelPath { get; set; } = string.Empty;

    /// <summary>Ruta relativa al .csproj de GameScripts (relativa a la raíz del proyecto).</summary>
    [JsonPropertyName("gameScriptsCsprojRelPath")]
    public string GameScriptsCsprojRelPath { get; set; } = string.Empty;

    /// <summary>Ruta relativa a la carpeta de contenido (relativa al directorio del GameAppCsproj o a la raíz del proyecto).</summary>
    [JsonPropertyName("contentRelPath")]
    public string ContentRelPath { get; set; } = "Content";

    /// <summary>Ruta relativa a la carpeta de localización (relativa al directorio del GameAppCsproj o a la raíz del proyecto).</summary>
    [JsonPropertyName("localizationRelPath")]
    public string LocalizationRelPath { get; set; } = "Localization";

    /// <summary>Tamaño de celda de la cuadrícula del editor en unidades de mundo. Se usa para el ajuste (snap) de transformaciones.</summary>
    [JsonPropertyName("gridCellSize")]
    public float GridCellSize { get; set; } = 1f;

    private static string GetPath(EditorProject project)
        => Path.Combine(project.ConfigPath, FileName);

    /// <summary>
    /// Carga la configuración desde <c>{EditorPath}/settings.json</c>.
    /// Devuelve una instancia con valores predeterminados cuando el archivo no existe.
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

    /// <summary>Persiste la instancia actual en <c>{EditorPath}/settings.json</c>.</summary>
    public async Task SaveAsync(EditorProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string path = GetPath(project);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(fs, this, s_opts).ConfigureAwait(false);
    }
}
