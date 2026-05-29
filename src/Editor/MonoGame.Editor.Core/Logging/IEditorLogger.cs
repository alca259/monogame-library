namespace MonoGame.Editor.Core.Logging;

/// <summary>Interfaz de registro expuesta por <see cref="EditorContext"/>.</summary>
public interface IEditorLogger
{
    /// <summary>Registra un mensaje en el nivel especificado.</summary>
    void Log(string message, LogLevel level = LogLevel.Info);

    /// <summary>Registra un mensaje en nivel de advertencia.</summary>
    void LogWarning(string message);

    /// <summary>Registra un mensaje en nivel de error.</summary>
    void LogError(string message);

    /// <summary>Registra un mensaje en nivel de depuración.</summary>
    void LogDebug(string message);
}
