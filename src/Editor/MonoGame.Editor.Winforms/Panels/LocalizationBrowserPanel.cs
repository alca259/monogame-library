using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel de localización: lista de claves de traducción y rejilla editable
/// de valores por locale.
/// </summary>
internal sealed class LocalizationBrowserPanel : UserControl
{
    private readonly LocalizationBrowserViewModel _vm = new();

    private ListBox      _lstKeys = null!;
    private DataGridView _grid    = null!;
    private readonly Label        _lblLocaleCount;
    private readonly Label        _lblStatus;

    private bool _updating;

    public LocalizationBrowserPanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

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

        // ── Save toolbar ───────────────────────────────────────────────────────
        Panel saveBar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 30,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4),
        };

        Button btnSave = new()
        {
            Text      = "Save",
            Dock      = DockStyle.Right,
            Width     = 70,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.AccentBlue,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
        };
        btnSave.FlatAppearance.BorderSize = 0;

        _lblLocaleCount = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "0 locales",
        };
        saveBar.Controls.Add(btnSave);
        saveBar.Controls.Add(_lblLocaleCount);

        // ── SplitContainer: keys | translations ───────────────────────────────
        SplitContainer split = new()
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            BackColor        = EditorColors.Border,
            Width = 500,
            SplitterWidth    = 2,
            Panel1MinSize    = 100,
            Panel2MinSize    = 150,
            SplitterDistance = 160,
        };

        // Columna izquierda: claves
        Panel keyPanel = BuildKeyPanel();
        split.Panel1.Controls.Add(keyPanel);

        // Columna derecha: traducciones
        Panel transPanel = BuildTranslationPanel();
        split.Panel2.Controls.Add(transPanel);

        Controls.Add(split);
        Controls.Add(saveBar);
        Controls.Add(_lblStatus);

        // ── Eventos de controles ──────────────────────────────────────────────
        _lstKeys.SelectedIndexChanged += (_, _) =>
        {
            if (_updating) return;
            _vm.SelectKey(_lstKeys.SelectedItem as string);
        };

        _grid.CellValueChanged += (_, e) =>
        {
            if (_updating || e.RowIndex < 0 || e.ColumnIndex != 1) return;
            IReadOnlyList<LocaleValueItem> rows = _vm.Translations;
            if (e.RowIndex < rows.Count)
                rows[e.RowIndex].Value = _grid.Rows[e.RowIndex].Cells[1].Value as string ?? string.Empty;
        };

        btnSave.Click += async (_, _) =>
            await _vm.SaveAsync().ConfigureAwait(true);

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.KeyListChanged      += OnKeyListChanged;
        _vm.TranslationGridChanged += OnTranslationGridChanged;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Rebuild ────────────────────────────────────────────────────────────────

    private void OnKeyListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnKeyListChanged); return; }

        _updating = true;
        _lstKeys.BeginUpdate();
        _lstKeys.Items.Clear();
        foreach (string key in _vm.KeyItems) _lstKeys.Items.Add(key);
        _lstKeys.EndUpdate();
        _lblLocaleCount.Text = _vm.LocaleCountText;
        _updating = false;
    }

    private void OnTranslationGridChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnTranslationGridChanged); return; }

        _updating = true;
        _grid.Rows.Clear();
        foreach (LocaleValueItem item in _vm.Translations)
            _grid.Rows.Add(item.Locale, item.Value);
        _lblStatus.Text = _vm.StatusText;
        _updating = false;
    }

    // ── Builders ────────────────────────────────────────────────────────────────

    private Panel BuildKeyPanel()
    {
        Panel panel = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
        };

        Label header = new()
        {
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = "Keys",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        _lstKeys = new ListBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = EditorColors.PanelBackground,
            ForeColor   = EditorColors.TextPrimary,
            Font        = EditorFonts.Small,
            BorderStyle = BorderStyle.None,
        };

        Panel toolbar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2),
        };

        Button btnAddKey    = MakeSmallBtn("+K");
        Button btnRemKey    = MakeSmallBtn("−K");
        Button btnAddLocale = MakeSmallBtn("+L");
        Button btnRemLocale = MakeSmallBtn("−L");

        new ToolTip().SetToolTip(btnAddKey,    "Add translation key");
        new ToolTip().SetToolTip(btnRemKey,    "Remove selected key");
        new ToolTip().SetToolTip(btnAddLocale, "Add locale");
        new ToolTip().SetToolTip(btnRemLocale, "Remove locale");

        btnAddKey.Dock = btnRemKey.Dock = btnAddLocale.Dock = btnRemLocale.Dock = DockStyle.Left;
        toolbar.Controls.Add(btnRemLocale);
        toolbar.Controls.Add(btnAddLocale);
        toolbar.Controls.Add(new Panel { Width = 4, Dock = DockStyle.Left, BackColor = EditorColors.PanelBackgroundAlt });
        toolbar.Controls.Add(btnRemKey);
        toolbar.Controls.Add(btnAddKey);

        panel.Controls.Add(_lstKeys);
        panel.Controls.Add(header);
        panel.Controls.Add(toolbar);

        // Eventos
        btnAddKey.Click += (_, _) =>
        {
            string? key = WinFormsDialogService.Prompt(FindForm(), "Add Key", "Translation key:");
            if (!string.IsNullOrWhiteSpace(key)) _vm.AddKey(key);
        };
        btnRemKey.Click += (_, _) =>
        {
            if (_lstKeys.SelectedItem is string key)
            {
                bool ok = WinFormsDialogService.Confirm(FindForm(), "Remove Key",
                    $"Remove key '{key}' from all locales?", "Remove", "Cancel");
                if (ok) _vm.RemoveKey();
            }
        };
        btnAddLocale.Click += (_, _) =>
        {
            string? locale = WinFormsDialogService.Prompt(FindForm(), "Add Locale",
                "Locale code (e.g. \"en\", \"es\"):");
            if (!string.IsNullOrWhiteSpace(locale)) _vm.AddLocale(locale);
        };
        btnRemLocale.Click += (_, _) =>
        {
            string? locale = WinFormsDialogService.Prompt(FindForm(), "Remove Locale",
                "Locale code to remove:");
            if (!string.IsNullOrWhiteSpace(locale))
            {
                bool ok = WinFormsDialogService.Confirm(FindForm(), "Remove Locale",
                    $"Remove locale '{locale}' and delete its file?", "Remove", "Cancel");
                if (ok) _vm.RemoveLocale(locale);
            }
        };

        return panel;
    }

    private Panel BuildTranslationPanel()
    {
        Panel panel = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
        };

        Label header = new()
        {
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = "Translations",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        _grid = new DataGridView
        {
            Dock                = DockStyle.Fill,
            BackgroundColor     = EditorColors.PanelBackground,
            ForeColor           = EditorColors.TextPrimary,
            GridColor           = EditorColors.Border,
            Font                = EditorFonts.Small,
            RowHeadersVisible   = false,
            AllowUserToAddRows  = false,
            AllowUserToDeleteRows = false,
            SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BorderStyle         = BorderStyle.None,
        };
        _grid.DefaultCellStyle.BackColor  = EditorColors.PanelBackground;
        _grid.DefaultCellStyle.ForeColor  = EditorColors.TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = EditorColors.AccentBlue;
        _grid.DefaultCellStyle.SelectionForeColor = EditorColors.TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = EditorColors.PanelBackgroundAlt;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = EditorColors.TextMuted;
        _grid.EnableHeadersVisualStyles = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Locale", HeaderText = "Locale", ReadOnly = true, FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value",  HeaderText = "Value",  ReadOnly = false, FillWeight = 70 });

        panel.Controls.Add(_grid);
        panel.Controls.Add(header);
        return panel;
    }

    private static Button MakeSmallBtn(string text) => new()
    {
        Text      = text,
        Width     = 30,
        FlatStyle = FlatStyle.Flat,
        BackColor = EditorColors.PanelBackgroundAlt,
        ForeColor = EditorColors.TextPrimary,
        Font      = EditorFonts.Small,
    };
}
