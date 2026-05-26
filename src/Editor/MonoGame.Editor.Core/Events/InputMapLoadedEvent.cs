namespace MonoGame.Editor.Core.Events;

/// <summary>Published when an input map file has been loaded into the editor.</summary>
public sealed record InputMapLoadedEvent(InputEditorModel Model) : IEditorEvent;
