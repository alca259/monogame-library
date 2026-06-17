namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel de la paleta de tiles: expone el tileset activo y el estado del modo
/// borrado para que el panel pueda dibujar la paleta de selección.
/// </summary>
public sealed class TilemapPaletteViewModel : ViewModelBase
{
    /// <summary>Se dispara cuando el tileset cambia y la paleta necesita redibujarse.</summary>
    public event Action<EditorTileset?>? TilesetChanged;

    public string TilemapInfoText    { get; private set; } = "No layer selected";
    public string TileCountText      { get; private set; } = string.Empty;
    public bool   HasLayer           { get; private set; }
    public bool   IsEraseMode        { get; private set; }
    public bool   PlaceholderVisible => !HasLayer;

    protected override void RegisterEvents()
    {
        On<TilemapLayerSelectedEvent>(OnLayerSelected);
    }

    private void OnLayerSelected(TilemapLayerSelectedEvent e)
    {
        if (e.Layer is null)
        {
            HasLayer        = false;
            IsEraseMode     = false;
            TilemapInfoText = "No layer selected";
            TileCountText   = string.Empty;
            TilesetChanged?.Invoke(null);
            return;
        }

        HasLayer = true;
        string mapName = Path.GetFileNameWithoutExtension(e.Tilemap.FilePath);
        TilemapInfoText = $"{mapName} / {e.Layer.Name}";

        EditorTileset? tileset = e.Tilemap.Tilesets.Count > 0 ? e.Tilemap.Tilesets[0] : null;
        TileCountText = tileset is not null ? $"{tileset.TileCount} tiles" : "No tileset";
        TilesetChanged?.Invoke(tileset);
    }

    public void ToggleEraseMode() => IsEraseMode = !IsEraseMode;
}
