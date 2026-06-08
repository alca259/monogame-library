namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando cambia el panel/área del editor con el foco de entrada.</summary>
/// <param name="OldContext">Contexto antes del cambio.</param>
/// <param name="NewContext">Contexto después del cambio.</param>
public sealed record FocusChangedEvent(EditorFocusContext OldContext, EditorFocusContext NewContext) : IEditorEvent;
