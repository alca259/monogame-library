namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Executes code generation for a scene via <see cref="ICodeGenService"/>.
/// Undo restores the previous file content (or deletes it if the file was new).
/// </summary>
public sealed class GenerateSceneCodeCommand : IEditorCommand
{
    private readonly ICodeGenService _service;
    private readonly EditorScene     _scene;
    private readonly EditorProject   _project;
    private readonly ProjectSettings _settings;

    private string?  _backupContent;
    private bool     _wasNew;
    private string   _outputPath = string.Empty;

    /// <param name="service">Code generation service to invoke.</param>
    /// <param name="scene">Scene to generate code for.</param>
    /// <param name="project">Active editor project.</param>
    /// <param name="settings">Project settings containing namespace and output folder.</param>
    public GenerateSceneCodeCommand(
        ICodeGenService service,
        EditorScene     scene,
        EditorProject   project,
        ProjectSettings settings)
    {
        _service  = service;
        _scene    = scene;
        _project  = project;
        _settings = settings;
    }

    /// <inheritdoc/>
    public string Description => $"Generate code for scene '{_scene.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _outputPath = ComputeOutputPath();

        if (File.Exists(_outputPath))
        {
            _backupContent = File.ReadAllText(_outputPath);
            _wasNew        = false;
        }
        else
        {
            _backupContent = null;
            _wasNew        = true;
        }

        // Block until generation completes — acceptable for an explicit user action
        _service.GenerateSceneAsync(_scene, _project, _settings)
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (string.IsNullOrEmpty(_outputPath)) return;

        if (_wasNew)
        {
            if (File.Exists(_outputPath))
                File.Delete(_outputPath);
        }
        else if (_backupContent is not null)
        {
            File.WriteAllText(_outputPath, _backupContent);
        }
    }

    private string ComputeOutputPath()
    {
        if (string.IsNullOrEmpty(_project.GameSourcePath)) return string.Empty;

        return Path.Combine(
            _project.GameSourcePath,
            _settings.GeneratedCodeFolder,
            "Scenes",
            $"{_scene.Name}Scene.Generated.cs");
    }
}
