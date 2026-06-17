using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel inspector de sprites: edita los metadatos NineSlice de un <c>.sprite.json</c>
/// seleccionado desde el Asset Browser.
/// </summary>
internal sealed class SpriteInspectorPanel : UserControl
{
    private readonly SpriteInspectorViewModel _vm = new();

    private readonly Label          _lblTitle;
    private readonly TextBox        _txtTexture;
    private readonly NumericUpDown  _numLeft;
    private readonly NumericUpDown  _numRight;
    private readonly NumericUpDown  _numTop;
    private readonly NumericUpDown  _numBottom;
    private readonly CheckBox       _chkEdges;
    private readonly CheckBox       _chkCenter;
    private readonly Button         _btnSave;
    private readonly Label          _lblStatus;

    private bool _loading;

    public SpriteInspectorPanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        // ── Título ────────────────────────────────────────────────────────────
        _lblTitle = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.AccentBlue,
            Font      = EditorFonts.PrimaryBold,
            Text      = "No sprite selected",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
        };

        // ── Barra de estado ────────────────────────────────────────────────────
        _lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
        };

        // ── Save button ────────────────────────────────────────────────────────
        _btnSave = new Button
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Text      = "Save",
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.AccentBlue,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
            Enabled   = false,
        };
        _btnSave.FlatAppearance.BorderSize = 0;

        // ── Campos ────────────────────────────────────────────────────────────
        Panel body = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Padding   = new Padding(8),
            AutoScroll = true,
        };

        ToolTip tip = new();
        _txtTexture = MakeTextBox();
        _numLeft    = MakeNumeric();
        _numRight   = MakeNumeric();
        _numTop     = MakeNumeric();
        _numBottom  = MakeNumeric();
        _chkEdges   = MakeCheckBox("Tile Edges");
        _chkCenter  = MakeCheckBox("Tile Center");

        Button btnBrowse = new()
        {
            Text      = "…",
            Width     = 28,
            Height    = 22,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextPrimary,
        };
        btnBrowse.FlatAppearance.BorderColor = EditorColors.Border;
        tip.SetToolTip(btnBrowse, "Browse for texture file");

        Panel texRow = new() { Dock = DockStyle.Top, Height = 26, BackColor = EditorColors.PanelBackground };
        texRow.Controls.Add(btnBrowse);
        texRow.Controls.Add(_txtTexture);
        btnBrowse.Dock = DockStyle.Right;
        _txtTexture.Dock = DockStyle.Fill;

        TableLayoutPanel grid = new()
        {
            Dock        = DockStyle.Top,
            ColumnCount = 2,
            RowCount    = 7,
            AutoSize    = true,
            BackColor   = EditorColors.PanelBackground,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(grid, 0, "Texture",      texRow);
        AddRow(grid, 1, "Border Left",  _numLeft);
        AddRow(grid, 2, "Border Right", _numRight);
        AddRow(grid, 3, "Border Top",   _numTop);
        AddRow(grid, 4, "Border Bottom",_numBottom);
        AddRow(grid, 5, string.Empty,   _chkEdges);
        AddRow(grid, 6, string.Empty,   _chkCenter);

        body.Controls.Add(grid);

        Controls.Add(body);
        Controls.Add(_lblTitle);
        Controls.Add(_btnSave);
        Controls.Add(_lblStatus);

        // ── Eventos de controles ──────────────────────────────────────────────
        _txtTexture.TextChanged += (_, _) => { if (!_loading) _vm.TexturePath = _txtTexture.Text; };
        _numLeft.ValueChanged   += (_, _) => { if (!_loading) _vm.BorderLeft   = (int)_numLeft.Value; };
        _numRight.ValueChanged  += (_, _) => { if (!_loading) _vm.BorderRight  = (int)_numRight.Value; };
        _numTop.ValueChanged    += (_, _) => { if (!_loading) _vm.BorderTop    = (int)_numTop.Value; };
        _numBottom.ValueChanged += (_, _) => { if (!_loading) _vm.BorderBottom = (int)_numBottom.Value; };
        _chkEdges.CheckedChanged  += (_, _) => { if (!_loading) _vm.TileEdges  = _chkEdges.Checked; };
        _chkCenter.CheckedChanged += (_, _) => { if (!_loading) _vm.TileCenter = _chkCenter.Checked; };

        btnBrowse.Click += (_, _) =>
        {
            string? path = WinFormsDialogService.PickFile(FindForm(), filter: "Texture files|*.png;*.jpg;*.bmp|All files|*.*");
            if (path is not null) { _vm.TexturePath = path; _txtTexture.Text = path; }
        };

        _btnSave.Click += (_, _) => _vm.Save();

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.FormUpdated += OnFormUpdated;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Rebuild ────────────────────────────────────────────────────────────────

    private void OnFormUpdated()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnFormUpdated); return; }

        _loading = true;
        _lblTitle.Text      = _vm.SpriteName;
        _txtTexture.Text    = _vm.TexturePath;
        _numLeft.Value      = _vm.BorderLeft;
        _numRight.Value     = _vm.BorderRight;
        _numTop.Value       = _vm.BorderTop;
        _numBottom.Value    = _vm.BorderBottom;
        _chkEdges.Checked   = _vm.TileEdges;
        _chkCenter.Checked  = _vm.TileCenter;
        _btnSave.Enabled    = _vm.CanSave;
        _lblStatus.Text     = _vm.StatusText;
        _loading = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TextBox MakeTextBox() => new()
    {
        Font        = EditorFonts.Small,
        BackColor   = EditorColors.InputBackground,
        ForeColor   = EditorColors.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private static NumericUpDown MakeNumeric() => new()
    {
        Minimum   = 0,
        Maximum   = 4096,
        Font      = EditorFonts.Small,
        BackColor = EditorColors.InputBackground,
        ForeColor = EditorColors.TextPrimary,
    };

    private static CheckBox MakeCheckBox(string text) => new()
    {
        Text      = text,
        AutoSize  = true,
        ForeColor = EditorColors.TextPrimary,
        Font      = EditorFonts.Small,
    };

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control control)
    {
        if (!string.IsNullOrEmpty(label))
        {
            Label lbl = new()
            {
                Text      = label,
                ForeColor = EditorColors.TextSecondary,
                Font      = EditorFonts.Small,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Fill,
            };
            grid.Controls.Add(lbl, 0, row);
        }

        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, 1, row);
    }
}
