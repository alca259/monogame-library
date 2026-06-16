using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Tilemap: gestiona el estado del toolbar (info de capa, número
/// de tiles, modo pintar/borrar) y notifica a la vista del tileset activo vía
/// <see cref="TilesetChanged"/>. El dibujado y la selección de tiles del canvas son
/// responsabilidad de la vista (GraphicsView).
/// </summary>
public sealed partial class TilemapPaletteViewModel : ViewModelBase
{
    /// <summary>Notifica a la vista el tileset activo (o <c>null</c>) para redibujar el canvas.</summary>
    public event Action<EditorTileset?>? TilesetChanged;

    [ObservableProperty]
    private string _tilemapInfoText = "No tilemap layer selected";

    [ObservableProperty]
    private string _tileCountText = "0 tiles";

    [ObservableProperty]
    private bool _placeholderVisible = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PaintModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(EraseModeCommand))]
    private bool _hasLayer;

    [ObservableProperty]
    private bool _isEraseMode;

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(_ => Reset());
        On<TilemapLayerSelectedEvent>(OnLayerSelected);
    }

    private void Reset()
    {
        TilemapInfoText = "No tilemap layer selected";
        TileCountText = "0 tiles";
        PlaceholderVisible = true;
        HasLayer = false;
        IsEraseMode = false;
        TilesetChanged?.Invoke(null);
    }

    private void OnLayerSelected(TilemapLayerSelectedEvent e)
    {
        EditorTileset? tileset = e.Layer is not null ? e.Tilemap.Tilesets.FirstOrDefault() : null;
        bool hasLayer = e.Layer is not null && tileset is not null;

        TilemapInfoText = hasLayer
            ? $"{Path.GetFileNameWithoutExtension(e.Tilemap.FilePath)} › {e.Layer!.Name}"
            : "No tilemap layer selected";

        int count = tileset?.TileCount ?? 0;
        TileCountText = count == 1 ? "1 tile" : $"{count} tiles";
        PlaceholderVisible = !hasLayer;
        HasLayer = hasLayer;
        IsEraseMode = false;

        TilesetChanged?.Invoke(tileset);
    }

    [RelayCommand(CanExecute = nameof(HasLayer))]
    private void PaintMode() => IsEraseMode = false;

    [RelayCommand(CanExecute = nameof(HasLayer))]
    private void EraseMode() => IsEraseMode = true;
}
