using System.Text.Json;
using MonoGame.Editor.Core.Assets;
using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Core.Models;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Muestra y edita un asset de tema de interfaz (.uitheme.json).
/// Muestra las rutas de textura NineSlice y los márgenes de borde para cada tipo de control compatible.
/// Se activa automáticamente cuando se selecciona un asset de tipo <see cref="AssetType.UITheme"/>.
/// </summary>
public sealed class UIThemeInspectorPanel : UserControl
{
    #region Constants

    private const int LabelWidth  = 110;
    private const int InputWidth  = 130;
    private const int RowHeight   = 28;
    private const int SidePad     = 8;
    private const int NumericW    = 54;

    #endregion

    #region Inner record — per-control row set

    private sealed class ControlRows
    {
        public required TextBox   TexturePath;
        public required NumericUpDown BorderLeft;
        public required NumericUpDown BorderRight;
        public required NumericUpDown BorderTop;
        public required NumericUpDown BorderBottom;
        public required CheckBox  TileEdges;
        public required CheckBox  TileCenter;
    }

    #endregion

    #region Fields

    private EditorContext?  _context;
    private AssetInfo?      _currentAsset;
    private EditorUITheme?  _currentTheme;

    private readonly Panel  _scroll;
    private readonly Label  _noSelectionLabel;
    private readonly Label  _titleLabel;
    private readonly Button _saveButton;

    private readonly Dictionary<string, ControlRows> _rows = new(StringComparer.Ordinal);

    #endregion

    #region Constructor

    /// <summary>Crea el panel. Llama a <see cref="Initialize"/> para conectar con el contexto del editor.</summary>
    public UIThemeInspectorPanel()
    {
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Controls.Add(_scroll);

        _noSelectionLabel = new Label
        {
            Text      = "Select a UI theme asset (.uitheme.json) to edit it.",
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            ForeColor = System.Drawing.Color.Gray,
        };
        _scroll.Controls.Add(_noSelectionLabel);

        _titleLabel = new Label
        {
            Font     = new System.Drawing.Font(Font.FontFamily, 9f, System.Drawing.FontStyle.Bold),
            AutoSize = true,
            Location = new System.Drawing.Point(SidePad, SidePad),
        };

        _saveButton = new Button
        {
            Text   = "Save .uitheme.json",
            Width  = 150,
            Height = 28,
        };
        _saveButton.Click += OnSave;

        ShowContent(false);
    }

    #endregion

    #region Initialization

