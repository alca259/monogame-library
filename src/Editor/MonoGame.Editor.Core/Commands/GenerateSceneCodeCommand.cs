namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Ejecuta la generación de código para una escena a través de <see cref="ICodeGenService"/>.
/// Deshacer restaura el contenido anterior del archivo (o lo elimina si el archivo era nuevo).
/// </summary>
public sealed class GenerateSceneCodeCommand : IEditorCommand
{
    private readonly ICodeGenService _service;
    private readonly EditorScene _scene;
    private readonly EditorProject _project;
    private readonly ProjectSettings _settings;

    private string? _backupContent;
    private bool _wasNew;
    private string _outputPath = string.Empty;

    /// <param name="service">Servicio de generación de código a invocar.</param>
    /// <param name="scene">Escena para la que se generará el código.</param>
    /// <param name="project">Proyecto de editor activo.</param>
    /// <param name="settings">Configuración del proyecto que contiene el espacio de nombres y la carpeta de salida.</param>
    public GenerateSceneCodeCommand(
        ICodeGenService service,
        EditorScene scene,
        EditorProject project,
        ProjectSettings settings)
    {
        _service = service;
        _scene = scene;
        _project = project;
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
            _wasNew = false;
        }
        else
        {
            _backupContent = null;
            _wasNew = true;
        }

        // Bloquear hasta que la generación finalice — aceptable para una acción explícita del usuario
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
