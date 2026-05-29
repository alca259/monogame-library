namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado por cada línea de salida emitida durante la compilación dotnet del juego.</summary>
public sealed record BuildOutputLineEvent(string Line, bool IsError) : IEditorEvent;
