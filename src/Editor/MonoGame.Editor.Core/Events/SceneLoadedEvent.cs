namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a scene is loaded or changed.</summary>
/// <param name="Scene">The newly active scene, or <c>null</c> when no scene is active.</param>
public sealed record SceneLoadedEvent(EditorScene? Scene) : IEditorEvent;
