using System.Text.Json;
using MonoGame.Editor.Core.Assets;
using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Core.Models;
using MonoGame.Editor.WinForms.Rendering;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Displays and edits a material asset (.mat.json).
/// Dynamically generates property controls from the shader's <see cref="EffectParameter"/> list.
/// Also hosts a 3D sphere preview rendered by <see cref="MaterialPreviewRenderer"/>.
/// </summary>
public sealed class MaterialInspectorPanel : UserControl
{
    #region Constants

    private const int LabelWidth  = 110;
    private const int InputWidth  = 130;
    private const int RowHeight   = 28;
    private const int SidePad     = 8;
    private const int PreviewSize = 256;

    #endregion

    #region Fields

    private EditorContext?           _context;
    private AssetInfo?               _currentAsset;
    private EditorMaterial?          _currentMaterial;
    private bool                     _suppressUpdate;

    private readonly Panel           _scroll;
    private readonly Label           _noSelectionLabel;
    private readonly Label           _titleLabel;
    private readonly TextBox         _shaderPathBox;
    private readonly Panel           _propertiesPanel;
    private readonly PictureBox      _previewBox;
    private readonly Button          _saveButton;
    private readonly Button          _renderPreviewButton;

    // Fired when the material changes — wired by EditorForm to trigger the preview render
    private Action<EditorMaterial>?  _onRenderPreviewRequested;

    #endregion

    #region Constructor

