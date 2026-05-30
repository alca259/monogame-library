namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Muestra y edita los metadatos de bordes 9-slice de un asset de textura o sprite seleccionado.
/// Se muestra automáticamente cuando se selecciona un asset de tipo <see cref="AssetType.Texture"/> o <see cref="AssetType.Sprite"/>
/// en el navegador de assets.
/// </summary>
public sealed class SpriteInspectorPanel : UserControl
{
    #region Constants

    private const int LabelWidth  = 100;
    private const int InputWidth  = 70;
    private const int RowHeight   = 28;
    private const int SidePad     = 8;

    #endregion

    #region Fields

    private EditorContext? _context;
    private AssetInfo? _currentAsset;
    private EditorSpriteMetadata? _currentMetadata;
    private bool _suppressUpdate;

    private readonly Panel        _scroll;
    private readonly PictureBox   _preview;
    private readonly NumericUpDown _borderLeft;
    private readonly NumericUpDown _borderRight;
    private readonly NumericUpDown _borderTop;
    private readonly NumericUpDown _borderBottom;
    private readonly CheckBox     _tileEdges;
    private readonly CheckBox     _tileCenter;
    private readonly Button       _saveButton;
    private readonly Label        _titleLabel;
    private readonly Label        _noSelectionLabel;

    #endregion

    #region Constructor

    /// <summary>Crea el panel. Llama a <see cref="Initialize"/> para conectar con el contexto del editor.</summary>
    public SpriteInspectorPanel()
    {
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Controls.Add(_scroll);

        _noSelectionLabel = new Label
        {
            Text      = "Select a texture or sprite asset to edit its 9-slice borders.",
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

        _preview = new PictureBox
        {
            SizeMode    = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Size        = new System.Drawing.Size(200, 200),
            Location    = new System.Drawing.Point(SidePad, SidePad + 22),
        };

        int y = SidePad + 200 + 28;

        _borderLeft   = CreateNumericInput();
        _borderRight  = CreateNumericInput();
        _borderTop    = CreateNumericInput();
        _borderBottom = CreateNumericInput();
        _tileEdges    = new CheckBox { Text = "Tile edges", AutoSize = true };
        _tileCenter   = new CheckBox { Text = "Tile center", AutoSize = true };

        _saveButton = new Button
        {
            Text   = "Save .sprite.json",
            Height = 28,
            Width  = 150,
        };
        _saveButton.Click += OnSave;

        _borderLeft.ValueChanged   += OnValueChanged;
        _borderRight.ValueChanged  += OnValueChanged;
        _borderTop.ValueChanged    += OnValueChanged;
        _borderBottom.ValueChanged += OnValueChanged;
        _tileEdges.CheckedChanged  += OnValueChanged;
        _tileCenter.CheckedChanged += OnValueChanged;

        int rowY = y;
        _scroll.Controls.AddRange(
        [
            _titleLabel,
            _preview,
            MakeRow("Border Left",   _borderLeft,   ref rowY),
            MakeRow("Border Right",  _borderRight,  ref rowY),
            MakeRow("Border Top",    _borderTop,    ref rowY),
            MakeRow("Border Bottom", _borderBottom, ref rowY),
            PlaceControl(_tileEdges,  ref rowY),
            PlaceControl(_tileCenter, ref rowY),
            PlaceSaveButton(ref rowY),
        ]);

        ShowContent(false);
    }

    #endregion

    #region Initialization

    /// <summary>Conecta este panel con el contexto del editor y se suscribe a eventos de selección de assets.</summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<AssetSelectedEvent>(OnAssetSelected);
    }

    #endregion

    #region Event handlers

