namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a game object's transform changes in the editor viewport.</summary>
public sealed record GameObjectTransformChangedEvent(EditorGameObject GameObject) : IEditorEvent;
