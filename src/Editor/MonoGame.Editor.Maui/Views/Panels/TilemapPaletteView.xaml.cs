namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Tilemap". El estado del toolbar (info, modo pintar/borrar) vive en
/// <see cref="TilemapPaletteViewModel"/>; el dibujado del canvas de tiles y la selección
/// por tap se mantienen en el code-behind (acoplados al <see cref="GraphicsView"/>).
/// </summary>
public sealed partial class TilemapPaletteView : ContentView
{
    internal const float CellSize = 32f;

    private readonly TilemapPaletteViewModel _vm = new();

    private EditorTileset? _tileset;
    private int _selectedTileId = -1;

    public TilemapPaletteView()
    {
        InitializeComponent();
        BindingContext = _vm;
        PaletteCanvas.Drawable = new TilePaletteDrawable(this);
        _vm.TilesetChanged += OnTilesetChanged;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }

    private void OnTilesetChanged(EditorTileset? tileset)
    {
        _tileset        = tileset;
        _selectedTileId = tileset?.FirstGid ?? -1;
        ResizePaletteCanvas();
        PaletteCanvas.Invalidate();
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

    private sealed class TilePaletteDrawable(TilemapPaletteView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Background
            canvas.FillColor = Color.FromArgb("#1E1E20");
            canvas.FillRectangle(dirtyRect);

            EditorTileset? tileset = owner._tileset;
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
                bool selected = gid == owner._selectedTileId;

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
            if (owner._selectedTileId >= tileset.FirstGid)
            {
                int localId = owner._selectedTileId - tileset.FirstGid;
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
