namespace MonoGame.Editor.Core.CodeGen;

/// <summary>Generates C# source files from editor scene and behaviour data.</summary>
public interface ICodeGenService
{
    /// <summary>
    /// Generates or overwrites the partial class initializer for <paramref name="scene"/>.
    /// Output: <c>{GameSourcePath}/{GeneratedFolder}/Scenes/{SceneName}Scene.Generated.cs</c>
    /// </summary>
    Task<CodeGenResult> GenerateSceneAsync(
        EditorScene     scene,
        EditorProject   project,
        ProjectSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>Scaffolds a new <c>GameBehaviour</c> subclass skeleton file.</summary>
    Task<CodeGenResult> GenerateBehaviourSkeletonAsync(
        string                  className,
        string                  namespaceName,
        string                  relativeFolder,
        IReadOnlyList<string>   lifecycleMethodsToOverride,
        EditorProject           project,
        CancellationToken       cancellationToken = default);
}
