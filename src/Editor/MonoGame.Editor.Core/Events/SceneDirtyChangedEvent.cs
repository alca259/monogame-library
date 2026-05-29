namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando cambia el estado de modificación de la escena activa.</summary>
public sealed record SceneDirtyChangedEvent(bool IsDirty) : IEditorEvent;
