using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Selector de color RGBA con sliders (R/G/B/A), campo hexadecimal y swatch de previsualización.
/// </summary>
internal sealed class RgbaColorPickerForm : Form
{
    private Color _current;

    private readonly TrackBar _tbR;
    private readonly TrackBar _tbG;
    private readonly TrackBar _tbB;
    private readonly TrackBar _tbA;
    private readonly Label    _lblRVal;
    private readonly Label    _lblGVal;
    private readonly Label    _lblBVal;
    private readonly Label    _lblAVal;
    private readonly TextBox  _txtHex;
    private readonly Panel    _swatch;
    private bool              _syncing;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Abre el selector de color y devuelve el color elegido, o <c>null</c> si se cancela.</summary>
    public static Color? Show(IWin32Window? owner, Color initial)
    {
        using RgbaColorPickerForm dlg = new(initial);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg._current : null;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private RgbaColorPickerForm(Color initial)
    {
        _current        = initial;
        Text            = "Pick Color";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(340, 310);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;
        MaximizeBox     = false;
        MinimizeBox     = false;

        // ── Swatch de previsualización ────────────────────────────────────────
        _swatch = new Panel
        {
            Location  = new Point(12, 12),
            Size      = new Size(64, 56),
            BackColor = initial,
            BorderStyle = BorderStyle.FixedSingle,
        };

        // ── Hex ───────────────────────────────────────────────────────────────
        Label lblHex = MakeLabel("Hex ARGB:", new Point(88, 16));
        lblHex.Width = 72;
        _txtHex = new TextBox
        {
            Location        = new Point(164, 14),
            Width           = 120,
            BackColor       = EditorColors.InputBackground,
            ForeColor       = EditorColors.TextPrimary,
            BorderStyle     = BorderStyle.FixedSingle,
            Font            = EditorFonts.Mono,
            MaxLength       = 8,
            CharacterCasing = CharacterCasing.Upper,
        };

        // ── Sliders RGBA ──────────────────────────────────────────────────────
        (_tbR, _lblRVal) = MakeSlider(initial.R, 84);
        (_tbG, _lblGVal) = MakeSlider(initial.G, 116);
        (_tbB, _lblBVal) = MakeSlider(initial.B, 148);
        (_tbA, _lblAVal) = MakeSlider(initial.A, 180);

        // ── Botonera ──────────────────────────────────────────────────────────
        Button btnCancel = MakeDlgButton("Cancel", DialogResult.Cancel, false, new Point(244, 242));
        Button btnOk     = MakeDlgButton("OK",     DialogResult.OK,     true,  new Point(156, 242));
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[]
        {
            _swatch, lblHex, _txtHex,
            MakeLabel("R:", new Point(12, 86)), _tbR, _lblRVal,
            MakeLabel("G:", new Point(12, 118)), _tbG, _lblGVal,
            MakeLabel("B:", new Point(12, 150)), _tbB, _lblBVal,
            MakeLabel("A:", new Point(12, 182)), _tbA, _lblAVal,
            btnCancel, btnOk,
        });

        // ── Eventos ───────────────────────────────────────────────────────────
        _tbR.ValueChanged += (_, _) => OnSliderChanged();
        _tbG.ValueChanged += (_, _) => OnSliderChanged();
        _tbB.ValueChanged += (_, _) => OnSliderChanged();
        _tbA.ValueChanged += (_, _) => OnSliderChanged();
        _txtHex.LostFocus += (_, _) => ParseHex();
        _txtHex.KeyPress  += (_, e) => { if (e.KeyChar == '\r') { ParseHex(); e.Handled = true; } };

        SyncAll();
    }

    // ── Lógica ────────────────────────────────────────────────────────────────

    private void OnSliderChanged()
    {
        if (_syncing) return;
        _current = Color.FromArgb(_tbA.Value, _tbR.Value, _tbG.Value, _tbB.Value);
        _syncing = true;
        _txtHex.Text      = $"{_current.A:X2}{_current.R:X2}{_current.G:X2}{_current.B:X2}";
        _swatch.BackColor = _current;
        UpdateValueLabels();
        _syncing = false;
    }

    private void ParseHex()
    {
        if (_syncing) return;
        string raw = _txtHex.Text.Trim();
        if (raw.Length is not 6 and not 8) return;
        try
        {
            string padded = raw.PadLeft(8, 'F');
            uint v = Convert.ToUInt32(padded, 16);
            byte a = raw.Length == 8 ? (byte)((v >> 24) & 0xFF) : (byte)255;
            byte r = (byte)((v >> 16) & 0xFF);
            byte g = (byte)((v >> 8)  & 0xFF);
            byte b = (byte)(v         & 0xFF);
            _current = Color.FromArgb(a, r, g, b);
            _syncing = true;
            SyncSliders();
            _swatch.BackColor = _current;
            _syncing = false;
        }
        catch { }
    }

    private void SyncAll()
    {
        _syncing = true;
        SyncSliders();
        _txtHex.Text      = $"{_current.A:X2}{_current.R:X2}{_current.G:X2}{_current.B:X2}";
        _swatch.BackColor = _current;
        _syncing = false;
    }

    private void SyncSliders()
    {
        _tbR.Value = _current.R;
        _tbG.Value = _current.G;
        _tbB.Value = _current.B;
        _tbA.Value = _current.A;
        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        _lblRVal.Text = _current.R.ToString();
        _lblGVal.Text = _current.G.ToString();
        _lblBVal.Text = _current.B.ToString();
        _lblAVal.Text = _current.A.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (TrackBar tb, Label val) MakeSlider(int value, int y)
    {
        TrackBar tb = new()
        {
            Location      = new Point(28, y - 4),
            Width         = 248,
            Minimum       = 0,
            Maximum       = 255,
            Value         = value,
            TickFrequency = 16,
            BackColor     = EditorColors.PanelBackground,
        };
        Label lbl = new()
        {
            Location  = new Point(280, y),
            Width     = 36,
            Height    = 18,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = value.ToString(),
        };
        return (tb, lbl);
    }

    private static Label MakeLabel(string text, Point loc) => new()
    {
        Text      = text,
        Location  = loc,
        Size      = new Size(72, 20),
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Primary,
    };

    private static Button MakeDlgButton(string text, DialogResult dr, bool accent, Point loc)
    {
        Button btn = new()
        {
            Text         = text,
            Location     = loc,
            Size         = new Size(80, 28),
            FlatStyle    = FlatStyle.Flat,
            BackColor    = accent ? EditorColors.AccentBlue : EditorColors.PanelBackground,
            ForeColor    = accent ? EditorColors.TextPrimary : EditorColors.TextSecondary,
            Font         = accent ? EditorFonts.PrimaryBold : EditorFonts.Primary,
            DialogResult = dr,
        };
        if (accent) btn.FlatAppearance.BorderSize = 0;
        else btn.FlatAppearance.BorderColor = EditorColors.Border;
        return btn;
    }
}