    /// <summary>Creates the panel. Call <see cref="Initialize"/> to connect to the editor context.</summary>
    public MaterialInspectorPanel()
    {
        _scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Controls.Add(_scroll);

        _noSelectionLabel = new Label
        {
            Text      = "Select a material asset (.mat.json) to edit it.",
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            ForeColor = System.Drawing.Color.Gray,
        };
        _scroll.Controls.Add(_noSelectionLabel);

        int y = SidePad;

        _titleLabel = new Label
        {
            Font     = new System.Drawing.Font(Font.FontFamily, 9f, System.Drawing.FontStyle.Bold),
            AutoSize = true,
            Location = new System.Drawing.Point(SidePad, y),
        };
        y += 22;

        // Shader path
        var shaderLabel = new Label
        {
            Text     = "Shader Path:",
            Width    = LabelWidth,
            Height   = RowHeight,
            Location = new System.Drawing.Point(SidePad, y),
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        };
        _shaderPathBox = new TextBox
        {
            Width    = InputWidth + 40,
            Location = new System.Drawing.Point(SidePad + LabelWidth + 4, y + 2),
        };
        _shaderPathBox.TextChanged += (_, _) =>
        {
            if (!_suppressUpdate && _currentMaterial is not null)
                _currentMaterial.ShaderPath = _shaderPathBox.Text;
        };
        y += RowHeight + 4;

        // Properties panel (dynamic, rebuilt on material load)
        _propertiesPanel = new Panel
        {
            Location = new System.Drawing.Point(SidePad, y),
            Width    = 350,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        y += 4;

        // Preview
        var previewLabel = new Label
        {
            Text     = "Preview:",
            AutoSize = true,
            Location = new System.Drawing.Point(SidePad, y + _propertiesPanel.Height + 8),
        };
        _previewBox = new PictureBox
        {
            Size        = new System.Drawing.Size(PreviewSize, PreviewSize),
            SizeMode    = PictureBoxSizeMode.Normal,
            BorderStyle = BorderStyle.FixedSingle,
            Location    = new System.Drawing.Point(SidePad, y + _propertiesPanel.Height + 24),
        };

        _renderPreviewButton = new Button
        {
            Text     = "Render Preview",
            Width    = 120,
            Height   = 28,
            Location = new System.Drawing.Point(SidePad, y + _propertiesPanel.Height + PreviewSize + 32),
        };
        _renderPreviewButton.Click += OnRenderPreview;

        _saveButton = new Button
        {
            Text     = "Save .mat.json",
            Width    = 120,
            Height   = 28,
            Location = new System.Drawing.Point(SidePad + 128, y + _propertiesPanel.Height + PreviewSize + 32),
        };
        _saveButton.Click += OnSave;

        _scroll.Controls.AddRange(
        [
            _titleLabel,
            shaderLabel,
            _shaderPathBox,
            _propertiesPanel,
            previewLabel,
            _previewBox,
            _renderPreviewButton,
            _saveButton,
        ]);

        ShowContent(false);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Connects this panel to the editor context.
    /// Optionally provide <paramref name="onRenderPreviewRequested"/> to hook the 3D render.
    /// </summary>
    public void Initialize(EditorContext context, Action<EditorMaterial>? onRenderPreviewRequested = null)
    {
        _context                   = context;
        _onRenderPreviewRequested  = onRenderPreviewRequested;
        _context.EventBus.Subscribe<AssetSelectedEvent>(OnAssetSelected);
    }

    /// <summary>
    /// Updates the preview image from the render thread result. Safe to call from the render thread
    /// (invokes on the UI thread internally).
    /// </summary>
    public void SetPreviewBitmap(System.Drawing.Bitmap bitmap)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetPreviewBitmap(bitmap));
            return;
        }

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
            _currentAsset    = null;
            _currentMaterial = null;
            ShowContent(false);
            return;
        }

        _currentAsset    = evt.Asset;
        _currentMaterial = LoadOrCreate(evt.Asset.AbsolutePath);
        PopulateControls();
        ShowContent(true);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_currentAsset is null || _currentMaterial is null) return;
        WriteControlsToMaterial();

        string json = JsonSerializer.Serialize(_currentMaterial, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_currentAsset.AbsolutePath, json);
    }

    private void OnRenderPreview(object? sender, EventArgs e)
    {
        if (_currentMaterial is not null)
            _onRenderPreviewRequested?.Invoke(_currentMaterial);
    }

    #endregion

    #region Populate / write

    private void PopulateControls()
    {
        if (_currentMaterial is null || _currentAsset is null) return;
        _suppressUpdate = true;

        _titleLabel.Text   = _currentMaterial.Name;
        _shaderPathBox.Text = _currentMaterial.ShaderPath;

        BuildPropertyRows();

        _suppressUpdate = false;
    }

    private void BuildPropertyRows()
    {
        _propertiesPanel.Controls.Clear();
        if (_currentMaterial is null) return;

        int y = 0;
        foreach (var (paramName, prop) in _currentMaterial.Properties)
        {
            Control? input = prop.Type switch
            {
                EditorMaterialPropertyType.Float     => BuildFloatInput(prop),
                EditorMaterialPropertyType.Color     => BuildColorInput(prop),
                EditorMaterialPropertyType.Texture2D => BuildTextureInput(prop),
                EditorMaterialPropertyType.Vector2   => BuildVectorInput(prop, 2),
                EditorMaterialPropertyType.Vector3   => BuildVectorInput(prop, 3),
                EditorMaterialPropertyType.Vector4   => BuildVectorInput(prop, 4),
                _                                    => null,
            };

            if (input is null) continue;

            var label = new Label
            {
                Text      = paramName,
                Width     = LabelWidth,
                Height    = RowHeight,
                Location  = new System.Drawing.Point(0, y),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            };
            input.Location = new System.Drawing.Point(LabelWidth + 4, y + 2);

            _propertiesPanel.Controls.Add(label);
            _propertiesPanel.Controls.Add(input);
            y += RowHeight + 2;
        }
    }

    private void WriteControlsToMaterial()
    {
        // Properties are written inline via event handlers in the input controls
    }

    #endregion

    #region Dynamic control builders

    private NumericUpDown BuildFloatInput(EditorMaterialProperty prop)
    {
        var input = new NumericUpDown
        {
            Width       = InputWidth,
            Height      = RowHeight - 4,
            DecimalPlaces = 4,
            Minimum     = -9999,
            Maximum     = 9999,
            Increment   = 0.01m,
            Value       = (decimal)(prop.Data?[0] ?? 0f),
        };
        input.ValueChanged += (_, _) =>
        {
            if (!_suppressUpdate)
                prop.Data = [(float)input.Value];
        };
        return input;
    }

    private Button BuildColorInput(EditorMaterialProperty prop)
    {
        float r = prop.Data?[0] ?? 1f;
        float g = prop.Data?[1] ?? 1f;
        float b = prop.Data?[2] ?? 1f;
        float a = prop.Data?[3] ?? 1f;

        var btn = new Button
        {
            Width     = InputWidth,
            Height    = RowHeight - 4,
            BackColor = System.Drawing.Color.FromArgb(
                (int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255)),
            FlatStyle = FlatStyle.Flat,
        };
        btn.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = btn.BackColor };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            btn.BackColor = dlg.Color;
            prop.Data = [dlg.Color.R / 255f, dlg.Color.G / 255f, dlg.Color.B / 255f, dlg.Color.A / 255f];
        };
        return btn;
    }

    private Panel BuildTextureInput(EditorMaterialProperty prop)
    {
        var panel = new Panel { Width = InputWidth + 30, Height = RowHeight - 4 };

        var pathLabel = new Label
        {
            Text     = prop.TexturePath ?? "(none)",
            AutoSize = false,
            Width    = InputWidth - 30,
            Height   = RowHeight - 4,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Location = new System.Drawing.Point(0, 0),
        };
        var btn = new Button
        {
            Text     = "...",
            Width    = 28,
            Height   = RowHeight - 4,
            Location = new System.Drawing.Point(InputWidth - 28, 0),
        };
        btn.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "Texture files|*.png;*.jpg;*.bmp;*.tga",
                Title  = "Select Texture Asset",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            string relative = Path.GetRelativePath(
                _context?.ActiveProject?.ContentPath ?? string.Empty, dlg.FileName);
            relative = Path.ChangeExtension(relative, null); // strip extension for content path
            prop.TexturePath  = relative;
            pathLabel.Text    = relative;
        };

        panel.Controls.Add(pathLabel);
        panel.Controls.Add(btn);
        return panel;
    }

    private Panel BuildVectorInput(EditorMaterialProperty prop, int components)
    {
        prop.Data ??= new float[components];

        var panel = new Panel { Width = InputWidth + 10, Height = RowHeight - 4 };
        int compWidth = (InputWidth + 10) / components - 2;

        for (int i = 0; i < components; i++)
        {
            int index = i;
            var input = new NumericUpDown
            {
                Width         = compWidth,
                Height        = RowHeight - 4,
                DecimalPlaces = 3,
                Minimum       = -9999,
                Maximum       = 9999,
                Increment     = 0.01m,
                Value         = index < prop.Data.Length ? (decimal)prop.Data[index] : 0m,
                Location      = new System.Drawing.Point(i * (compWidth + 2), 0),
            };
            input.ValueChanged += (_, _) =>
            {
                if (!_suppressUpdate && index < prop.Data.Length)
                    prop.Data[index] = (float)input.Value;
            };
            panel.Controls.Add(input);
        }

        return panel;
    }

    #endregion

    #region Helpers

    private static EditorMaterial LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<EditorMaterial>(json)
                    ?? EditorMaterial.CreateEmpty();
            }
            catch { /* fall through */ }
        }

        return EditorMaterial.CreateEmpty();
    }

    private void ShowContent(bool show)
    {
        _noSelectionLabel.Visible    = !show;
        _titleLabel.Visible          = show;
        _shaderPathBox.Visible       = show;
        _propertiesPanel.Visible     = show;
        _previewBox.Visible          = show;
        _renderPreviewButton.Visible = show;
        _saveButton.Visible          = show;

        foreach (Control c in _scroll.Controls)
            if (c is Label l && l != _titleLabel && l != _noSelectionLabel)
                l.Visible = show;
    }

    #endregion
}
