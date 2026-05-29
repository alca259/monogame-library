namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se selecciona una capa de tilemap para edición en el viewport.</summary>
public sealed record TilemapLayerSelectedEvent(
    EditorTilemapAsset Tilemap,
    EditorTileLayer? Layer) : IEditorEvent;
