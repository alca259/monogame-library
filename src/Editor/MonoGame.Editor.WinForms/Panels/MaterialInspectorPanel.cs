using SdColor = System.Drawing.Color;
using SdPoint = System.Drawing.Point;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Inspector de materiales estilo Unity. Muestra secciones PBR estructuradas para el shader Standard
/// y recurre a una lista de propiedades genérica para shaders personalizados.
/// </summary>
public sealed class MaterialInspectorPanel : UserControl
{
    #region Constants — layout

    private const int PanelWidth      = 290;
    private const int LeftPad         = 8;
    private const int LabelWidth      = 100;
    private const int TextureSize     = 40;
    private const int RowTexture      = TextureSize + 8;  // altura de fila con miniatura de textura
    private const int RowFlat         = 24;               // altura de fila sin textura
    private const int SectionHeight   = 22;
    private const int PreviewSize     = 256;
    private const string StandardPath = "StandardEffect";

    #endregion

    #region Constants — colours (Unity dark theme)

    private static readonly SdColor BgColor       = SdColor.FromArgb(56,  56,  56);
    private static readonly SdColor BgDark        = SdColor.FromArgb(42,  42,  42);
    private static readonly SdColor SectionColor  = SdColor.FromArgb(70,  70,  70);
    private static readonly SdColor TextColor     = SdColor.FromArgb(220, 220, 220);
    private static readonly SdColor DimText       = SdColor.FromArgb(140, 140, 140);
    private static readonly SdColor InputBg       = SdColor.FromArgb(40,  40,  40);
    private static readonly SdColor BorderColor   = SdColor.FromArgb(28,  28,  28);
    private static readonly SdColor SwatchBorder  = SdColor.FromArgb(80,  80,  80);

    #endregion

    #region Fields

    private EditorContext?           _context;
    private AssetInfo?               _currentAsset;
    private EditorMaterial?          _current;
    private bool                     _suppress;

    private readonly Panel           _scroll;
    private readonly Label           _noSel;

    // Controles de cabecera (siempre visibles cuando hay material cargado)
    private readonly Panel           _header;
    private readonly Label           _titleLabel;
    private readonly ComboBox        _shaderDropdown;

    // Fila de modo de renderizado (siempre visible)
    private readonly Panel           _renderModeRow;
    private readonly ComboBox        _renderModeDropdown;

    // Área dinámica — reconstruida por material
    private readonly Panel           _propertiesArea;

    // Área de vista previa
    private readonly Panel           _previewArea;
    private readonly PictureBox      _previewBox;
    private readonly Button          _renderPreviewBtn;
    private readonly Button          _saveBtn;

    private Action<EditorMaterial>? _onRenderRequested;

    #endregion

    #region Constructor

    /// <summary>Crea el panel. Llama a <see cref="Initialize"/> para conectar con el contexto del editor.</summary>
    public MaterialInspectorPanel()
    {
        BackColor = BgColor;
        Width     = PanelWidth;

        // ── Contenedor con scroll ─────────────────────────────────────────
        _scroll = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
            BackColor  = BgColor,
        };
        Controls.Add(_scroll);

        // ── Etiqueta de ninguna selección ──────────────────────────────────
        _noSel = new Label
        {
            Text      = "Select a .mat.json file to edit it.",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = DimText,
            BackColor = BgColor,
        };
        _scroll.Controls.Add(_noSel);

        // ── Cabecera (nombre del material + selector de shader) ──────────────
        _header = new Panel
        {
            BackColor = BgDark,
            Width     = PanelWidth,
            Height    = 58,
            Location  = new SdPoint(0, 0),
        };

        _titleLabel = new Label
        {
            AutoSize  = true,
            Location  = new SdPoint(LeftPad, 6),
            Font      = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            ForeColor = TextColor,
            BackColor = SdColor.Transparent,
        };

