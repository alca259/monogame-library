namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando una operación de build (Content o Solution) termina.</summary>
/// <param name="ExitCode">Código de salida del proceso. 0 = éxito.</param>
/// <param name="BuildType">Categoría de build: <c>"Content"</c>, <c>"Solution"</c> o <c>"CodeGen"</c>.</param>
public sealed record BuildFinishedEvent(int ExitCode, string BuildType) : IEditorEvent
{
    /// <summary><c>true</c> si el build finalizó con éxito (<see cref="ExitCode"/> == 0).</summary>
    public bool Success => ExitCode == 0;
}
