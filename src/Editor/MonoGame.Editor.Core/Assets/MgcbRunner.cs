using System.Diagnostics;

namespace MonoGame.Editor.Core.Assets;

/// <summary>Invoca el MonoGame Content Builder (MGCB) y la cadena de herramientas de compilación de dotnet.</summary>
public static class MgcbRunner
{
    /// <summary>
    /// Ejecuta <c>dotnet mgcb</c> contra <paramref name="mgcbFilePath"/> y transmite cada
    /// línea de salida a <paramref name="onOutput"/>. Devuelve el código de salida del proceso.
    /// </summary>
    /// <param name="mgcbFilePath">Ruta absoluta al archivo <c>.mgcb</c>.</param>
    /// <param name="onOutput">Callback invocado para cada línea de stdout/stderr.</param>
    /// <param name="cancellationToken">Token para cancelar la compilación.</param>
    public static async Task<int> RunAsync(
        string mgcbFilePath,
        Action<string> onOutput,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mgcbFilePath);
        ArgumentNullException.ThrowIfNull(onOutput);

        string workingDir = Path.GetDirectoryName(mgcbFilePath) ?? string.Empty;

        ProcessStartInfo psi = new("dotnet", $"mgcb \"{mgcbFilePath}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
        };

        using Process process = new() { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) onOutput(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) onOutput($"[ERR] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    /// <summary>
    /// Ejecuta <c>dotnet build</c> contra <paramref name="csprojPath"/> con la
    /// <paramref name="configuration"/> especificada y transmite cada línea de salida a <paramref name="onLine"/>.
    /// Devuelve el código de salida del proceso.
    /// </summary>
    /// <param name="csprojPath">Ruta absoluta al archivo <c>.csproj</c> del juego.</param>
    /// <param name="configuration">Configuración de MSBuild (p. ej. "Debug" o "Release").</param>
    /// <param name="onLine">Callback invocado para cada línea de stdout/stderr.</param>
    /// <param name="cancellationToken">Token para cancelar la compilación.</param>
    public static async Task<int> RunDotnetBuildAsync(
        string csprojPath,
        string configuration,
        Action<string> onLine,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csprojPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration);
        ArgumentNullException.ThrowIfNull(onLine);

        string workingDir = Path.GetDirectoryName(csprojPath) ?? string.Empty;
        string args = $"build \"{csprojPath}\" --configuration {configuration}";

        ProcessStartInfo psi = new("dotnet", args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
        };

        using Process process = new() { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) onLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) onLine($"[ERR] {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
