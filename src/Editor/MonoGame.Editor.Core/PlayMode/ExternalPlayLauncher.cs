using System.ComponentModel;
using System.Diagnostics;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>
/// Lanza y gestiona el proceso externo del juego para el modo play.
/// Reemplaza el enfoque integrado de <c>MonoGameControl</c> con un subproceso a nivel de sistema operativo.
/// </summary>
public sealed class ExternalPlayLauncher : IDisposable
{
    private Process? _process;
    private bool _disposed;

    /// <summary>Devuelve <c>true</c> cuando el proceso del juego está en ejecución.</summary>
    public bool IsRunning => _process is { HasExited: false };

    /// <summary>
    /// Compila y lanza el ejecutable del juego, pasando una ruta de escena opcional mediante <c>--scene</c>.
    /// Se detiene cualquier proceso en ejecución previo antes de iniciar.
    /// </summary>
    /// <param name="gameExePath">Ruta absoluta al ejecutable del juego.</param>
    /// <param name="scenePath">Ruta absoluta al JSON de escena que se cargará al inicio. Vacío para cargar la predeterminada.</param>
    /// <param name="logLine">Callback opcional para recibir las líneas de stdout y stderr redirigidas.</param>
    /// <param name="onExited">Callback invocado cuando el proceso termina (por cualquier causa).</param>
    public void Launch(string gameExePath, string scenePath = "", Action<string>? logLine = null, Action? onExited = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Stop();

        string args = string.IsNullOrWhiteSpace(scenePath)
            ? string.Empty
            : $"--scene \"{scenePath}\"";

        ProcessStartInfo psi = new(gameExePath, args)
        {
            UseShellExecute         = false,
            RedirectStandardOutput  = logLine is not null,
            RedirectStandardError   = logLine is not null,
            CreateNoWindow          = false,
        };

        _process = Process.Start(psi);

        if (_process is null) return;

        if (logLine is not null)
        {
            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logLine(e.Data);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logLine(e.Data);
            };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        if (onExited is not null)
        {
            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => onExited();
        }
    }

    /// <summary>Termina el proceso del juego y sus procesos hijos.</summary>
    public void Stop()
    {
        if (_process is null) return;

        try
        {
            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // el proceso puede haber terminado ya
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
