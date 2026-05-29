namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se carga o cambia una escena.</summary>
/// <param name="Scene">La escena recién activada, o <c>null</c> cuando no hay ninguna escena activa.</param>
public sealed record SceneLoadedEvent(EditorScene? Scene) : IEditorEvent;
