namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se abre o cierra un proyecto.</summary>
/// <param name="Project">El proyecto recién activado, o <c>null</c> cuando no hay ningún proyecto activo.</param>
public sealed record ProjectOpenedEvent(EditorProject? Project) : IEditorEvent;