        var shaderLabel = MakeLabel("Shader", LeftPad, 30, LabelWidth);
        _shaderDropdown = MakeComboBox(LeftPad + LabelWidth + 4, 28, PanelWidth - LabelWidth - LeftPad * 2 - 8);
        _shaderDropdown.Items.AddRange(new object[] { "Shaders/StandardEffect", "Shaders/SpriteTint", "Shaders/Grayscale", "Shaders/Vignette" });
        _shaderDropdown.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppress && _current is not null)
            {
                _current.ShaderPath = _shaderDropdown.Text;
                RebuildProperties();
            }
        };

        _header.Controls.Add(_titleLabel);
        _header.Controls.Add(shaderLabel);
        _header.Controls.Add(_shaderDropdown);
        _scroll.Controls.Add(_header);

        // ── Fila de modo de renderizado ──────────────────────────────────────
        _renderModeRow = new Panel
        {
            BackColor = BgColor,
            Width     = PanelWidth,
            Height    = RowFlat + 6,
            Location  = new SdPoint(0, _header.Bottom + 2),
        };
        _renderModeRow.Controls.Add(MakeLabel("Rendering Mode", LeftPad, 4, LabelWidth + 10));
        _renderModeDropdown = MakeComboBox(LeftPad + LabelWidth + 14, 3, PanelWidth - LabelWidth - LeftPad * 2 - 20);
        _renderModeDropdown.Items.AddRange(new object[] { "Opaque", "Cutout", "Fade", "Transparent" });
        _renderModeDropdown.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppress && _current is not null)
                _current.RenderingMode = _renderModeDropdown.Text;
        };
        _renderModeRow.Controls.Add(_renderModeDropdown);
        _scroll.Controls.Add(_renderModeRow);

        // ── Área de propiedades dinámicas ──────────────────────────────────
        _propertiesArea = new Panel
        {
            BackColor    = BgColor,
            Width        = PanelWidth,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location     = new SdPoint(0, _renderModeRow.Bottom + 2),
        };
        _scroll.Controls.Add(_propertiesArea);

        // ── Área de vista previa ──────────────────────────────────────────────
        _previewArea = new Panel
        {
            BackColor = BgDark,
            Width     = PanelWidth,
            Height    = PreviewSize + 40,
        };

        _previewBox = new PictureBox
        {
            Size        = new Size(PreviewSize, PreviewSize),
            SizeMode    = PictureBoxSizeMode.Normal,
            BackColor   = SdColor.FromArgb(32, 32, 32),
            BorderStyle = BorderStyle.None,
            Location    = new SdPoint((PanelWidth - PreviewSize) / 2, 4),
        };

        _renderPreviewBtn = MakeButton("▶ Render", 8, PreviewSize + 10, 90, 24);
        _renderPreviewBtn.Click += (_, _) =>
        {
            if (_current is not null)
                _onRenderRequested?.Invoke(_current);
        };

        _saveBtn = MakeButton("Save .mat.json", 104, PreviewSize + 10, 110, 24);
        _saveBtn.Click += OnSave;

        _previewArea.Controls.Add(_previewBox);
        _previewArea.Controls.Add(_renderPreviewBtn);
        _previewArea.Controls.Add(_saveBtn);
        _scroll.Controls.Add(_previewArea);

        ShowContent(false);
    }

    #endregion

    #region Initialize

    /// <summary>Conecta este panel con el contexto del editor.</summary>
    public void Initialize(EditorContext context, Action<EditorMaterial>? onRenderRequested = null)
    {
        _context           = context;
        _onRenderRequested = onRenderRequested;
        _context.EventBus.Subscribe<AssetSelectedEvent>(OnAssetSelected);
    }

    /// <summary>Actualiza la imagen de vista previa desde el hilo de renderizado.</summary>
    public void SetPreviewBitmap(System.Drawing.Bitmap bitmap)
    {
        if (InvokeRequired) { BeginInvoke(() => SetPreviewBitmap(bitmap)); return; }
        _previewBox.Image?.Dispose();
        _previewBox.Image = bitmap;
    }

    #endregion

    #region Event handlers

    private void OnAssetSelected(AssetSelectedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnAssetSelected(evt)); return; }

        if (evt.Asset is null || evt.Asset.Type != AssetType.Material)
        {
            _currentAsset = null;
            _current      = null;
            ShowContent(false);
            return;
        }

        _currentAsset = evt.Asset;
        _current      = LoadOrCreate(evt.Asset.AbsolutePath);
        PopulateHeader();
        RebuildProperties();
        ShowContent(true);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentAsset is null || _current is null) return;
        string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_currentAsset.AbsolutePath, json);
    }

    #endregion

    #region Populate header

    private void PopulateHeader()
    {
        if (_current is null) return;
        _suppress = true;

        _titleLabel.Text = _current.Name;

        _shaderDropdown.Text = _current.ShaderPath;
        if (_shaderDropdown.FindStringExact(_current.ShaderPath) < 0)
            _shaderDropdown.Items.Insert(0, _current.ShaderPath);

        int modeIdx = _renderModeDropdown.FindStringExact(_current.RenderingMode);
        _renderModeDropdown.SelectedIndex = modeIdx >= 0 ? modeIdx : 0;

        _suppress = false;
    }

    #endregion

    #region Rebuild properties area

    private void RebuildProperties()
    {
        _suppress = true;
        _propertiesArea.SuspendLayout();
        _propertiesArea.Controls.Clear();

        int y = 0;

        if (_current is not null && IsStandardShader())
            BuildStandardSections(ref y);
        else
            BuildGenericSection(ref y);

        _propertiesArea.Height = y + 4;
        _propertiesArea.ResumeLayout(true);
        _previewArea.Location = new SdPoint(0, _propertiesArea.Bottom + 4);
        _suppress = false;
    }

    private bool IsStandardShader() =>
        _current?.ShaderPath?.Contains(StandardPath, StringComparison.OrdinalIgnoreCase) == true;

    #endregion

    #region Standard shader sections

    private void BuildStandardSections(ref int y)
    {
        // ── Mapas principales ─────────────────────────────────────────────────
        AddSectionHeader("Main Maps", ref y);
        AddTextureRow("Albedo",       "AlbedoTexture",      ref y, colorKey: "AlbedoColor");
        AddTextureRow("Metallic",     "MetallicTexture",    ref y, sliderKey: "Metallic");
        AddSliderOnlyRow("Smoothness","Smoothness",          ref y, indented: true);
        AddTextureRow("Normal Map",   "NormalTexture",      ref y, floatKey: "NormalScale");
        AddTextureRow("Height Map",   "HeightTexture",      ref y, floatKey: "HeightScale");
        AddTextureRow("Occlusion",    "OcclusionTexture",   ref y);
        AddTextureRow("Emission",     "EmissionTexture",    ref y, colorKey: "EmissionColor", floatKey: "EmissionIntensity");
        AddTextureRow("Detail Mask",  "DetailMaskTexture",  ref y);
        AddTilingOffsetRows("Tiling", "Offset",              ref y);

        // ── Mapas secundarios ─────────────────────────────────────────────────
        y += 4;
        AddSectionHeader("Secondary Maps", ref y);
        AddTextureRow("Detail Albedo x2",  "DetailAlbedoTexture", ref y);
        AddTextureRow("Normal Map",        "DetailNormalTexture", ref y, floatKey: "DetailNormalScale");
        AddTilingOffsetRows("DetailTiling", "DetailOffset",        ref y);
        AddUVSetRow(ref y);
    }

    #endregion

    #region Generic section (fallback for custom shaders)

    private void BuildGenericSection(ref int y)
    {
        if (_current is null) return;

        foreach (var (paramName, prop) in _current.Properties)
        {
            Control? input = prop.Type switch
            {
                EditorMaterialPropertyType.Float     => MakeFloatBox(prop),
                EditorMaterialPropertyType.Color     => MakeColorSwatch(prop),
                EditorMaterialPropertyType.Texture2D => MakeTextureBrowse(prop),
                EditorMaterialPropertyType.Vector2   => MakeVectorBox(prop, 2),
                EditorMaterialPropertyType.Vector3   => MakeVectorBox(prop, 3),
                EditorMaterialPropertyType.Vector4   => MakeVectorBox(prop, 4),
                _                                    => null,
            };
            if (input is null) continue;

            var row = new Panel
            {
                BackColor = BgColor,
                Width     = PanelWidth,
                Height    = RowFlat + 2,
                Location  = new SdPoint(0, y),
            };
            row.Controls.Add(MakeLabel(paramName, LeftPad, 3, LabelWidth));
            input.Location = new SdPoint(LeftPad + LabelWidth + 4, 2);
            row.Controls.Add(input);
            _propertiesArea.Controls.Add(row);
            y += row.Height;
        }
    }

    #endregion

    #region Row builders

    // ── Encabezado de sección ────────────────────────────────────────────────

    private void AddSectionHeader(string title, ref int y)
    {
        var panel = new Panel
        {
            BackColor = SectionColor,
            Width     = PanelWidth,
            Height    = SectionHeight,
            Location  = new SdPoint(0, y),
        };
        var lbl = new Label
        {
            Text      = title,
            ForeColor = TextColor,
            BackColor = SdColor.Transparent,
            Font      = new Font(Font.FontFamily, 8f, FontStyle.Bold),
            AutoSize  = true,
            Location  = new SdPoint(LeftPad, (SectionHeight - 14) / 2),
        };
        panel.Controls.Add(lbl);
        _propertiesArea.Controls.Add(panel);
        y += SectionHeight + 1;
    }

    // ── Fila de textura (miniatura + swatch de color / entrada flotante opcionales) ──

    private void AddTextureRow(
        string label,
        string textureKey,
        ref int y,
        string? colorKey  = null,
        string? floatKey  = null,
        string? sliderKey = null)
    {
        if (_current is null) return;

        var row = new Panel
        {
            BackColor = BgColor,
            Width     = PanelWidth,
            Height    = RowTexture + 4,
            Location  = new SdPoint(0, y),
        };

        // Punto indicador
        var dot = new Label
        {
            Text      = "◉",
            ForeColor = DimText,
            BackColor = SdColor.Transparent,
            Font      = new Font(Font.FontFamily, 7f),
            AutoSize  = true,
            Location  = new SdPoint(LeftPad, (RowTexture - 10) / 2),
        };

        // Etiqueta de propiedad
        var lbl = MakeLabel(label, LeftPad + 14, (RowTexture - 14) / 2, LabelWidth - 14);

        // PictureBox de miniatura de textura
        string? currentPath = GetTexturePath(textureKey);
        var thumb = new PictureBox
        {
            Size        = new Size(TextureSize, TextureSize),
            SizeMode    = PictureBoxSizeMode.Zoom,
            BackColor   = SdColor.FromArgb(30, 30, 30),
            BorderStyle = BorderStyle.None,
            Location    = new SdPoint(LeftPad + LabelWidth + 2, 4),
            Tag         = textureKey,
            Cursor      = Cursors.Hand,
        };
        SetThumbImage(thumb, currentPath);

        thumb.Click += (_, _) => BrowseTexture(textureKey, thumb);

        int rightX = thumb.Right + 6;
        int rightW = PanelWidth - rightX - LeftPad;

        // Swatch de color opcional
        if (colorKey is not null)
        {
            var swatch = MakeColorSwatchBtn(colorKey, rightX, 4 + (TextureSize - 20) / 2, 40, 20);
            row.Controls.Add(swatch);
            rightX += 46;
            rightW -= 46;
        }

        // Entrada flotante/deslizante opcional
        if (floatKey is not null)
        {
            var floatBox = MakeSmallFloat(floatKey, rightX, 4 + (TextureSize - 22) / 2, rightW);
            row.Controls.Add(floatBox);
        }
        else if (sliderKey is not null)
        {
            var sliderPanel = MakeSliderWithValue(sliderKey, rightX, 4 + (TextureSize - 22) / 2, rightW);
            row.Controls.Add(sliderPanel);
        }

        row.Controls.Add(dot);
        row.Controls.Add(lbl);
        row.Controls.Add(thumb);

        _propertiesArea.Controls.Add(row);
        y += row.Height;
    }

    // ── Fila solo con deslizador (sin miniatura de textura, sangría opcional) ──

    private void AddSliderOnlyRow(string label, string key, ref int y, bool indented = false)
    {
        if (_current is null) return;

        int indent = indented ? 28 : LeftPad;
        var row = new Panel
        {
            BackColor = BgColor,
            Width     = PanelWidth,
            Height    = RowFlat + 4,
            Location  = new SdPoint(0, y),
        };
        row.Controls.Add(MakeLabel(label, indent, 4, LabelWidth));
        var slider = MakeSliderWithValue(key, indent + LabelWidth + 4, 4, PanelWidth - indent - LabelWidth - LeftPad - 12);
        row.Controls.Add(slider);

        _propertiesArea.Controls.Add(row);
        y += row.Height;
    }

    // ── Filas de Tiling / Offset ─────────────────────────────────────────────

    private void AddTilingOffsetRows(string tilingKey, string offsetKey, ref int y)
    {
        AddVector2Row("Tiling", tilingKey, ref y);
        AddVector2Row("Offset", offsetKey, ref y);
    }

    private void AddVector2Row(string label, string key, ref int y)
    {
        if (_current is null) return;

        float[] xy = GetVec2(key);

        var row = new Panel
        {
            BackColor = BgColor,
            Width     = PanelWidth,
            Height    = RowFlat + 4,
            Location  = new SdPoint(0, y),
        };
        row.Controls.Add(MakeLabel(label, LeftPad, 4, 46));

        int fieldW = 52;
        row.Controls.Add(MakeLabel("X", LeftPad + 50, 4, 14));
        var xBox = MakeNumeric(xy[0], LeftPad + 64, 3, fieldW);
        xBox.ValueChanged += (_, _) => { if (!_suppress) SetVec2(key, (float)xBox.Value, GetVec2(key)[1]); };

        row.Controls.Add(MakeLabel("Y", LeftPad + 122, 4, 14));
        var yBox = MakeNumeric(xy[1], LeftPad + 136, 3, fieldW);
        yBox.ValueChanged += (_, _) => { if (!_suppress) SetVec2(key, GetVec2(key)[0], (float)yBox.Value); };

        row.Controls.Add(xBox);
        row.Controls.Add(yBox);
        _propertiesArea.Controls.Add(row);
        y += row.Height;
    }

    // ── Fila UV Set ────────────────────────────────────────────────────────────

    private void AddUVSetRow(ref int y)
    {
        if (_current is null) return;

        var row = new Panel
        {
            BackColor = BgColor,
            Width     = PanelWidth,
            Height    = RowFlat + 4,
            Location  = new SdPoint(0, y),
        };
        row.Controls.Add(MakeLabel("UV Set", LeftPad, 4, LabelWidth));
        var cb = MakeComboBox(LeftPad + LabelWidth + 4, 3, 80);
        cb.Items.AddRange(new object[] { "UV0", "UV1" });
        cb.SelectedIndex = _current.UVSet == 1 ? 1 : 0;
        cb.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppress && _current is not null)
                _current.UVSet = cb.SelectedIndex;
        };
        row.Controls.Add(cb);
        _propertiesArea.Controls.Add(row);
        y += row.Height;
    }

    #endregion

    #region Control factories

    private Label MakeLabel(string text, int x, int y, int width)
    {
        return new Label
        {
            Text      = text,
            ForeColor = TextColor,
            BackColor = SdColor.Transparent,
            Width     = width,
            Height    = 20,
            Location  = new SdPoint(x, y),
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private ComboBox MakeComboBox(int x, int y, int width)
    {
        return new ComboBox
        {
            DropDownStyle   = ComboBoxStyle.DropDown,
            FlatStyle       = FlatStyle.Flat,
            BackColor       = InputBg,
            ForeColor       = TextColor,
            Width           = width,
            Location        = new SdPoint(x, y),
        };
    }

    private Button MakeButton(string text, int x, int y, int w, int h)
    {
        return new Button
        {
            Text      = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = SdColor.FromArgb(68, 68, 68),
            ForeColor = TextColor,
            Width     = w,
            Height    = h,
            Location  = new SdPoint(x, y),
        };
    }

    private NumericUpDown MakeNumeric(float value, int x, int y, int width)
    {
        var n = new NumericUpDown
        {
            Minimum       = -9999m,
            Maximum       = 9999m,
            DecimalPlaces = 3,
            Increment     = 0.01m,
            Value         = (decimal)Math.Clamp(value, -9999f, 9999f),
            Width         = width,
            Height        = RowFlat,
            Location      = new SdPoint(x, y),
            BackColor     = InputBg,
            ForeColor     = TextColor,
            BorderStyle   = BorderStyle.FixedSingle,
        };
        return n;
    }

    // Caja de flotante vinculada a una clave de propiedad
    private NumericUpDown MakeFloatBox(EditorMaterialProperty prop)
    {
        float v = prop.Data?[0] ?? 0f;
        var n = MakeNumeric(v, 0, 0, 100);
        n.ValueChanged += (_, _) =>
        {
            if (!_suppress) prop.Data = [(float)n.Value];
        };
        return n;
    }

    // Entrada flotante pequeña vinculada a una clave de propiedad con nombre
    private NumericUpDown MakeSmallFloat(string key, int x, int y, int width)
    {
        float v = GetFloat(key);
        var n = MakeNumeric(v, x, y, Math.Max(width, 50));
        n.ValueChanged += (_, _) =>
        {
            if (!_suppress && _current is not null)
                SetFloat(key, (float)n.Value);
        };
        return n;
    }

    // Panel deslizante (TrackBar + etiqueta con valor) vinculado a una clave de propiedad [0..1]
    private Panel MakeSliderWithValue(string key, int x, int y, int width)
    {
        float v = Math.Clamp(GetFloat(key), 0f, 1f);
        int   w = Math.Max(width, 60);

        var panel = new Panel
        {
            BackColor = SdColor.Transparent,
            Width     = w,
            Height    = RowFlat,
            Location  = new SdPoint(x, y),
        };

        int numW = 38;
        var track = new TrackBar
        {
            Minimum     = 0,
            Maximum     = 100,
            TickFrequency= 10,
            TickStyle   = TickStyle.None,
            Value       = (int)(v * 100),
            Width       = w - numW - 4,
            Height      = RowFlat,
            Location    = new SdPoint(0, 0),
            BackColor   = BgColor,
        };
        var valueLabel = new Label
        {
            Text      = v.ToString("F2"),
            ForeColor = TextColor,
            BackColor = SdColor.Transparent,
            Width     = numW,
            Height    = RowFlat,
            Location  = new SdPoint(track.Width + 2, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        track.ValueChanged += (_, _) =>
        {
            float newVal = track.Value / 100f;
            valueLabel.Text = newVal.ToString("F2");
            if (!_suppress && _current is not null)
                SetFloat(key, newVal);
        };

        panel.Controls.Add(track);
        panel.Controls.Add(valueLabel);
        return panel;
    }

    // Botón de swatch de color vinculado a una clave de propiedad
    private Button MakeColorSwatchBtn(string key, int x, int y, int w, int h)
    {
        float[] rgba = GetColor(key);
        var btn = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = FloatsToWinColor(rgba),
            Width     = w,
            Height    = h,
            Location  = new SdPoint(x, y),
        };
        btn.FlatAppearance.BorderColor = SwatchBorder;
        btn.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = btn.BackColor };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            btn.BackColor = dlg.Color;
            SetColor(key, WinColorToFloats(dlg.Color));
        };
        return btn;
    }

    // Swatch de color para lista de propiedades genérica
    private Button MakeColorSwatch(EditorMaterialProperty prop)
    {
        float r = prop.Data?[0] ?? 1f, g = prop.Data?[1] ?? 1f, b = prop.Data?[2] ?? 1f, a = prop.Data?[3] ?? 1f;
        var btn = new Button
        {
            FlatStyle = FlatStyle.Flat,
            BackColor = SdColor.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255)),
            Width     = 80,
            Height    = RowFlat - 2,
        };
        btn.FlatAppearance.BorderColor = SwatchBorder;
        btn.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = btn.BackColor };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            btn.BackColor = dlg.Color;
            prop.Data = WinColorToFloats(dlg.Color);
        };
        return btn;
    }

    // Panel de exploración de textura para lista de propiedades genérica
    private Panel MakeTextureBrowse(EditorMaterialProperty prop)
    {
        var panel = new Panel { Width = 160, Height = RowFlat - 2, BackColor = SdColor.Transparent };
        var lbl = new Label
        {
            Text      = string.IsNullOrEmpty(prop.TexturePath) ? "(none)" : prop.TexturePath,
            ForeColor = DimText,
            BackColor = InputBg,
            Width     = 126,
            Height    = RowFlat - 2,
            Location  = new SdPoint(0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };
        var btn = new Button
        {
            Text      = "...",
            FlatStyle = FlatStyle.Flat,
            BackColor = SdColor.FromArgb(68, 68, 68),
            ForeColor = TextColor,
            Width     = 28,
            Height    = RowFlat - 2,
            Location  = new SdPoint(128, 0),
        };
        btn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Textures|*.png;*.jpg;*.bmp;*.tga" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            string rel = Path.GetRelativePath(_context?.ActiveProject?.ContentPath ?? "", dlg.FileName);
            rel              = Path.ChangeExtension(rel, null);
            prop.TexturePath = rel;
            lbl.Text         = rel;
        };
        panel.Controls.Add(lbl);
        panel.Controls.Add(btn);
        return panel;
    }

    // Campo múltiple de vector para lista de propiedades genérica
    private Panel MakeVectorBox(EditorMaterialProperty prop, int components)
    {
        prop.Data ??= new float[components];
        int cw    = 74 / components;
        var panel = new Panel { Width = 80, Height = RowFlat - 2, BackColor = SdColor.Transparent };
        for (int i = 0; i < components; i++)
        {
            int idx = i;
            var n   = MakeNumeric(idx < prop.Data.Length ? prop.Data[idx] : 0f, idx * (cw + 2), 0, cw);
            n.ValueChanged += (_, _) =>
            {
                if (!_suppress && idx < prop.Data.Length)
                    prop.Data[idx] = (float)n.Value;
            };
            panel.Controls.Add(n);
        }
        return panel;
    }

    #endregion

    #region Texture thumbnail helpers

    private void BrowseTexture(string key, PictureBox thumb)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Textures|*.png;*.jpg;*.bmp;*.tga",
            Title  = $"Select texture for '{key}'",
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        string root = _context?.ActiveProject?.ContentPath ?? string.Empty;
        string rel  = Path.GetRelativePath(root, dlg.FileName);
        rel = Path.ChangeExtension(rel, null);

        // Actualizar propiedad
        if (_current is not null)
        {
            _current.Properties[key] = new EditorMaterialProperty
            {
                Name        = key,
                Type        = EditorMaterialPropertyType.Texture2D,
                TexturePath = rel,
            };
            // Establecer automáticamente la bandera HasXXXMap
            string flagKey = "Has" + key; // e.g. HasNormalTexture — but shader uses e.g. HasNormalMap
            // Correspondencia clave de textura → bandera HasXXX
            string? flagName = TextureKeyToFlag(key);
            if (flagName is not null)
                SetFloat(flagName, 1f);
        }

        SetThumbImage(thumb, dlg.FileName);
    }

    private static void SetThumbImage(PictureBox thumb, string? path)
    {
        thumb.Image?.Dispose();
        thumb.Image = null;

        if (string.IsNullOrEmpty(path)) return;

        // Intentar cargar desde ruta absoluta primero, luego desde ruta relativa al contenido
        string absPath = File.Exists(path) ? path : string.Empty;
        if (string.IsNullOrEmpty(absPath)) return;

        try
        {
            thumb.Image = System.Drawing.Image.FromFile(absPath);
        }
        catch
        {
            thumb.Image = null;
        }
    }

    private static string? TextureKeyToFlag(string textureKey) => textureKey switch
    {
        "AlbedoTexture"       => "HasAlbedoMap",
        "MetallicTexture"     => "HasMetallicMap",
        "NormalTexture"       => "HasNormalMap",
        "HeightTexture"       => "HasHeightMap",
        "OcclusionTexture"    => "HasOcclusionMap",
        "EmissionTexture"     => "HasEmissionMap",
        "DetailMaskTexture"   => "HasDetailMask",
        "DetailAlbedoTexture" => "HasDetailAlbedoMap",
        "DetailNormalTexture" => "HasDetailNormalMap",
        _                     => null,
    };

    private string? GetTexturePath(string key)
    {
        if (_current is null) return null;
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p)) return null;
        return p.TexturePath;
    }

    #endregion

    #region Property data helpers

    private float GetFloat(string key)
    {
        if (_current is null) return 0f;
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p)) return 0f;
        return p.Data?[0] ?? 0f;
    }

    private void SetFloat(string key, float value)
    {
        if (_current is null) return;
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p))
        {
            p = new EditorMaterialProperty { Name = key, Type = EditorMaterialPropertyType.Float };
            _current.Properties[key] = p;
        }
        p.Data = [value];
    }

    private float[] GetColor(string key)
    {
        if (_current is null) return [1f, 1f, 1f, 1f];
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p)) return [1f, 1f, 1f, 1f];
        return p.Data ?? [1f, 1f, 1f, 1f];
    }

    private void SetColor(string key, float[] rgba)
    {
        if (_current is null) return;
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p))
        {
            p = new EditorMaterialProperty { Name = key, Type = EditorMaterialPropertyType.Color };
            _current.Properties[key] = p;
        }
        p.Data = rgba;
    }

    private float[] GetVec2(string key)
    {
        if (_current is null) return [0f, 0f];
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p)) return [0f, 0f];
        float[] d = p.Data ?? [0f, 0f];
        return d.Length >= 2 ? d : [d.Length > 0 ? d[0] : 0f, 0f];
    }

    private void SetVec2(string key, float x, float y)
    {
        if (_current is null) return;
        if (!_current.Properties.TryGetValue(key, out EditorMaterialProperty? p))
        {
            p = new EditorMaterialProperty { Name = key, Type = EditorMaterialPropertyType.Vector2 };
            _current.Properties[key] = p;
        }
        p.Data = [x, y];
    }

    private static SdColor FloatsToWinColor(float[] rgba)
    {
        float r = rgba.Length > 0 ? rgba[0] : 1f;
        float g = rgba.Length > 1 ? rgba[1] : 1f;
        float b = rgba.Length > 2 ? rgba[2] : 1f;
        float a = rgba.Length > 3 ? rgba[3] : 1f;
        return SdColor.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static float[] WinColorToFloats(SdColor c) =>
        [c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f];

    #endregion

    #region Helpers

    private static EditorMaterial LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                return JsonSerializer.Deserialize<EditorMaterial>(File.ReadAllText(path))
                    ?? EditorMaterial.CreateEmpty();
            }
            catch { /* continuar */ }
        }
        return EditorMaterial.CreateEmpty();
    }

    private void ShowContent(bool show)
    {
        _noSel.Visible          = !show;
        _header.Visible         = show;
        _renderModeRow.Visible  = show;
        _propertiesArea.Visible = show;
        _previewArea.Visible    = show;
    }

    #endregion
}
