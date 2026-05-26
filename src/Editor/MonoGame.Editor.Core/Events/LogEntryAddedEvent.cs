using MonoGame.Editor.Core.Logging;

namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a new log entry is added to the editor logger.</summary>
public sealed record LogEntryAddedEvent(LogEntry Entry) : IEditorEvent;
