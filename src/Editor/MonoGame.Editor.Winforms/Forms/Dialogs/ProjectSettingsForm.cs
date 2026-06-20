using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo de configuración del proyecto (General, Content, Localization, CodeGen).
/// Carga ajustes desde disco, permite editarlos y los guarda al confirmar.
/// </summary>
internal sealed class ProjectSettingsForm : Form
{
    private readonly ProjectSettings _settings;

    // ── General ───────────────────────────────────────────────────────────────
    private readonly ComboBox      _cmbBuildConfig;
    private readonly NumericUpDown _numVirtualW;
    private readonly NumericUpDown _numVirtualH;
    private readonly TextBox       _txtAppCsproj;
    private readonly TextBox       _txtScriptsCsproj;
    private readonly NumericUpDown _numGridCell;

    // ── Content ───────────────────────────────────────────────────────────────
    private readonly TextBox _txtContentRel;
    private readonly TextBox _txtLocRel;

    // ── Localization ──────────────────────────────────────────────────────────
    private readonly TextBox  _txtDefaultLocale;
    private readonly ListBox  _lstLocales;
    private readonly TextBox  _txtNewLocale;

    // ── CodeGen ───────────────────────────────────────────────────────────────
    private readonly TextBox  _txtNamespace;
    private readonly TextBox  _txtGenFolder;
    private readonly CheckBox _chkGenOnSave;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Carga los ajustes del proyecto, muestra el diálogo y guarda en caso de confirmación.
    /// </summary>
    public static async Task ShowAsync(IWin32Window? owner, EditorProject project)
    {
        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        using ProjectSettingsForm dlg = new(settings);
        if (dlg.ShowDialog(owner) == DialogResult.OK)
            await settings.SaveAsync(project).ConfigureAwait(true);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private ProjectSettingsForm(ProjectSettings settings)
    {
        _settings       = settings;
        Text            = "Project Settings";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(520, 460);
        MinimumSize     = new Size(440, 400);
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;

        // ── TabControl ────────────────────────────────────────────────────────
        TabControl tabs = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Font      = EditorFonts.Primary,
        };

        // ── Tab: General ──────────────────────────────────────────────────────
        _cmbBuildConfig   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = EditorColors.InputBackground, ForeColor = EditorColors.TextPrimary };
        _cmbBuildConfig.Items.AddRange(["Debug", "Release"]);
        _cmbBuildConfig.SelectedItem = settings.BuildConfiguration is "Release" ? "Release" : "Debug";

        _numVirtualW = MakeInt(settings.VirtualWidth,  1, 7680);
        _numVirtualH = MakeInt(settings.VirtualHeight, 1, 4320);
        _numGridCell = MakeFloat(settings.GridCellSize, 0.001f, 10000f);

        _txtAppCsproj     = MakeTxt(settings.GameAppCsprojRelPath);
        _txtScriptsCsproj = MakeTxt(settings.GameScriptsCsprojRelPath);

        Button btnPickApp     = MakeBrowse();
        Button btnPickScripts = MakeBrowse();

        TabPage tabGeneral = MakeTab("General",
            Row("Build config:",      _cmbBuildConfig),
            RowTwo("Virtual W×H:",    _numVirtualW, _numVirtualH),
            RowBrowse("App .csproj:", _txtAppCsproj, btnPickApp),
            RowBrowse("Scripts .csproj:", _txtScriptsCsproj, btnPickScripts),
            Row("Grid cell size:",    _numGridCell));

        // ── Tab: Content ──────────────────────────────────────────────────────
        _txtContentRel = MakeTxt(settings.ContentRelPath);
        _txtLocRel     = MakeTxt(settings.LocalizationRelPath);

        TabPage tabContent = MakeTab("Content",
            Row("Content path:", _txtContentRel),
            Row("Localization path:", _txtLocRel));

        // ── Tab: Localization ─────────────────────────────────────────────────
        _txtDefaultLocale = MakeTxt(settings.DefaultLocale);
        _lstLocales       = new ListBox
        {
            BackColor  = EditorColors.InputBackground,
            ForeColor  = EditorColors.TextPrimary,
            Font       = EditorFonts.Primary,
            BorderStyle = BorderStyle.FixedSingle,
            Height     = 120,
        };
        foreach (string locale in settings.SupportedLocales)
            _lstLocales.Items.Add(locale);

        _txtNewLocale = MakeTxt(string.Empty);
        _txtNewLocale.PlaceholderText = "e.g. es-ES";

        Button btnAddLocale = MakeSmallBtn("Add");
        Button btnRemLocale = MakeSmallBtn("Remove");

        Panel localeList = BuildLocalePanel();

        TabPage tabLoc = MakeTab("Localization",
            Row("Default locale:", _txtDefaultLocale),
            LabelRow("Supported locales:"),
            FullRow(localeList));

