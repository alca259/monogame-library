namespace MonoGame.Editor.Core.Project;

/// <summary>Represents a game project open in the editor.</summary>
public sealed class EditorProject
{
    /// <summary>Project name.</summary>
    public string Name { get; }

    /// <summary>Absolute path to the project root folder (where the .sln/.slnx lives).</summary>
    public string RootPath { get; }

    /// <summary>Absolute path to the <c>Editor/</c> subfolder — all editor-generated files live here.</summary>
    public string EditorPath { get; }

    /// <summary>Absolute path to the game's <c>Content/</c> folder (configurable, default <c>{RootPath}/Content</c>).</summary>
    public string ContentPath { get; }

    /// <summary>Absolute path to the <c>Editor/Scenes/</c> folder where scene JSON files are stored.</summary>
    public string ScenesPath { get; }

    /// <summary>Absolute path to the <c>Editor/Prefabs/</c> folder where prefab JSON files are stored.</summary>
    public string PrefabsPath { get; }

    /// <summary>Absolute path to the game's <c>Localization/</c> folder (configurable, default <c>{RootPath}/Localization</c>).</summary>
    public string LocalizationPath { get; }

    /// <summary>
    /// Absolute path to the main game <c>.csproj</c> file.
    /// Empty string if not configured.
    /// </summary>
    public string GameCsprojPath { get; }

    /// <summary>
    /// Absolute path to the directory that contains <see cref="GameCsprojPath"/>.
    /// Empty string if <see cref="GameCsprojPath"/> is not set.
    /// </summary>
    public string GameSourcePath { get; }

    /// <summary>
    /// Initializes the project with computed sub-paths derived from <paramref name="rootPath"/>.
    /// </summary>
    /// <param name="name">Project name.</param>
    /// <param name="rootPath">Absolute path to the project root.</param>
    /// <param name="gameCsprojPath">Absolute path to the main game .csproj file (optional).</param>
    /// <param name="contentRelativePath">
    /// Relative path to the game content folder.
    /// Resolved relative to <paramref name="gameCsprojPath"/>'s directory when provided,
    /// otherwise relative to <paramref name="rootPath"/>. Default: <c>Content</c>.
    /// </param>
    /// <param name="localizationRelativePath">
    /// Relative path to the localization folder.
    /// Resolved relative to <paramref name="gameCsprojPath"/>'s directory when provided,
    /// otherwise relative to <paramref name="rootPath"/>. Default: <c>Localization</c>.
    /// </param>
    public EditorProject(
        string name,
        string rootPath,
        string gameCsprojPath = "",
        string contentRelativePath = "Content",
        string localizationRelativePath = "Localization")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        Name             = name;
        RootPath         = rootPath;
        EditorPath       = Path.Combine(rootPath, "Editor");
        ScenesPath       = Path.Combine(EditorPath, "Scenes");
        PrefabsPath      = Path.Combine(EditorPath, "Prefabs");
        GameCsprojPath   = gameCsprojPath ?? string.Empty;
        GameSourcePath   = string.IsNullOrWhiteSpace(GameCsprojPath)
                               ? string.Empty
                               : Path.GetDirectoryName(GameCsprojPath) ?? string.Empty;

        string baseForPaths = string.IsNullOrWhiteSpace(GameSourcePath) ? rootPath : GameSourcePath;
        ContentPath      = Path.Combine(baseForPaths, contentRelativePath);
        LocalizationPath = Path.Combine(baseForPaths, localizationRelativePath);
    }
}
