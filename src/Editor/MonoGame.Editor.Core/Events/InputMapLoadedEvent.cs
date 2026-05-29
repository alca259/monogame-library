namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se ha cargado un archivo de mapa de entrada en el editor.</summary>
public sealed record InputMapLoadedEvent(InputEditorModel Model) : IEditorEvent;
