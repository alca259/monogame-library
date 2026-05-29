namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Genera archivos fuente C# a partir de datos de escenas y comportamientos del editor.</summary>
public interface ICodeGenService
{
    /// <summary>
    /// Genera o sobreescribe el inicializador de la clase parcial para <paramref name="scene"/>.
    /// Salida: <c>{GameSourcePath}/{GeneratedFolder}/Scenes/{SceneName}Scene.Generated.cs</c>
    /// </summary>
    Task<CodeGenResult> GenerateSceneAsync(
        EditorScene     scene,
        EditorProject   project,
        ProjectSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>Genera el esqueleto de un nuevo archivo de subclase de <c>GameBehaviour</c>.</summary>
    Task<CodeGenResult> GenerateBehaviourSkeletonAsync(
        string                  className,
        string                  namespaceName,
        string                  relativeFolder,
        IReadOnlyList<string>   lifecycleMethodsToOverride,
        EditorProject           project,
        CancellationToken       cancellationToken = default);
}
