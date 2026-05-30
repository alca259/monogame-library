namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se ha cargado un modelo de localización desde el disco.</summary>
public sealed record LocalizationLoadedEvent(LocalizationEditorModel Model) : IEditorEvent;
