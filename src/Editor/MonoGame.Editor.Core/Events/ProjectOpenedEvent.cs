namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a project is opened or closed.</summary>
/// <param name="Project">The newly active project, or <c>null</c> when no project is active.</param>
public sealed record ProjectOpenedEvent(EditorProject? Project) : IEditorEvent;
