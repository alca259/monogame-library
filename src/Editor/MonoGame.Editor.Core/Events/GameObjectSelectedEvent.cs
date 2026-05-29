namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se selecciona un objeto en la jerarquía o el viewport.</summary>
/// <param name="GameObject">El objeto seleccionado, o <c>null</c> cuando se limpia la selección.</param>
public sealed record GameObjectSelectedEvent(EditorGameObject? GameObject) : IEditorEvent;
