namespace MonoGame.Editor.Core.Events;

/// <summary>Published when code generation begins for a scene.</summary>
public sealed record CodeGenStartedEvent(string SceneName) : IEditorEvent;
