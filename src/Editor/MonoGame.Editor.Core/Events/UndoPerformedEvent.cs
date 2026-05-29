namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado después de que se ejecuta una operación de deshacer.</summary>
/// <param name="Description">Descripción legible del comando deshecho.</param>
public sealed record UndoPerformedEvent(string Description) : IEditorEvent;
