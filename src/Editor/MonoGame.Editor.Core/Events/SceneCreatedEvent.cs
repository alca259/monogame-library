namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se crea una nueva escena en el editor.</summary>
public sealed record SceneCreatedEvent(EditorScene Scene) : IEditorEvent;
