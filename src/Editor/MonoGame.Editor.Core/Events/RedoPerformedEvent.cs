namespace MonoGame.Editor.Core.Events;

/// <summary>Published after a redo operation is performed.</summary>
/// <param name="Description">Human-readable description of the redone command.</param>
public sealed record RedoPerformedEvent(string Description) : IEditorEvent;