        // ── Tab: CodeGen ──────────────────────────────────────────────────────
        _txtNamespace = MakeTxt(settings.RootNamespace);
        _txtGenFolder = MakeTxt(settings.GeneratedCodeFolder);
        _chkGenOnSave = new CheckBox
        {
            Text      = "Generate on Save",
            Checked   = settings.GenerateOnSave,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.Primary,
            FlatStyle = FlatStyle.Flat,
            Height    = 22,
        };

        TabPage tabCodeGen = MakeTab("CodeGen",
            Row("Root namespace:",    _txtNamespace),
            Row("Generated folder:",  _txtGenFolder),
            FullRow(_chkGenOnSave));

        tabs.TabPages.AddRange([tabGeneral, tabContent, tabLoc, tabCodeGen]);

        // ── Botonera inferior ─────────────────────────────────────────────────
        Panel footer = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 46,
            Padding   = new Padding(10, 8, 10, 8),
            BackColor = EditorColors.PanelBackgroundAlt,
        };
        Button btnOk = new()
        {
            Text         = "Save",
            Dock         = DockStyle.Right,
            Width        = 80,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            DialogResult = DialogResult.OK,
        };
        btnOk.FlatAppearance.BorderSize = 0;
        Button btnCancel = new()
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
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;
        footer.Controls.Add(btnOk);
        footer.Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.Add(tabs);
        Controls.Add(footer);

        // ── Eventos de browse ─────────────────────────────────────────────────
        btnPickApp.Click += (_, _) =>
        {
            string? p = WinFormsDialogService.PickFile(this, null, "C# Project (*.csproj)|*.csproj|All (*.*)|*.*");
            if (p is not null) _txtAppCsproj.Text = p;
        };
        btnPickScripts.Click += (_, _) =>
        {
            string? p = WinFormsDialogService.PickFile(this, null, "C# Project (*.csproj)|*.csproj|All (*.*)|*.*");
            if (p is not null) _txtScriptsCsproj.Text = p;
        };

        // ── Eventos de locales ────────────────────────────────────────────────
        btnAddLocale.Click += (_, _) =>
        {
            string loc = _txtNewLocale.Text.Trim();
            if (!string.IsNullOrWhiteSpace(loc) && !_lstLocales.Items.Contains(loc))
            {
                _lstLocales.Items.Add(loc);
                _txtNewLocale.Clear();
            }
        };
        btnRemLocale.Click += (_, _) =>
        {
            if (_lstLocales.SelectedItem is string sel)
                _lstLocales.Items.Remove(sel);
        };

        // ── Guardar al cerrar con OK ───────────────────────────────────────────
        FormClosing += (_, e) =>
        {
            if (DialogResult == DialogResult.OK)
                ApplyToSettings();
        };
    }

    // ── Aplicar al modelo ────────────────────────────────────────────────────

    private void ApplyToSettings()
    {
        _settings.BuildConfiguration      = _cmbBuildConfig.SelectedItem as string ?? "Debug";
        _settings.VirtualWidth            = (int)_numVirtualW.Value;
        _settings.VirtualHeight           = (int)_numVirtualH.Value;
        _settings.GameAppCsprojRelPath    = _txtAppCsproj.Text.Trim();
        _settings.GameScriptsCsprojRelPath = _txtScriptsCsproj.Text.Trim();
        _settings.GridCellSize            = (float)_numGridCell.Value;
        _settings.ContentRelPath          = _txtContentRel.Text.Trim();
        _settings.LocalizationRelPath     = _txtLocRel.Text.Trim();
        _settings.DefaultLocale           = _txtDefaultLocale.Text.Trim();
        _settings.RootNamespace           = _txtNamespace.Text.Trim();
        _settings.GeneratedCodeFolder     = _txtGenFolder.Text.Trim();
        _settings.GenerateOnSave          = _chkGenOnSave.Checked;

        _settings.SupportedLocales.Clear();
        foreach (object item in _lstLocales.Items)
            _settings.SupportedLocales.Add(item.ToString() ?? string.Empty);
    }

    // ── Construcción de paneles ───────────────────────────────────────────────

    private Panel BuildLocalePanel()
    {
        Panel p = new()
        {
            Height    = 168,
            Dock      = DockStyle.Top,
            BackColor = EditorColors.PanelBackground,
        };

        _lstLocales.Dock = DockStyle.Fill;

        Panel addRow = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            BackColor = EditorColors.PanelBackground,
        };
        Button btnAdd = MakeSmallBtn("Add");
        Button btnRem = MakeSmallBtn("Remove");
        _txtNewLocale.Dock = DockStyle.Fill;
        btnAdd.Dock        = DockStyle.Right;
        btnRem.Dock        = DockStyle.Right;
        addRow.Controls.Add(_txtNewLocale);
        addRow.Controls.Add(btnAdd);
        addRow.Controls.Add(btnRem);

        p.Controls.Add(_lstLocales);
        p.Controls.Add(addRow);

        Button btnAddLocale = btnAdd;
        Button btnRemLocale = btnRem;

        btnAddLocale.Click += (_, _) =>
        {
            string loc = _txtNewLocale.Text.Trim();
            if (!string.IsNullOrWhiteSpace(loc) && !_lstLocales.Items.Contains(loc))
            {
                _lstLocales.Items.Add(loc);
                _txtNewLocale.Clear();
            }
        };
        btnRemLocale.Click += (_, _) =>
        {
            if (_lstLocales.SelectedItem is string sel)
                _lstLocales.Items.Remove(sel);
        };

        return p;
    }

    // ── Helpers de layout ─────────────────────────────────────────────────────

    private static TabPage MakeTab(string title, params Panel[] rows)
    {
        TabPage tp = new(title)
        {
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextPrimary,
            Padding   = new Padding(10),
        };
        Panel body = new() { Dock = DockStyle.Fill, BackColor = EditorColors.PanelBackground, AutoScroll = true };
        int y = 8;
        foreach (Panel row in rows)
        {
            row.Top  = y;
            row.Left = 0;
            body.Controls.Add(row);
            y += row.Height + 4;
        }
        tp.Controls.Add(body);
        return tp;
    }

    private static Panel Row(string label, Control control)
    {
        Panel p = new() { Width = 460, Height = 26, BackColor = EditorColors.PanelBackground };
        Label lbl = RowLabel(label);
        control.Dock   = DockStyle.None;
        control.Left   = 130;
        control.Top    = 2;
        control.Width  = 300;
        control.Height = 22;
        p.Controls.Add(lbl);
        p.Controls.Add(control);
        return p;
    }

    private static Panel RowTwo(string label, Control left, Control right)
    {
        Panel p = new() { Width = 460, Height = 26, BackColor = EditorColors.PanelBackground };
        p.Controls.Add(RowLabel(label));
        left.Location  = new Point(130, 2); left.Width  = 90; left.Height = 22;
        right.Location = new Point(228, 2); right.Width = 90; right.Height = 22;
        p.Controls.Add(left);
        p.Controls.Add(right);
        return p;
    }

    private static Panel RowBrowse(string label, TextBox txt, Button browse)
    {
        Panel p = new() { Width = 460, Height = 26, BackColor = EditorColors.PanelBackground };
        p.Controls.Add(RowLabel(label));
        txt.Location    = new Point(130, 2); txt.Width  = 264; txt.Height = 22;
        browse.Location = new Point(398, 1); browse.Width = 30; browse.Height = 24;
        p.Controls.Add(txt);
        p.Controls.Add(browse);
        return p;
    }

    private static Panel LabelRow(string text)
    {
        Panel p = new() { Width = 460, Height = 20, BackColor = EditorColors.PanelBackground };
        p.Controls.Add(new Label
        {
            Text = text, Location = new Point(0, 2), Width = 200,
            ForeColor = EditorColors.TextSecondary, Font = EditorFonts.Primary,
        });
        return p;
    }

    private static Panel FullRow(Control control)
    {
        Panel p = new() { Width = 460, Height = control.Height + 4, BackColor = EditorColors.PanelBackground };
        control.Location = new Point(130, 2);
        p.Controls.Add(control);
        return p;
    }

    private static Label RowLabel(string text) => new()
    {
        Text      = text,
        Location  = new Point(0, 4),
        Width     = 126,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Primary,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static TextBox MakeTxt(string value) => new()
    {
        Text        = value,
        BackColor   = EditorColors.InputBackground,
        ForeColor   = EditorColors.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Font        = EditorFonts.Primary,
    };

    private static NumericUpDown MakeInt(int value, int min, int max) => new()
    {
        Minimum = min, Maximum = max,
        Value   = Math.Clamp(value, min, max),
        DecimalPlaces = 0,
        BackColor = EditorColors.InputBackground, ForeColor = EditorColors.TextPrimary,
        Font = EditorFonts.Primary, BorderStyle = BorderStyle.FixedSingle,
        TextAlign = HorizontalAlignment.Right,
    };

    private static NumericUpDown MakeFloat(float value, float min, float max) => new()
    {
        Minimum = (decimal)min, Maximum = (decimal)max,
        Value   = Math.Clamp((decimal)value, (decimal)min, (decimal)max),
        DecimalPlaces = 3, Increment = 0.1m,
        BackColor = EditorColors.InputBackground, ForeColor = EditorColors.TextPrimary,
        Font = EditorFonts.Primary, BorderStyle = BorderStyle.FixedSingle,
        TextAlign = HorizontalAlignment.Right,
    };

    private static Button MakeBrowse()
    {
        Button b = new()
        {
            Text = "…", FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt, ForeColor = EditorColors.TextSecondary,
            Font = EditorFonts.Primary,
        };
        b.FlatAppearance.BorderColor = EditorColors.Border;
        return b;
    }

    private static Button MakeSmallBtn(string text)
    {
        Button b = new()
        {
            Text = text, FlatStyle = FlatStyle.Flat, Width = 60,
            BackColor = EditorColors.PanelBackgroundAlt, ForeColor = EditorColors.TextSecondary,
            Font = EditorFonts.Small,
        };
        b.FlatAppearance.BorderColor = EditorColors.Border;
        return b;
    }
}
