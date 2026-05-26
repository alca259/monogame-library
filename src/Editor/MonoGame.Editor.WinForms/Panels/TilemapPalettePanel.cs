using System.Drawing;
using System.Windows.Forms;
using SDColor = System.Drawing.Color;
using SDRectangle = System.Drawing.Rectangle;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Panel that shows the tile palette for editing a tilemap.
/// Provides layer selection and tile picking for use with <see cref="PaintTileCommand"/>.
/// </summary>
public sealed class TilemapPalettePanel : Panel
{
    private readonly Label _lblLayer;
    private readonly ComboBox _cboLayer;
    private readonly Panel _scrollPanel;
    private readonly PictureBox _pbTileset;

    private EditorContext? _context;
    private IEditorEventBus? _eventBus;
    private EditorTilemapAsset? _currentTilemap;
    private Bitmap? _tilesetBitmap;
    private int _selectedTileGid = -1;
    private SDRectangle _hoverRect;
    private SDRectangle _selectedRect;

    /// <summary>Gets the currently selected global tile ID, or <c>-1</c> when nothing is selected.</summary>
    public int SelectedTileGid => _selectedTileGid;

    /// <summary>Gets the active layer selected in the layer dropdown.</summary>
    public EditorTileLayer? ActiveLayer =>
        _cboLayer.SelectedIndex >= 0 && _currentTilemap is not null
            ? _currentTilemap.Layers[_cboLayer.SelectedIndex]
            : null;

    /// <summary>Initializes a new instance of <see cref="TilemapPalettePanel"/>.</summary>
    public TilemapPalettePanel()
    {
        _lblLayer = new Label
        {
            Text = "Layer:",
            AutoSize = true,
            Padding = new Padding(0, 4, 4, 0),
        };

        _cboLayer = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
        };

        var layerBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(4, 4, 4, 2),
        };
        layerBar.Controls.Add(_lblLayer);
        layerBar.Controls.Add(_cboLayer);

        _pbTileset = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
        };

        _scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
        };
        _scrollPanel.Controls.Add(_pbTileset);

        // Fill first so Top panel gets priority
        Controls.Add(_scrollPanel);
        Controls.Add(layerBar);

        _cboLayer.SelectedIndexChanged += OnLayerChanged;
        _pbTileset.MouseClick += OnTilesetClick;
        _pbTileset.MouseMove += OnTilesetMouseMove;
        _pbTileset.MouseLeave += OnTilesetMouseLeave;
        _pbTileset.Paint += OnTilesetPaint;
    }

    /// <summary>Wires up the panel to the editor event bus and context.</summary>
    public void Initialize(EditorContext context, IEditorEventBus eventBus)
    {
        _context = context;
        _eventBus = eventBus;
        eventBus.Subscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
    }

    /// <summary>Loads a tilemap asset and populates the layer list and tile palette.</summary>
    public void LoadTilemap(EditorTilemapAsset tilemap)
    {
        _currentTilemap = tilemap;

        _cboLayer.Items.Clear();
        foreach (var layer in tilemap.Layers)
            _cboLayer.Items.Add(layer.Name);

        if (_cboLayer.Items.Count > 0)
            _cboLayer.SelectedIndex = 0;

        LoadFirstTilesetImage();
    }

    private void LoadFirstTilesetImage()
    {
        _tilesetBitmap?.Dispose();
        _tilesetBitmap = null;
        _pbTileset.Image = null;

        if (_currentTilemap?.Tilesets.Count > 0)
        {
            var tileset = _currentTilemap.Tilesets[0];
            string? tmxDir = Path.GetDirectoryName(_currentTilemap.FilePath);
            if (tmxDir is not null && !string.IsNullOrEmpty(tileset.ImagePath))
            {
                string imgPath = Path.Combine(tmxDir, tileset.ImagePath);
                if (File.Exists(imgPath))
                {
                    _tilesetBitmap = new Bitmap(imgPath);
                    _pbTileset.Image = _tilesetBitmap;
                }
            }
        }

        _selectedTileGid = -1;
        _selectedRect = SDRectangle.Empty;
        _hoverRect = SDRectangle.Empty;
        _pbTileset.Invalidate();
    }

    private void OnLayerChanged(object? sender, EventArgs e)
    {
        if (_currentTilemap is null) return;
        int idx = _cboLayer.SelectedIndex;
        var layer = idx >= 0 ? _currentTilemap.Layers[idx] : null;
        _eventBus?.Publish(new TilemapLayerSelectedEvent(_currentTilemap, layer));
    }

    private void OnGameObjectSelected(GameObjectSelectedEvent e)
    {
        // Integration point: future phases will check if the selected object has a
        // TiledMapRenderer and automatically load its tilemap here.
    }

    private void OnTilesetClick(object? sender, MouseEventArgs e)
    {
        if (_currentTilemap?.Tilesets.Count is not > 0) return;

        var tileset = _currentTilemap.Tilesets[0];
        if (tileset.TileWidth <= 0 || tileset.TileHeight <= 0) return;

        int col = e.X / tileset.TileWidth;
        int row = e.Y / tileset.TileHeight;
        int localId = row * tileset.Columns + col;

        if (localId < 0 || localId >= tileset.TileCount) return;

        _selectedTileGid = tileset.FirstGid + localId;
        _selectedRect = new SDRectangle(
            col * tileset.TileWidth,
            row * tileset.TileHeight,
            tileset.TileWidth,
            tileset.TileHeight);

        _pbTileset.Invalidate();
    }

    private void OnTilesetMouseMove(object? sender, MouseEventArgs e)
    {
        if (_currentTilemap?.Tilesets.Count is not > 0) return;

        var tileset = _currentTilemap.Tilesets[0];
        if (tileset.TileWidth <= 0 || tileset.TileHeight <= 0) return;

        int col = e.X / tileset.TileWidth;
        int row = e.Y / tileset.TileHeight;
        _hoverRect = new SDRectangle(
            col * tileset.TileWidth,
            row * tileset.TileHeight,
            tileset.TileWidth,
            tileset.TileHeight);

        _pbTileset.Invalidate();
    }

    private void OnTilesetMouseLeave(object? sender, EventArgs e)
    {
        _hoverRect = SDRectangle.Empty;
        _pbTileset.Invalidate();
    }

    private void OnTilesetPaint(object? sender, PaintEventArgs e)
    {
        if (_currentTilemap?.Tilesets.Count is not > 0) return;

        var g = e.Graphics;

        if (!_hoverRect.IsEmpty)
        {
            using var hoverBrush = new SolidBrush(SDColor.FromArgb(60, SDColor.White));
            g.FillRectangle(hoverBrush, _hoverRect);
        }

        if (!_selectedRect.IsEmpty)
        {
            using var selPen = new Pen(SDColor.Yellow, 2f);
            g.DrawRectangle(selPen, _selectedRect);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tilesetBitmap?.Dispose();
            if (_eventBus is not null)
                _eventBus.Unsubscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
        }
        base.Dispose(disposing);
    }
}