    private void OnAssetSelected(AssetSelectedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnAssetSelected(evt)); return; }

        if (evt.Asset is null || evt.Asset.Type is not (AssetType.Texture or AssetType.Sprite))
        {
            _currentAsset    = null;
            _currentMetadata = null;
            ShowContent(false);
            return;
        }

        _currentAsset    = evt.Asset;
        _currentMetadata = LoadOrCreateMetadata(evt.Asset);
        PopulateControls();
        ShowContent(true);
    }

    private void OnValueChanged(object? sender, EventArgs e)
    {
        if (_suppressUpdate || _currentMetadata is null) return;
        WriteControlsToMetadata();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentAsset is null || _currentMetadata is null) return;
        WriteControlsToMetadata();

        string metaPath = GetMetaPath(_currentAsset.AbsolutePath);
        string json = JsonSerializer.Serialize(_currentMetadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metaPath, json);
        _context?.EventBus.Publish(new AssetImportedEvent(AssetClassifier.CreateInfo(metaPath, Path.GetDirectoryName(metaPath)!)));
    }

    #endregion

    #region Helpers

    private static EditorSpriteMetadata LoadOrCreateMetadata(AssetInfo asset)
    {
        string metaPath = GetMetaPath(asset.AbsolutePath);
        if (File.Exists(metaPath))
        {
            try
            {
                string json = File.ReadAllText(metaPath);
                return JsonSerializer.Deserialize<EditorSpriteMetadata>(json)
                    ?? new EditorSpriteMetadata { TextureRelativePath = asset.RelativePath };
            }
            catch { /* continuar */ }
        }

        return new EditorSpriteMetadata { TextureRelativePath = asset.RelativePath };
    }

    private static string GetMetaPath(string texturePath)
    {
        string dir      = Path.GetDirectoryName(texturePath)!;
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        return Path.Combine(dir, baseName + ".sprite.json");
    }

    private void PopulateControls()
    {
        if (_currentAsset is null || _currentMetadata is null) return;

        _suppressUpdate = true;

        _titleLabel.Text = _currentAsset.Name;

        _preview.Image?.Dispose();
        _preview.Image = null;
        if (File.Exists(_currentAsset.AbsolutePath))
        {
            try { _preview.Image = System.Drawing.Image.FromFile(_currentAsset.AbsolutePath); }
            catch { /* ignorar */ }
        }

        _borderLeft.Value   = _currentMetadata.BorderLeft;
        _borderRight.Value  = _currentMetadata.BorderRight;
        _borderTop.Value    = _currentMetadata.BorderTop;
        _borderBottom.Value = _currentMetadata.BorderBottom;
        _tileEdges.Checked  = _currentMetadata.TileEdges;
        _tileCenter.Checked = _currentMetadata.TileCenter;

        _suppressUpdate = false;
    }

    private void WriteControlsToMetadata()
    {
        if (_currentMetadata is null) return;
        _currentMetadata.BorderLeft   = (int)_borderLeft.Value;
        _currentMetadata.BorderRight  = (int)_borderRight.Value;
        _currentMetadata.BorderTop    = (int)_borderTop.Value;
        _currentMetadata.BorderBottom = (int)_borderBottom.Value;
        _currentMetadata.TileEdges    = _tileEdges.Checked;
        _currentMetadata.TileCenter   = _tileCenter.Checked;
    }

    private void ShowContent(bool show)
    {
        _noSelectionLabel.Visible = !show;
        _titleLabel.Visible  = show;
        _preview.Visible     = show;
        _borderLeft.Visible  = show;
        _borderRight.Visible = show;
        _borderTop.Visible   = show;
        _borderBottom.Visible = show;
        _tileEdges.Visible   = show;
        _tileCenter.Visible  = show;
        _saveButton.Visible  = show;
        foreach (Control c in _scroll.Controls)
            if (c is Label l && l != _titleLabel && l != _noSelectionLabel)
                l.Visible = show;
    }

    private static NumericUpDown CreateNumericInput() =>
        new() { Minimum = 0, Maximum = 512, Width = InputWidth, Height = 24 };

    private Panel MakeRow(string label, Control input, ref int y)
    {
        var lbl = new Label
        {
            Text      = label,
            Width     = LabelWidth,
            Height    = RowHeight,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Location  = new System.Drawing.Point(0, 0),
        };
        input.Location = new System.Drawing.Point(LabelWidth + 4, 2);
        input.Height   = RowHeight - 4;

        var row = new Panel
        {
            Location = new System.Drawing.Point(SidePad, y),
            Width    = LabelWidth + InputWidth + 16,
            Height   = RowHeight,
        };
        row.Controls.Add(lbl);
        row.Controls.Add(input);
        y += RowHeight + 2;
        return row;
    }

    private Control PlaceControl(Control c, ref int y)
    {
        c.Location = new System.Drawing.Point(SidePad, y);
        y += c.Height + 4;
        return c;
    }

    private Control PlaceSaveButton(ref int y)
    {
        _saveButton.Location = new System.Drawing.Point(SidePad, y + 8);
        y += _saveButton.Height + 16;
        return _saveButton;
    }

    #endregion
}
