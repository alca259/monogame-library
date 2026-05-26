namespace MonoGame.Editor.Core.Events;

/// <summary>Published for each output line emitted during a game dotnet build.</summary>
public sealed record BuildOutputLineEvent(string Line, bool IsError) : IEditorEvent;
