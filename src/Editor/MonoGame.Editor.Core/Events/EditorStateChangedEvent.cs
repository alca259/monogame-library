namespace MonoGame.Editor.Core.Events;

/// <summary>Published when the editor transitions between Play, Pause, and Editing states.</summary>
/// <param name="OldState">State before the transition.</param>
/// <param name="NewState">State after the transition.</param>
public sealed record EditorStateChangedEvent(EditorState OldState, EditorState NewState) : IEditorEvent;
