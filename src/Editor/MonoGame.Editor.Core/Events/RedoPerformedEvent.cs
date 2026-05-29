namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado después de que se ejecuta una operación de rehacer.</summary>
/// <param name="Description">Descripción legible del comando rehecho.</param>
public sealed record RedoPerformedEvent(string Description) : IEditorEvent;
