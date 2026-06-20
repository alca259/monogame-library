using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo de configuración del mundo (física 2D, iluminación, navegación, audio).
/// Devuelve un nuevo <see cref="EditorWorldConfig"/> o <c>null</c> si se cancela.
/// </summary>
internal sealed class WorldConfigForm : Form
{
    // ── Physics 2D ────────────────────────────────────────────────────────────
    private readonly CheckBox       _chkPhysics2D;
    private readonly NumericUpDown  _numGravX;
    private readonly NumericUpDown  _numGravY;

    // ── Lighting ─────────────────────────────────────────────────────────────
    private readonly CheckBox  _chkLighting;
    private readonly Button    _btnAmbient;
    private readonly Panel     _swatchAmbient;
    private Color              _ambientColor;

    // ── Navigation ────────────────────────────────────────────────────────────
    private readonly CheckBox      _chkNav;
    private readonly NumericUpDown _numNavW;
    private readonly NumericUpDown _numNavH;
    private readonly NumericUpDown _numNavCell;
    private readonly NumericUpDown _numNavOriginX;
    private readonly NumericUpDown _numNavOriginY;

    // ── Audio ─────────────────────────────────────────────────────────────────
    private readonly CheckBox _chkAudio;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el diálogo modal y devuelve la configuración editada,
    /// o <c>null</c> si el usuario cancela.
    /// </summary>
    public static EditorWorldConfig? Show(IWin32Window? owner, EditorWorldConfig? initial)
    {
        using WorldConfigForm dlg = new(initial ?? new EditorWorldConfig());
        if (dlg.ShowDialog(owner) != DialogResult.OK) return null;
        return dlg.BuildConfig();
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private WorldConfigForm(EditorWorldConfig cfg)
    {
        Text            = "World Config";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(420, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;
        MaximizeBox     = false;
        MinimizeBox     = false;

        int[] rgba     = cfg.AmbientColorRgba ?? [0, 0, 0, 255];
        _ambientColor  = Color.FromArgb(rgba[3], rgba[0], rgba[1], rgba[2]);

        // ── ScrollPanel interior ──────────────────────────────────────────────
        Panel body = new()
        {
            Dock      = DockStyle.Fill,
            Padding   = new Padding(12, 10, 12, 0),
            BackColor = EditorColors.PanelBackground,
            AutoScroll = true,
        };

        int y = 0;

        // ── Sección: Physics 2D ───────────────────────────────────────────────
        y = AddSection(body, "Physics 2D", y);

        _chkPhysics2D = MakeCheck("Enable Physics 2D", cfg.UsePhysics2D, y); y += 26;
        body.Controls.Add(_chkPhysics2D);

        Label lblGrav = MakeFieldLabel("Gravity (X, Y):", y);
        _numGravX = MakeFloat(cfg.GravityX, -999f, 999f);
        _numGravY = MakeFloat(cfg.GravityY, -999f, 999f);
        Panel gravRow = MakeRowPanel(_numGravX, _numGravY, y); y += 28;
        body.Controls.Add(lblGrav);
        body.Controls.Add(gravRow);

        // ── Sección: Lighting ─────────────────────────────────────────────────
        y = AddSection(body, "Lighting", y + 4);

        _chkLighting = MakeCheck("Enable Lighting", cfg.UseLighting, y); y += 26;
        body.Controls.Add(_chkLighting);

        Label lblAmb = MakeFieldLabel("Ambient color:", y);
        _swatchAmbient = new Panel
        {
            Location    = new Point(120, y),
            Size        = new Size(28, 22),
            BackColor   = _ambientColor,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _btnAmbient = new Button
        {
            Text      = "Pick…",
            Location  = new Point(154, y - 1),
            Width     = 58,
            Height    = 24,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
        };
        _btnAmbient.FlatAppearance.BorderColor = EditorColors.Border;
        y += 28;
        body.Controls.Add(lblAmb);
        body.Controls.Add(_swatchAmbient);
        body.Controls.Add(_btnAmbient);

        // ── Sección: Navigation ───────────────────────────────────────────────
        y = AddSection(body, "Navigation", y + 4);

        _chkNav = MakeCheck("Enable Navigation", cfg.UseNavigation, y); y += 26;
        body.Controls.Add(_chkNav);

        Label lblNavSize = MakeFieldLabel("Grid size (W×H):", y);
        _numNavW = MakeInt(cfg.NavGridWidth,  1, 4096);
        _numNavH = MakeInt(cfg.NavGridHeight, 1, 4096);
        body.Controls.Add(lblNavSize);
        body.Controls.Add(MakeRowPanel(_numNavW, _numNavH, y)); y += 28;

        Label lblNavCell = MakeFieldLabel("Cell size:", y);
        _numNavCell = MakeFloat(cfg.NavGridCellSize, 1f, 512f);
        _numNavCell.Location = new Point(120, y);
        _numNavCell.Width    = 80;
        body.Controls.Add(lblNavCell);
        body.Controls.Add(_numNavCell); y += 28;

        Label lblNavOrigin = MakeFieldLabel("Origin (X, Y):", y);
        _numNavOriginX = MakeFloat(cfg.NavGridOriginX, -99999f, 99999f);
        _numNavOriginY = MakeFloat(cfg.NavGridOriginY, -99999f, 99999f);
        body.Controls.Add(lblNavOrigin);
        body.Controls.Add(MakeRowPanel(_numNavOriginX, _numNavOriginY, y)); y += 28;

        // ── Sección: Audio ────────────────────────────────────────────────────
        y = AddSection(body, "Audio", y + 4);

        _chkAudio = MakeCheck("Enable Audio", cfg.UseAudio, y); y += 26;
        body.Controls.Add(_chkAudio);

        // ── Botonera inferior ─────────────────────────────────────────────────
        Panel footer = MakeFooter();

        Controls.Add(body);
        Controls.Add(footer);

        // ── Eventos ───────────────────────────────────────────────────────────
        _btnAmbient.Click += (_, _) =>
        {
            Color? picked = RgbaColorPickerForm.Show(this, _ambientColor);
            if (picked is not null)
            {
                _ambientColor     = picked.Value;
                _swatchAmbient.BackColor = _ambientColor;
            }
        };
    }

    // ── Construcción del resultado ────────────────────────────────────────────

    private EditorWorldConfig BuildConfig() => new()
    {
        UsePhysics2D     = _chkPhysics2D.Checked,
        GravityX         = (float)_numGravX.Value,
        GravityY         = (float)_numGravY.Value,
        UseLighting      = _chkLighting.Checked,
        AmbientColorRgba = [_ambientColor.R, _ambientColor.G, _ambientColor.B, _ambientColor.A],
        UseNavigation    = _chkNav.Checked,
        NavGridWidth     = (int)_numNavW.Value,
        NavGridHeight    = (int)_numNavH.Value,
        NavGridCellSize  = (float)_numNavCell.Value,
        NavGridOriginX   = (float)_numNavOriginX.Value,
        NavGridOriginY   = (float)_numNavOriginY.Value,
        UseAudio         = _chkAudio.Checked,
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int AddSection(Panel body, string title, int y)
    {
        Label lbl = new()
        {
            Text      = title,
            Location  = new Point(0, y),
            Width     = 380,
            Height    = 22,
            ForeColor = EditorColors.AccentBlue,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = ContentAlignment.BottomLeft,
        };
        Panel line = new()
        {
            Location  = new Point(0, y + 22),
            Width     = 380,
            Height    = 1,
            BackColor = EditorColors.Border,
        };
        body.Controls.Add(lbl);
        body.Controls.Add(line);
        return y + 28;
    }

    private static CheckBox MakeCheck(string text, bool value, int y) => new()
    {
        Text      = text,
        Location  = new Point(8, y),
        Width     = 200,
        Height    = 22,
        Checked   = value,
        ForeColor = EditorColors.TextPrimary,
        Font      = EditorFonts.Primary,
        FlatStyle = FlatStyle.Flat,
    };

    private static Label MakeFieldLabel(string text, int y) => new()
    {
        Text      = text,
        Location  = new Point(8, y + 3),
        Width     = 108,
        Height    = 20,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Primary,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static NumericUpDown MakeFloat(float value, float min, float max) => new()
    {
        Minimum       = (decimal)min,
        Maximum       = (decimal)max,
        Value         = Math.Clamp((decimal)value, (decimal)min, (decimal)max),
        DecimalPlaces = 2,
        Increment     = 0.1m,
        BackColor     = EditorColors.InputBackground,
        ForeColor     = EditorColors.TextPrimary,
        Font          = EditorFonts.Primary,
        BorderStyle   = BorderStyle.FixedSingle,
        TextAlign     = HorizontalAlignment.Right,
    };

    private static NumericUpDown MakeInt(int value, int min, int max) => new()
    {
        Minimum       = min,
        Maximum       = max,
        Value         = Math.Clamp(value, min, max),
        DecimalPlaces = 0,
        BackColor     = EditorColors.InputBackground,
        ForeColor     = EditorColors.TextPrimary,
        Font          = EditorFonts.Primary,
        BorderStyle   = BorderStyle.FixedSingle,
        TextAlign     = HorizontalAlignment.Right,
    };

    private static Panel MakeRowPanel(NumericUpDown left, NumericUpDown right, int y)
    {
        left.Location  = new Point(0, 0);
        left.Width     = 90;
        right.Location = new Point(96, 0);
        right.Width    = 90;

        Panel row = new()
        {
            Location  = new Point(120, y),
            Width     = 190,
            Height    = 26,
            BackColor = EditorColors.PanelBackground,
        };
        row.Controls.Add(left);
        row.Controls.Add(right);
        return row;
    }

    private static Panel MakeFooter()
    {
        Panel panel = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 46,
            Padding   = new Padding(10, 8, 10, 8),
            BackColor = EditorColors.PanelBackgroundAlt,
        };
        Button ok = new()
        {
            Text         = "OK",
            Dock         = DockStyle.Right,
            Width        = 80,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            DialogResult = DialogResult.OK,
        };
        ok.FlatAppearance.BorderSize = 0;
        Button cancel = new()
        {
            Text         = "Cancel",
            Dock         = DockStyle.Right,
            Width        = 76,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.PanelBackground,
            ForeColor    = EditorColors.TextSecondary,
            Font         = EditorFonts.Primary,
            DialogResult = DialogResult.Cancel,
        };
        cancel.FlatAppearance.BorderColor = EditorColors.Border;
        panel.Controls.Add(ok);
        panel.Controls.Add(cancel);
        return panel;
    }
}
