namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando comienza la generación de código para una escena.</summary>
public sealed record CodeGenStartedEvent(string SceneName) : IEditorEvent;
