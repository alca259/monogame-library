namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado por el bucle de render cada segundo con los fotogramas por segundo actuales.</summary>
public sealed record FpsUpdatedEvent(int Fps) : IEditorEvent;
