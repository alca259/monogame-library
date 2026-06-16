namespace MonoGame.Editor.Core.Events;

/// <summary>
/// Publicado cuando cambia el valor de una propiedad de cualquier <see cref="EditorBehaviour"/>
/// adjunto a un <see cref="EditorGameObject"/> (por slider, stepper o file picker del Inspector).
/// Distinto de <see cref="GameObjectSelectedEvent"/> para evitar que el Inspector reconstruya
/// todas las tarjetas de Behaviour al editar una propiedad.
/// </summary>
public sealed record GameObjectPropertyChangedEvent(EditorGameObject Target) : IEditorEvent;
