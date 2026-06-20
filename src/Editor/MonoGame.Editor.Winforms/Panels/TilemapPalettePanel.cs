using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel de paleta de tiles: muestra la información del mapa/capa activos,
/// el modo borrado y un área de previsualización del tileset seleccionado.
/// </summary>
internal sealed class TilemapPalettePanel : UserControl
{
    private readonly TilemapPaletteViewModel _vm = new();

    private readonly Label   _lblInfo;
    private readonly Label   _lblTileCount;
    private readonly Button  _btnErase;
    private readonly Panel   _placeholder;

    public TilemapPalettePanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        // ── Toolbar superior ─────────────────────────────────────────────────
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4, 4, 4, 4),
        };

        _btnErase = new Button
        {
            Text      = "Erase",
            Dock      = DockStyle.Left,
            Width     = 56,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
        };
        _btnErase.FlatAppearance.BorderColor = EditorColors.Border;

        _lblTileCount = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Padding   = new Padding(0, 0, 6, 0),
        };

        toolbar.Controls.Add(_lblTileCount);
        toolbar.Controls.Add(_btnErase);

        // ── Info bar ──────────────────────────────────────────────────────────
        _lblInfo = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 22,
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            Text      = "No layer selected",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        // ── Área de paleta (placeholder) ──────────────────────────────────────
        _placeholder = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
        };

        Label placeholderLabel = new()
        {
            Dock      = DockStyle.Fill,
            Text      = "Select a tilemap layer\nto show the tile palette",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
        };
        _placeholder.Controls.Add(placeholderLabel);

        Controls.Add(_placeholder);
        Controls.Add(_lblInfo);
        Controls.Add(toolbar);

        // ── Eventos de controles ──────────────────────────────────────────────
        _btnErase.Click += (_, _) =>
        {
            _vm.ToggleEraseMode();
            UpdateEraseButton();
        };

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.TilesetChanged += OnTilesetChanged;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Handlers ───────────────────────────────────────────────────────────────

    private void OnTilesetChanged(EditorTileset? tileset)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => OnTilesetChanged(tileset)); return; }

        _lblInfo.Text      = _vm.TilemapInfoText;
        _lblTileCount.Text = _vm.TileCountText;
        _placeholder.Visible = _vm.PlaceholderVisible;
        UpdateEraseButton();
    }

    private void UpdateEraseButton()
    {
        _btnErase.BackColor = _vm.IsEraseMode ? EditorColors.AxisRed : EditorColors.PanelBackgroundAlt;
        _btnErase.ForeColor = _vm.IsEraseMode ? EditorColors.TextPrimary   : EditorColors.TextSecondary;
    }
}
