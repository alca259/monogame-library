using System.Diagnostics;

namespace MonoGame.Editor.Core.Assets;

/// <summary>Invokes the MonoGame Content Builder (MGCB) and the dotnet build toolchain.</summary>
public static class MgcbRunner
{
    /// <summary>
    /// Runs <c>dotnet mgcb</c> against <paramref name="mgcbFilePath"/> and streams each
    /// output line to <paramref name="onOutput"/>. Returns the process exit code.
    /// </summary>
    /// <param name="mgcbFilePath">Absolute path to the <c>.mgcb</c> file.</param>
    /// <param name="onOutput">Callback invoked for each stdout/stderr line.</param>
    /// <param name="cancellationToken">Token to cancel the build.</param>
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
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            WorkingDirectory       = workingDir,
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
    /// Runs <c>dotnet build</c> against <paramref name="csprojPath"/> with the specified
    /// <paramref name="configuration"/> and streams each output line to <paramref name="onLine"/>.
    /// Returns the process exit code.
    /// </summary>
    /// <param name="csprojPath">Absolute path to the game <c>.csproj</c> file.</param>
    /// <param name="configuration">MSBuild configuration (e.g. "Debug" or "Release").</param>
    /// <param name="onLine">Callback invoked for each stdout/stderr line.</param>
    /// <param name="cancellationToken">Token to cancel the build.</param>
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
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            WorkingDirectory       = workingDir,
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
