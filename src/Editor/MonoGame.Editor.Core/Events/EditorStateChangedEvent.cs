namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando el editor realiza una transición entre los estados Play, Pause y Editing.</summary>
/// <param name="OldState">Estado antes de la transición.</param>
/// <param name="NewState">Estado después de la transición.</param>
public sealed record EditorStateChangedEvent(EditorState OldState, EditorState NewState) : IEditorEvent;
