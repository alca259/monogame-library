namespace MonoGame.Editor.Core.Logging;

/// <summary>Instantánea inmutable de un único mensaje de registro.</summary>
public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Message);
