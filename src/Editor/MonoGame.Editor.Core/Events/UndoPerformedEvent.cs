namespace MonoGame.Editor.Core.Events;

/// <summary>Published after an undo operation is performed.</summary>
/// <param name="Description">Human-readable description of the undone command.</param>
public sealed record UndoPerformedEvent(string Description) : IEditorEvent;
