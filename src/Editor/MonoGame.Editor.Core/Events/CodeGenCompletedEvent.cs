namespace MonoGame.Editor.Core.Events;

/// <summary>Published when code generation finishes (success or failure).</summary>
public sealed record CodeGenCompletedEvent(CodeGenResult Result) : IEditorEvent;
