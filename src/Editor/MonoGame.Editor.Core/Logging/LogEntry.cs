namespace MonoGame.Editor.Core.Logging;

/// <summary>Immutable snapshot of a single log message.</summary>
public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Message);
