namespace MonoGame.Editor.Winforms.ViewModels;

/// <summary>Resultado del diálogo de creación de proyecto nuevo.</summary>
public sealed record NewProjectResult(string ProjectName, string ParentPath, string GameCsprojPath);

/// <summary>Resultado del diálogo de creación de escena nueva.</summary>
public sealed record NewSceneResult(string SceneName, float WorldWidth, float WorldHeight);

/// <summary>
/// Callbacks para reportar progreso al formulario de generación de código.
/// El VM no depende de ninguna clase Form concreta.
/// </summary>
public sealed record CodeGenProgressCallbacks(
    Action<string, bool> AddFileResult,
    Action<int, int>     MarkComplete);
