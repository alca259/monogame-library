using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel inspector de materiales: carga y edita un <c>.material.json</c>
/// seleccionado desde el Asset Browser.
/// </summary>
internal sealed class MaterialInspectorPanel : UserControl
{
    private readonly MaterialInspectorViewModel _vm = new();

    private readonly Label     _lblTitle;
    private readonly TextBox   _txtName;
    private readonly TextBox   _txtShader;
    private readonly TextBox   _txtRenderMode;
    private readonly ListView  _lstProps;
    private readonly Label     _lblStatus;
    private readonly Button    _btnSave;

    private EditorMaterial? _material;
    private string          _currentPath = string.Empty;
    private bool            _loading;

    public MaterialInspectorPanel()
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
            Text      = "No material selected",
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

        // ── Campos editables ──────────────────────────────────────────────────
        _txtName       = MakeTextBox();
        _txtShader     = MakeTextBox();
        _txtRenderMode = MakeTextBox();

        ToolTip tip = new();
        Button btnBrowseShader = new()
        {
            Text      = "…",
            Width     = 28,
            Height    = 22,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextPrimary,
        };
        btnBrowseShader.FlatAppearance.BorderColor = EditorColors.Border;
        tip.SetToolTip(btnBrowseShader, "Browse for shader file");

        Panel shaderRow = new() { Dock = DockStyle.Top, Height = 26, BackColor = EditorColors.PanelBackground };
        btnBrowseShader.Dock = DockStyle.Right;
        _txtShader.Dock      = DockStyle.Fill;
        shaderRow.Controls.Add(_txtShader);
        shaderRow.Controls.Add(btnBrowseShader);

        TableLayoutPanel fields = new()
        {
            Dock        = DockStyle.Top,
            ColumnCount = 2,
            RowCount    = 3,
            AutoSize    = true,
            BackColor   = EditorColors.PanelBackground,
            Padding     = new Padding(8, 4, 8, 0),
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(fields, 0, "Name",        _txtName);
        AddRow(fields, 1, "Shader",      shaderRow);
        AddRow(fields, 2, "Render Mode", _txtRenderMode);

        // ── Lista de propiedades ──────────────────────────────────────────────
        Label lblProps = new()
        {
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = "Properties",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        _lstProps = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            HeaderStyle   = ColumnHeaderStyle.Nonclickable,
            BackColor     = EditorColors.PanelBackground,
            ForeColor     = EditorColors.TextPrimary,
            Font          = EditorFonts.Small,
            BorderStyle   = BorderStyle.None,
            GridLines     = false,
            MultiSelect   = false,
        };
        _lstProps.Columns.Add("Property", 110, HorizontalAlignment.Left);
        _lstProps.Columns.Add("Type",      70, HorizontalAlignment.Left);
        _lstProps.Columns.Add("Value",    -2,  HorizontalAlignment.Left);

        Panel propsPanel = new() { Dock = DockStyle.Fill, BackColor = EditorColors.PanelBackground };
        propsPanel.Controls.Add(_lstProps);
        propsPanel.Controls.Add(lblProps);

        Controls.Add(propsPanel);
        Controls.Add(fields);
        Controls.Add(_lblTitle);
        Controls.Add(_btnSave);
        Controls.Add(_lblStatus);

        // ── Eventos de controles ──────────────────────────────────────────────
        _txtName.TextChanged       += (_, _) => { if (!_loading && _material is not null) _material.Name = _txtName.Text; };
        _txtShader.TextChanged     += (_, _) => { if (!_loading && _material is not null) _material.ShaderPath = _txtShader.Text; };
        _txtRenderMode.TextChanged += (_, _) => { if (!_loading && _material is not null) _material.RenderingMode = _txtRenderMode.Text; };

        btnBrowseShader.Click += (_, _) =>
        {
            string? path = WinFormsDialogService.PickFile(FindForm(), filter: "Effect files|*.fx|All files|*.*");
            if (path is not null) { if (_material is not null) _material.ShaderPath = path; _txtShader.Text = path; }
        };

        _btnSave.Click += (_, _) => SaveMaterial();

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.AssetSelected += OnAssetSelected;
        _vm.ProjectOpened += OnProjectOpened;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Handlers ───────────────────────────────────────────────────────────────

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => OnAssetSelected(e)); return; }

        if (e.Asset is null || e.Asset.Type != AssetType.Material)
        {
            ClearForm();
            return;
        }

        _currentPath = e.Asset.AbsolutePath;
        _material    = LoadMaterial(_currentPath, e.Asset.Name);
        PopulateForm();
    }

    private void OnProjectOpened()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnProjectOpened); return; }
        ClearForm();
    }

    // ── Load / save ────────────────────────────────────────────────────────────

    private static EditorMaterial LoadMaterial(string path, string name)
    {
        if (!File.Exists(path)) return EditorMaterial.CreateEmpty();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorMaterial>(json) ?? EditorMaterial.CreateEmpty();
        }
        catch { return EditorMaterial.CreateEmpty(); }
    }

    private void SaveMaterial()
    {
        if (_material is null || string.IsNullOrEmpty(_currentPath)) return;
        try
        {
            string json = JsonSerializer.Serialize(_material, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentPath, json);
            _lblStatus.Text = "Saved";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
        }
    }

    // ── Form population ────────────────────────────────────────────────────────

    private void PopulateForm()
    {
        if (_material is null) return;
        _loading = true;

        _lblTitle.Text       = _material.Name;
        _txtName.Text        = _material.Name;
        _txtShader.Text      = _material.ShaderPath;
        _txtRenderMode.Text  = _material.RenderingMode;
        _btnSave.Enabled     = true;
        _lblStatus.Text      = string.Empty;

        _lstProps.BeginUpdate();
        _lstProps.Items.Clear();
        foreach (EditorMaterialProperty prop in _material.Properties.Values)
        {
            string valueText = prop.Type == EditorMaterialPropertyType.Texture2D
                ? prop.TexturePath
                : TrySerialize(prop.Data);

            ListViewItem item = new(prop.Name);
            item.SubItems.Add(prop.Type.ToString());
            item.SubItems.Add(valueText);
            _lstProps.Items.Add(item);
        }
        _lstProps.EndUpdate();

        _loading = false;
    }

    private void ClearForm()
    {
        _material    = null;
        _currentPath = string.Empty;
        _loading     = true;

        _lblTitle.Text      = "No material selected";
        _txtName.Text       = string.Empty;
        _txtShader.Text     = string.Empty;
        _txtRenderMode.Text = string.Empty;
        _btnSave.Enabled    = false;
        _lblStatus.Text     = string.Empty;
        _lstProps.Items.Clear();

        _loading = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string TrySerialize(object? data)
    {
        if (data is null) return string.Empty;
        try { return JsonSerializer.Serialize(data); }
        catch { return data.ToString() ?? string.Empty; }
    }

    private static TextBox MakeTextBox() => new()
    {
        Font        = EditorFonts.Small,
        BackColor   = EditorColors.InputBackground,
        ForeColor   = EditorColors.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
    };

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control control)
    {
        grid.Controls.Add(new Label
        {
            Text      = label,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock      = DockStyle.Fill,
        }, 0, row);
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, 1, row);
    }
}
