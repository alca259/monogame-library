namespace MonoGame.Editor.Core.Events;

/// <summary>Published when an object is selected in the hierarchy or viewport.</summary>
/// <param name="GameObject">The selected object, or <c>null</c> when selection is cleared.</param>
public sealed record GameObjectSelectedEvent(EditorGameObject? GameObject) : IEditorEvent;
