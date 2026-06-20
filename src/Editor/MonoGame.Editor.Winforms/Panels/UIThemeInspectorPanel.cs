using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel inspector de temas de UI: edita las cinco secciones NineSlice de un
/// <c>.uitheme.json</c> seleccionado desde el Asset Browser.
/// </summary>
internal sealed class UIThemeInspectorPanel : UserControl
{
    private readonly UIThemeInspectorViewModel _vm = new();

    private readonly Label     _lblTitle;
    private readonly Label     _lblStatus;
    private readonly Button    _btnSave;
    private readonly TabControl _tabs;

    public UIThemeInspectorPanel()
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
            Text      = "No theme selected",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
        };

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

        // ── TabControl con una pestaña por sección ─────────────────────────────
        _tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Font      = EditorFonts.Small,
            BackColor = EditorColors.PanelBackground,
        };

        foreach (UIThemeSectionModel section in _vm.Sections)
            _tabs.TabPages.Add(BuildSectionTab(section));

        Controls.Add(_tabs);
        Controls.Add(_lblTitle);
        Controls.Add(_btnSave);
        Controls.Add(_lblStatus);

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

        _lblTitle.Text   = _vm.ThemeName;
        _lblStatus.Text  = _vm.StatusText;
        _btnSave.Enabled = _vm.CanSave;

        // Forzar refresco de todos los campos de sección.
        foreach (TabPage tab in _tabs.TabPages)
        {
            if (tab.Tag is (UIThemeSectionModel section, SectionControls ctrls))
                PopulateSectionControls(section, ctrls);
        }
    }

    // ── Builder de pestaña ─────────────────────────────────────────────────────

    private TabPage BuildSectionTab(UIThemeSectionModel section)
    {
        TabPage page = new(section.SectionName)
        {
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextPrimary,
        };

        Panel body = new()
        {
            Dock       = DockStyle.Fill,
            Padding    = new Padding(8),
            BackColor  = EditorColors.PanelBackground,
            AutoScroll = true,
        };

        SectionControls ctrls = BuildSectionControls(body, section);
        PopulateSectionControls(section, ctrls);

        page.Controls.Add(body);
        page.Tag = (section, ctrls);
        return page;
    }

    private SectionControls BuildSectionControls(Panel body, UIThemeSectionModel section)
    {
        ToolTip tip = new();

        TextBox txtTexture = MakeTextBox();
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
        btnBrowse.Dock   = DockStyle.Right;
        txtTexture.Dock  = DockStyle.Fill;
        texRow.Controls.Add(txtTexture);
        texRow.Controls.Add(btnBrowse);

        NumericUpDown numLeft   = MakeNumeric();
        NumericUpDown numRight  = MakeNumeric();
        NumericUpDown numTop    = MakeNumeric();
        NumericUpDown numBottom = MakeNumeric();
        CheckBox chkEdges  = new() { Text = "Tile Edges",  AutoSize = true, ForeColor = EditorColors.TextPrimary, Font = EditorFonts.Small };
        CheckBox chkCenter = new() { Text = "Tile Center", AutoSize = true, ForeColor = EditorColors.TextPrimary, Font = EditorFonts.Small };

        TableLayoutPanel grid = new()
        {
            Dock        = DockStyle.Top,
            ColumnCount = 2,
            RowCount    = 7,
            AutoSize    = true,
            BackColor   = EditorColors.PanelBackground,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddGridRow(grid, 0, "Texture",       texRow);
        AddGridRow(grid, 1, "Border Left",   numLeft);
        AddGridRow(grid, 2, "Border Right",  numRight);
        AddGridRow(grid, 3, "Border Top",    numTop);
        AddGridRow(grid, 4, "Border Bottom", numBottom);
        AddGridRow(grid, 5, string.Empty,    chkEdges);
        AddGridRow(grid, 6, string.Empty,    chkCenter);

        body.Controls.Add(grid);

        // Wire events
        bool loading = false;
        txtTexture.TextChanged += (_, _) => { if (!loading) section.TexturePath = txtTexture.Text; };
        numLeft.ValueChanged   += (_, _) => { if (!loading) section.BorderLeft   = (int)numLeft.Value; };
        numRight.ValueChanged  += (_, _) => { if (!loading) section.BorderRight  = (int)numRight.Value; };
        numTop.ValueChanged    += (_, _) => { if (!loading) section.BorderTop    = (int)numTop.Value; };
        numBottom.ValueChanged += (_, _) => { if (!loading) section.BorderBottom = (int)numBottom.Value; };
        chkEdges.CheckedChanged  += (_, _) => { if (!loading) section.TileEdges  = chkEdges.Checked; };
        chkCenter.CheckedChanged += (_, _) => { if (!loading) section.TileCenter = chkCenter.Checked; };

        btnBrowse.Click += (_, _) =>
        {
            string? path = WinFormsDialogService.PickFile(FindForm(), filter: "Texture files|*.png;*.jpg;*.bmp|All files|*.*");
            if (path is not null) { section.TexturePath = path; txtTexture.Text = path; }
        };

        return new SectionControls(txtTexture, numLeft, numRight, numTop, numBottom, chkEdges, chkCenter, () => loading = true, () => loading = false);
    }

    private static void PopulateSectionControls(UIThemeSectionModel section, SectionControls c)
    {
        c.BeginLoad();
        c.TxtTexture.Text     = section.TexturePath;
        c.NumLeft.Value       = section.BorderLeft;
        c.NumRight.Value      = section.BorderRight;
        c.NumTop.Value        = section.BorderTop;
        c.NumBottom.Value     = section.BorderBottom;
        c.ChkEdges.Checked    = section.TileEdges;
        c.ChkCenter.Checked   = section.TileCenter;
        c.EndLoad();
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
        Maximum   = 2048,
        Font      = EditorFonts.Small,
        BackColor = EditorColors.InputBackground,
        ForeColor = EditorColors.TextPrimary,
    };

    private static void AddGridRow(TableLayoutPanel grid, int row, string label, Control control)
    {
        if (!string.IsNullOrEmpty(label))
        {
            grid.Controls.Add(new Label
            {
                Text      = label,
                ForeColor = EditorColors.TextSecondary,
                Font      = EditorFonts.Small,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Fill,
            }, 0, row);
        }
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, 1, row);
    }

    // ── Helper record de controles por sección ────────────────────────────────

    private sealed record SectionControls(
        TextBox       TxtTexture,
        NumericUpDown NumLeft,
        NumericUpDown NumRight,
        NumericUpDown NumTop,
        NumericUpDown NumBottom,
        CheckBox      ChkEdges,
        CheckBox      ChkCenter,
        Action        BeginLoad,
        Action        EndLoad);
}