    /// <summary>Conecta este panel con el contexto del editor.</summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<AssetSelectedEvent>(OnAssetSelected);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _context is not null)
            _context.EventBus.Unsubscribe<AssetSelectedEvent>(OnAssetSelected);
        base.Dispose(disposing);
    }

    #endregion

    #region Event handlers

    private void OnAssetSelected(AssetSelectedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnAssetSelected(evt)); return; }

        if (evt.Asset is null || evt.Asset.Type != AssetType.UITheme)
        {
            _currentAsset = null;
            _currentTheme = null;
            ShowContent(false);
            return;
        }

        _currentAsset = evt.Asset;
        _currentTheme = LoadOrCreate(evt.Asset.AbsolutePath);
        PopulateControls();
        ShowContent(true);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentAsset is null || _currentTheme is null) return;
        WriteRowsToTheme();

        string json = JsonSerializer.Serialize(_currentTheme, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_currentAsset.AbsolutePath, json);
    }

    #endregion

    #region Build / populate

    private void PopulateControls()
    {
        if (_currentTheme is null || _currentAsset is null) return;

        _scroll.SuspendLayout();
        _scroll.Controls.Clear();
        _rows.Clear();

        int y = SidePad;

        _titleLabel.Location = new System.Drawing.Point(SidePad, y);
        _titleLabel.Text     = _currentTheme.Name;
        _scroll.Controls.Add(_titleLabel);
        y += 24;

        y = AddControlSection("Panel",       _currentTheme.Panel,       y);
        y = AddControlSection("Button",      _currentTheme.Button,      y);
        y = AddControlSection("Dropdown",    _currentTheme.Dropdown,    y);
        y = AddControlSection("ProgressBar", _currentTheme.ProgressBar, y);
        y = AddControlSection("TextBox",     _currentTheme.TextBox,     y);

        _saveButton.Location = new System.Drawing.Point(SidePad, y + 8);
        _scroll.Controls.Add(_saveButton);

        _scroll.ResumeLayout();
    }

    private int AddControlSection(string controlName, EditorUIThemeEntry entry, int startY)
    {
        int y = startY;
        int w = ClientSize.Width - SidePad * 2 - SystemInformation.VerticalScrollBarWidth;

        // Encabezado de sección
        Panel header = new Panel
        {
            Location  = new System.Drawing.Point(SidePad, y),
            Width     = w,
            Height    = 22,
            BackColor = System.Drawing.SystemColors.ControlDark,
        };
        Label headerLabel = new Label
        {
            Text      = controlName,
            Dock      = DockStyle.Fill,
            Font      = new System.Drawing.Font(Font.FontFamily, 8.5f, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
        };
        header.Controls.Add(headerLabel);
        _scroll.Controls.Add(header);
        y += 24;

        // Fila de ruta de textura
        TextBox texBox = new TextBox();
        y = AddRow(_scroll, "Texture", y, w, BuildTextureRow(entry, texBox));

        // Filas de borde
        NumericUpDown nLeft   = CreateBorderInput(entry.BorderLeft);
        NumericUpDown nRight  = CreateBorderInput(entry.BorderRight);
        NumericUpDown nTop    = CreateBorderInput(entry.BorderTop);
        NumericUpDown nBottom = CreateBorderInput(entry.BorderBottom);
        y = AddRow(_scroll, "Left / Right", y, w, BuildPairPanel(nLeft, nRight));
        y = AddRow(_scroll, "Top / Bottom", y, w, BuildPairPanel(nTop, nBottom));

        // TileEdges / TileCenter
        CheckBox chkEdges  = new CheckBox { Text = "Tile edges",  Checked = entry.TileEdges,  AutoSize = true };
        CheckBox chkCenter = new CheckBox { Text = "Tile center", Checked = entry.TileCenter, AutoSize = true };
        y = AddRow(_scroll, string.Empty, y, w, BuildCheckRow(chkEdges, chkCenter));

        _rows[controlName] = new ControlRows
        {
            TexturePath  = texBox,
            BorderLeft   = nLeft,
            BorderRight  = nRight,
            BorderTop    = nTop,
            BorderBottom = nBottom,
            TileEdges    = chkEdges,
            TileCenter   = chkCenter,
        };

        return y + SidePad;
    }

    private static int AddRow(Panel parent, string labelText, int y, int width, Control input)
    {
        if (labelText.Length > 0)
        {
            Label lbl = new Label
            {
                Text      = labelText,
                Location  = new System.Drawing.Point(0, y + 4),
                Width     = LabelWidth,
                Height    = RowHeight,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            };
            parent.Controls.Add(lbl);
        }

        input.Location = new System.Drawing.Point(LabelWidth + 4, y + 2);
        input.Width    = width - LabelWidth - 4;
        parent.Controls.Add(input);
        return y + RowHeight + 2;
    }

    private Panel BuildTextureRow(EditorUIThemeEntry entry, TextBox texBox)
    {
        Panel panel = new Panel { Height = RowHeight - 4 };

        texBox.Text = entry.TexturePath;
        texBox.Dock = DockStyle.Fill;

        Button browse = new Button { Text = "...", Width = 28, Dock = DockStyle.Right };
        TextBox captured = texBox;
        browse.Click += (_, _) =>
        {
            using OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Texture files|*.png;*.jpg;*.bmp;*.tga",
                Title  = "Select NineSlice Texture",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            string relative = Path.GetRelativePath(
                _context?.ActiveProject?.ContentPath ?? string.Empty, dlg.FileName);
            relative       = Path.ChangeExtension(relative, null);
            captured.Text  = relative;
        };

        panel.Controls.Add(texBox);
        panel.Controls.Add(browse);
        return panel;
    }

    private static Panel BuildPairPanel(NumericUpDown a, NumericUpDown b)
    {
        Panel panel = new Panel { Height = RowHeight - 4 };
        a.Width = NumericW;
        a.Dock  = DockStyle.Left;
        b.Dock  = DockStyle.Fill;
        panel.Controls.Add(b);
        panel.Controls.Add(a);
        return panel;
    }

    private static Panel BuildCheckRow(CheckBox a, CheckBox b)
    {
        Panel panel = new Panel { Height = RowHeight - 4 };
        a.Dock = DockStyle.Left;
        a.Width = 90;
        b.Dock = DockStyle.Fill;
        panel.Controls.Add(b);
        panel.Controls.Add(a);
        return panel;
    }

    private static NumericUpDown CreateBorderInput(int value) =>
        new NumericUpDown
        {
            Minimum       = 0,
            Maximum       = 512,
            DecimalPlaces = 0,
            Increment     = 1,
            Value         = Math.Clamp(value, 0, 512),
            Height        = RowHeight - 4,
        };

    private void WriteRowsToTheme()
    {
        if (_currentTheme is null) return;
        WriteEntry(_currentTheme.Panel,       "Panel");
        WriteEntry(_currentTheme.Button,      "Button");
        WriteEntry(_currentTheme.Dropdown,    "Dropdown");
        WriteEntry(_currentTheme.ProgressBar, "ProgressBar");
        WriteEntry(_currentTheme.TextBox,     "TextBox");
    }

    private void WriteEntry(EditorUIThemeEntry entry, string controlName)
    {
        if (!_rows.TryGetValue(controlName, out ControlRows? r)) return;
        entry.TexturePath  = r.TexturePath.Text.Trim();
        entry.BorderLeft   = (int)r.BorderLeft.Value;
        entry.BorderRight  = (int)r.BorderRight.Value;
        entry.BorderTop    = (int)r.BorderTop.Value;
        entry.BorderBottom = (int)r.BorderBottom.Value;
        entry.TileEdges    = r.TileEdges.Checked;
        entry.TileCenter   = r.TileCenter.Checked;
    }

    #endregion

    #region Helpers

    private static EditorUITheme LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<EditorUITheme>(json) ?? EditorUITheme.CreateEmpty();
            }
            catch { /* continuar */ }
        }

        return EditorUITheme.CreateEmpty();
    }

    private void ShowContent(bool show)
    {
        _noSelectionLabel.Visible = !show;
        _titleLabel.Visible       = show;
        _saveButton.Visible       = show;

        foreach (Control c in _scroll.Controls)
        {
            if (!ReferenceEquals(c, _noSelectionLabel) && !ReferenceEquals(c, _titleLabel) && !ReferenceEquals(c, _saveButton))
                c.Visible = show;
        }
    }

    #endregion
}
