namespace MonoGame.Editor.Winforms.ViewModels;

/// <summary>Resultado del diálogo de creación de proyecto nuevo.</summary>
public sealed record NewProjectResult(string ProjectName, string ParentPath, string GameCsprojPath);

/// <summary>Resultado del diálogo de creación de escena nueva.</summary>
public sealed record NewSceneResult(string SceneName, float WorldWidth, float WorldHeight);
