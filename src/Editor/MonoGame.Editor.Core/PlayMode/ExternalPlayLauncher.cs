using System.ComponentModel;
using System.Diagnostics;

namespace MonoGame.Editor.Core.PlayMode;

/// <summary>
/// Launches and manages the external game process for play mode.
/// Replaces the embedded <c>MonoGameControl</c> approach with an OS-level subprocess.
/// </summary>
public sealed class ExternalPlayLauncher : IDisposable
{
    private Process? _process;
    private bool _disposed;

    /// <summary>Returns <c>true</c> when the game process is running.</summary>
    public bool IsRunning => _process is { HasExited: false };

    /// <summary>
    /// Builds and launches the game executable, passing an optional scene path via <c>--scene</c>.
    /// Any previous running process is stopped first.
    /// </summary>
    /// <param name="gameExePath">Absolute path to the game executable.</param>
    /// <param name="scenePath">Absolute path to the scene JSON to load on startup. Empty to load default.</param>
    /// <param name="logLine">Optional callback to receive redirected stderr lines.</param>
    public void Launch(string gameExePath, string scenePath = "", Action<string>? logLine = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Stop();

        string args = string.IsNullOrWhiteSpace(scenePath)
            ? string.Empty
            : $"--scene \"{scenePath}\"";

        ProcessStartInfo psi = new(gameExePath, args)
        {
            UseShellExecute        = false,
            RedirectStandardError  = logLine is not null,
            CreateNoWindow         = false,
        };

        _process = Process.Start(psi);

        if (logLine is not null && _process is not null)
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logLine(e.Data);
            };
    }

    /// <summary>Terminates the game process and its child processes.</summary>
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
            // process may have already exited
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
