namespace MonoGame.Editor.Core.Events;

/// <summary>Published when the active scene's dirty state changes.</summary>
public sealed record SceneDirtyChangedEvent(bool IsDirty) : IEditorEvent;
