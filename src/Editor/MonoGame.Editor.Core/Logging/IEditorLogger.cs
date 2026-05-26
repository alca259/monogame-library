namespace MonoGame.Editor.Core.Logging;

/// <summary>Logging interface exposed by <see cref="EditorContext"/>.</summary>
public interface IEditorLogger
{
    /// <summary>Logs a message at the specified level.</summary>
    void Log(string message, LogLevel level = LogLevel.Info);

    /// <summary>Logs a warning-level message.</summary>
    void LogWarning(string message);

    /// <summary>Logs an error-level message.</summary>
    void LogError(string message);

    /// <summary>Logs a debug-level message.</summary>
    void LogDebug(string message);
}
