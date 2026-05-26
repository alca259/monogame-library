namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a tilemap layer is selected for editing in the viewport.</summary>
public sealed record TilemapLayerSelectedEvent(
    EditorTilemapAsset Tilemap,
    EditorTileLayer? Layer) : IEditorEvent;
