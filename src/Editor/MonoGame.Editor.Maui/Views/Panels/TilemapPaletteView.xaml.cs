namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Tilemap". Tile palette for painting/erasing tiles in the active tilemap layer.
/// Subscribes to <see cref="TilemapLayerSelectedEvent"/> to refresh the palette grid.
/// </summary>
public sealed partial class TilemapPaletteView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private EditorTileset? _tileset;
    private int _selectedTileId = -1;
#pragma warning disable CS0414
    private bool _eraseMode;
#pragma warning restore CS0414

    private Action<ProjectOpenedEvent>?        _onProjectOpened;
    private Action<TilemapLayerSelectedEvent>? _onLayerSelected;

    public TilemapPaletteView()
    {
        InitializeComponent();
        PaletteCanvas.Drawable = new TilePaletteDrawable(this);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _onLayerSelected = e => MainThread.BeginInvokeOnMainThread(() => OnLayerSelected(e));
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onLayerSelected);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
        if (_onLayerSelected is not null) _bus.Unsubscribe(_onLayerSelected);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _tileset       = null;
        _selectedTileId = -1;
        _eraseMode     = false;
        TilemapInfoLabel.Text           = "No tilemap layer selected";
        TileCountLabel.Text             = "0 tiles";
        PalettePlaceholderLabel.IsVisible = true;
        PaintModeBtn.IsEnabled           = false;
        EraseModeBtn.IsEnabled           = false;
        ResizePaletteCanvas();
        PaletteCanvas.Invalidate();
    }

    private void OnLayerSelected(TilemapLayerSelectedEvent e)
    {
        _tileset        = e.Layer is not null ? e.Tilemap.Tilesets.FirstOrDefault() : null;
        _selectedTileId = _tileset is not null ? _tileset.FirstGid : -1;
        _eraseMode      = false;

        bool hasLayer = e.Layer is not null && _tileset is not null;

        TilemapInfoLabel.Text = hasLayer
            ? $"{Path.GetFileNameWithoutExtension(e.Tilemap.FilePath)} › {e.Layer!.Name}"
            : "No tilemap layer selected";

        int count = _tileset?.TileCount ?? 0;
        TileCountLabel.Text = count == 1 ? "1 tile" : $"{count} tiles";

        PalettePlaceholderLabel.IsVisible = !hasLayer;
        PaintModeBtn.IsEnabled            = hasLayer;
        EraseModeBtn.IsEnabled            = hasLayer;

        ResizePaletteCanvas();
        PaletteCanvas.Invalidate();
    }

    // ── Mode buttons ──────────────────────────────────────────────────────────

    private void OnPaintModeClicked(object sender, EventArgs e)
    {
        _eraseMode = false;
        PaintModeBtn.Style = (Style)Application.Current!.Resources["ActivePillButton"];
        EraseModeBtn.Style = (Style)Application.Current!.Resources["PillButton"];
    }

    private void OnEraseModeClicked(object sender, EventArgs e)
    {
        _eraseMode = true;
        PaintModeBtn.Style = (Style)Application.Current!.Resources["PillButton"];
        EraseModeBtn.Style = (Style)Application.Current!.Resources["ActivePillButton"];
    }

    // ── Palette tap ───────────────────────────────────────────────────────────

    private void OnPaletteTapped(object sender, TappedEventArgs e)
    {
        if (_tileset is null || _tileset.Columns <= 0) return;

        Point? pos = e.GetPosition(PaletteCanvas);
        if (pos is null) return;

        int col = (int)(pos.Value.X / CellSize);
        int row = (int)(pos.Value.Y / CellSize);

        if (col < 0 || col >= _tileset.Columns) return;

        int localId = row * _tileset.Columns + col;
        if (localId < 0 || localId >= _tileset.TileCount) return;

        _selectedTileId = _tileset.FirstGid + localId;
        PaletteCanvas.Invalidate();
    }

    // ── Canvas resize ─────────────────────────────────────────────────────────

    private void ResizePaletteCanvas()
    {
        if (_tileset is null || _tileset.Columns <= 0 || _tileset.TileCount <= 0)
        {
            PaletteCanvas.HeightRequest = 200;
            return;
        }

        int rows = (int)Math.Ceiling((double)_tileset.TileCount / _tileset.Columns);
        PaletteCanvas.HeightRequest = rows * CellSize;
    }

    // ── Drawable ──────────────────────────────────────────────────────────────

    internal const float CellSize = 32f;

    private sealed class TilePaletteDrawable : IDrawable
    {
        private readonly TilemapPaletteView _owner;

        public TilePaletteDrawable(TilemapPaletteView owner) => _owner = owner;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Background
            canvas.FillColor = Color.FromArgb("#1E1E20");
            canvas.FillRectangle(dirtyRect);

            EditorTileset? tileset = _owner._tileset;
            if (tileset is null || tileset.Columns <= 0 || tileset.TileCount <= 0)
            {
                DrawGrid(canvas, dirtyRect);
                return;
            }

            int cols  = tileset.Columns;
            int count = tileset.TileCount;
            int rows  = (int)Math.Ceiling((double)count / cols);

            // Tile cells
            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x = col * CellSize;
                float y = row * CellSize;

                int gid = tileset.FirstGid + i;
                bool selected = gid == _owner._selectedTileId;

                canvas.FillColor = selected
                    ? Color.FromArgb("#2A4A7F")
                    : Color.FromArgb("#252528");
                canvas.FillRectangle(x, y, CellSize, CellSize);

                // Tile ID label
                canvas.FontSize   = 9f;
                canvas.FontColor  = Color.FromArgb("#9A9AA2");
                canvas.DrawString(gid.ToString(), x + 2, y + 2, CellSize - 4, CellSize - 4,
                                  HorizontalAlignment.Left, VerticalAlignment.Top);
            }

            // Grid lines
            canvas.StrokeColor = Color.FromArgb("#2A2A2E");
            canvas.StrokeSize  = 1f;

            for (int col = 0; col <= cols; col++)
                canvas.DrawLine(col * CellSize, 0, col * CellSize, rows * CellSize);
            for (int row = 0; row <= rows; row++)
                canvas.DrawLine(0, row * CellSize, cols * CellSize, row * CellSize);

            // Selection highlight border
            if (_owner._selectedTileId >= tileset.FirstGid)
            {
                int localId = _owner._selectedTileId - tileset.FirstGid;
                if (localId >= 0 && localId < count)
                {
                    int sc = localId % cols;
                    int sr = localId / cols;
                    canvas.StrokeColor = Color.FromArgb("#4A9EFF");
                    canvas.StrokeSize  = 2f;
                    canvas.DrawRectangle(sc * CellSize, sr * CellSize, CellSize, CellSize);
                }
            }
        }

        private static void DrawGrid(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color.FromArgb("#2A2A2E");
            canvas.StrokeSize  = 1f;
            for (float x = 0; x < dirtyRect.Width; x += CellSize)
                canvas.DrawLine(x, 0, x, dirtyRect.Height);
            for (float y = 0; y < dirtyRect.Height; y += CellSize)
                canvas.DrawLine(0, y, dirtyRect.Width, y);
        }
    }
}
