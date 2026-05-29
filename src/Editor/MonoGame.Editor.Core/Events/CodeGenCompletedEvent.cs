namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando finaliza la generación de código (con éxito o con error).</summary>
public sealed record CodeGenCompletedEvent(CodeGenResult Result) : IEditorEvent;
