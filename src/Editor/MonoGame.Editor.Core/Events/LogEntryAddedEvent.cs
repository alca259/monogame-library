using MonoGame.Editor.Core.Logging;

namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se añade una nueva entrada al logger del editor.</summary>
public sealed record LogEntryAddedEvent(LogEntry Entry) : IEditorEvent;
