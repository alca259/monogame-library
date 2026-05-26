namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a new scene is created in the editor.</summary>
public sealed record SceneCreatedEvent(EditorScene Scene) : IEditorEvent;
