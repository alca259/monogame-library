using System.Text.Json;
using Microsoft.Maui.Controls.Shapes;
using MonoGame.Editor.Maui.Controls;
using MonoGame.Editor.Maui.Rendering;
using MonoGame.Editor.Maui.Views.Dialogs;
using SysPath = System.IO.Path;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Inspector tab "Material Editor". Displays fixed header fields (shader, rendering mode)
/// plus shader-aware dynamic sections for each built-in shader (StandardEffect, SpriteTint,
/// Grayscale, Vignette) and a generic fallback for custom project shaders.
/// </summary>
public sealed partial class MaterialInspectorView : ContentView
{
    #region Fields

    private static readonly string[] BuiltInShaders =
    [
        "Shaders/StandardEffect",
        "Shaders/SpriteTint",
        "Shaders/Grayscale",
        "Shaders/Vignette",
    ];

    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    // Getters keyed by property name; each returns the current EditorMaterialProperty from the UI.
    private readonly Dictionary<string, Func<EditorMaterialProperty>> _propGetters =
        new(StringComparer.Ordinal);

    private EditorMaterial? _material;
    private Action<AssetSelectedEvent>? _onAssetSelected;
    private Action<ProjectOpenedEvent>? _onProjectOpened;
    private string _currentFilePath      = string.Empty;
    private string _projectContentRoot   = string.Empty;
    private bool   _suppressShaderChange = false;

    #endregion

    #region Constructor

    public MaterialInspectorView()
    {
        InitializeComponent();
    }

    #endregion

    #region Lifecycle

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onAssetSelected = e => MainThread.BeginInvokeOnMainThread(() => OnAssetSelected(e));
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _bus.Subscribe(_onAssetSelected);
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onAssetSelected is not null) _bus.Unsubscribe(_onAssetSelected);
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    #endregion

    #region Event handlers

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _projectContentRoot = e.Project?.ContentPath ?? string.Empty;
        PopulateShaderPicker();
    }

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.Material)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        string name = SysPath.GetFileName(_currentFilePath);
        _material = LoadOrCreate(_currentFilePath, SysPath.GetFileNameWithoutExtension(name));
        PopulateForm(_material, name);
        MaterialSaveButton.IsEnabled = true;
        RenderButton.IsEnabled       = true;
        PreviewBorder.IsVisible      = true;
        _ = UpdatePreviewAsync();
    }

    private void OnShaderPickerChanged(object sender, EventArgs e)
    {
        if (_suppressShaderChange) return;
        RebuildShaderSections();
    }

    private void OnRenderClicked(object sender, EventArgs e) =>
        _ = UpdatePreviewAsync();

    private async Task UpdatePreviewAsync()
    {
        if (_material is null) return;

        CollectFormIntoMaterial();
        SaveMaterialToDisk();

        RenderButton.IsEnabled   = false;
        MaterialStatusLabel.Text = "Rendering…";

        try
        {
            string root = _projectContentRoot;
            EditorMaterial mat = _material;
            byte[] png = await Task.Run(() => MaterialPreviewRenderer.Render(mat, root))
                                   .ConfigureAwait(true);
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
            MaterialStatusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            MaterialStatusLabel.Text = $"Preview error: {ex.Message}";
        }
        finally
        {
            RenderButton.IsEnabled = true;
        }
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (_material is null) return;
        CollectFormIntoMaterial();
        SaveMaterialToDisk();
    }

    private void CollectFormIntoMaterial()
    {
        if (_material is null) return;

        _material.ShaderPath    = ShaderPicker.SelectedItem as string ?? string.Empty;
        _material.RenderingMode = RenderingModePicker.SelectedIndex switch
        {
            1 => "Cutout",
            2 => "Fade",
            3 => "Transparent",
            _ => "Opaque",
        };
        _material.UVSet = _uvSet;

        foreach (KeyValuePair<string, Func<EditorMaterialProperty>> kv in _propGetters)
            _material.Properties[kv.Key] = kv.Value();
    }

    private void SaveMaterialToDisk()
    {
        if (string.IsNullOrEmpty(_currentFilePath) || _material is null) return;

        try
        {
            string json = JsonSerializer.Serialize(_material,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            MaterialStatusLabel.Text = "Saved";
        }
        catch (Exception ex)
        {
            MaterialStatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Form management

    private void PopulateShaderPicker()
    {
        string? current = ShaderPicker.SelectedItem as string;
        _suppressShaderChange = true;
        ShaderPicker.Items.Clear();

        foreach (string s in BuiltInShaders)
            ShaderPicker.Items.Add(s);

        if (!string.IsNullOrEmpty(_projectContentRoot) && Directory.Exists(_projectContentRoot))
        {
            foreach (string fx in Directory.GetFiles(_projectContentRoot, "*.fx",
                             SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string rel = SysPath.GetRelativePath(_projectContentRoot, fx);
                if (!ShaderPicker.Items.Contains(rel))
                    ShaderPicker.Items.Add(rel);
            }
        }

        int idx = string.IsNullOrEmpty(current) ? -1 : ShaderPicker.Items.IndexOf(current);
        ShaderPicker.SelectedIndex = idx;
        _suppressShaderChange = false;
    }

    private void PopulateForm(EditorMaterial mat, string displayName)
    {
        MaterialFileLabel.Text = displayName;

        _suppressShaderChange = true;
        int shaderIdx = ShaderPicker.Items.IndexOf(mat.ShaderPath);
        if (shaderIdx < 0 && !string.IsNullOrEmpty(mat.ShaderPath))
        {
            ShaderPicker.Items.Add(mat.ShaderPath);
            shaderIdx = ShaderPicker.Items.Count - 1;
        }
        ShaderPicker.SelectedIndex = shaderIdx;
        _suppressShaderChange = false;

        RenderingModePicker.SelectedIndex = mat.RenderingMode switch
        {
            "Cutout"      => 1,
            "Fade"        => 2,
            "Transparent" => 3,
            _             => 0,
        };

        RebuildShaderSections();
        MaterialStatusLabel.Text = string.Empty;
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        _material        = null;
        MaterialFileLabel.Text             = "(no material)";
        _suppressShaderChange              = true;
        ShaderPicker.SelectedIndex         = -1;
        _suppressShaderChange              = false;
        RenderingModePicker.SelectedIndex  = 0;
        ShaderSections.Children.Clear();
        _propGetters.Clear();
        _uvSet = 0;
        MaterialSaveButton.IsEnabled = false;
        RenderButton.IsEnabled       = false;
        PreviewBorder.IsVisible      = false;
        MaterialStatusLabel.Text = string.Empty;
    }

    private static EditorMaterial LoadOrCreate(string path, string fallbackName)
    {
        if (!File.Exists(path)) return EditorMaterial.CreateEmpty(fallbackName);
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorMaterial>(json)
                   ?? EditorMaterial.CreateEmpty(fallbackName);
        }
        catch
        {
            return EditorMaterial.CreateEmpty(fallbackName);
        }
    }

    #endregion

    #region Section builders

    private int _uvSet = 0; // tracks UV Set picker selection for save

    private void RebuildShaderSections()
    {
        ShaderSections.Children.Clear();
        _propGetters.Clear();

        string shader = ShaderPicker.SelectedItem as string ?? string.Empty;

        if (shader.Equals("Shaders/StandardEffect", StringComparison.OrdinalIgnoreCase))
            BuildStandardSections();
        else if (shader.Equals("Shaders/SpriteTint", StringComparison.OrdinalIgnoreCase))
            BuildSpriteTintSection();
        else if (shader.Equals("Shaders/Grayscale", StringComparison.OrdinalIgnoreCase))
            BuildSimpleIntensitySection("Effect");
        else if (shader.Equals("Shaders/Vignette", StringComparison.OrdinalIgnoreCase))
            BuildSimpleIntensitySection("Effect");
        else if (!string.IsNullOrEmpty(shader))
            BuildGenericSection();
    }

    private void BuildStandardSections()
    {
        // ── Main Maps ───────────────────────────────────────────────────
        ShaderSections.Children.Add(MakeSectionHeader("Main Maps"));

        // Albedo: dot · label · texture slot · color swatch
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Albedo",
                MakeTextureColorControls("AlbedoTexture", "AlbedoColor", defaultRgba: [1f, 1f, 1f, 1f])));

        AddSpacer(4);

        // Metallic: dot · label · texture slot · slider
        // Smoothness sub-row: no dot · label(indented) · slider
        ShaderSections.Children.Add(
            MakeMetallicSmoothnessRows());

        AddSpacer(4);

        // Normal Map: dot · label · texture slot · stepper
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Normal Map",
                MakeTextureStepperControls("NormalTexture", "NormalScale", 1f, 0.001)));

        AddSpacer(4);

        // Height Map
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Height Map",
                MakeTextureStepperControls("HeightTexture", "HeightScale", 0.02f, 0.001)));

        AddSpacer(4);

        // Occlusion: dot · label · texture slot (OcclusionStrength handled as stepper beside texture)
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Occlusion",
                MakeTextureStepperControls("OcclusionTexture", "OcclusionStrength", 1f, 0.01)));

        AddSpacer(4);

        // Emission: dot · label · texture slot · color swatch · stepper
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Emission",
                MakeTextureColorStepperControls(
                    "EmissionTexture", "EmissionColor", "EmissionIntensity",
                    defaultRgba: [0f, 0f, 0f, 1f], defaultIntensity: 0f)));

        AddSpacer(4);

        // Detail Mask: dot · label · texture slot
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Detail Mask",
                MakeTextureOnlyControls("DetailMaskTexture")));

        AddSpacer(8);

        // Tiling / Offset
        ShaderSections.Children.Add(MakeTilingOffsetRows("Tiling", "Offset"));

        // ── Secondary Maps ───────────────────────────────────────────────
        ShaderSections.Children.Add(MakeSectionHeader("Secondary Maps"));

        // Detail Albedo
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Detail Albedo",
                MakeTextureOnlyControls("DetailAlbedoTexture")));

        AddSpacer(4);

        // Normal Map (detail)
        ShaderSections.Children.Add(
            MakeRow(hasDot: true, "Normal Map",
                MakeTextureStepperControls("DetailNormalTexture", "DetailNormalScale", 1f, 0.001)));

        AddSpacer(8);

        // Tiling / Offset (detail)
        ShaderSections.Children.Add(MakeTilingOffsetRows("DetailTiling", "DetailOffset"));

        // UV Set
        ShaderSections.Children.Add(MakeUVSetRow());
    }

    private void BuildSpriteTintSection()
    {
        ShaderSections.Children.Add(MakeSectionHeader("Tint"));

        ShaderSections.Children.Add(
            MakeRow(hasDot: false, "Color",
                MakeColorOnlyControls("TintColor", defaultRgba: [1f, 1f, 1f, 1f])));

        AddSpacer(4);

        ShaderSections.Children.Add(
            MakeRow(hasDot: false, "Alpha",
                MakeSliderControls("Alpha", initialValue: 1f, min: 0f, max: 1f)));
    }

    private void BuildSimpleIntensitySection(string sectionTitle)
    {
        ShaderSections.Children.Add(MakeSectionHeader(sectionTitle));

        ShaderSections.Children.Add(
            MakeRow(hasDot: false, "Intensity",
                MakeSliderControls("Intensity", initialValue: 1f, min: 0f, max: 1f)));
    }

    private void BuildGenericSection()
    {
        if (_material is null) return;
        ShaderSections.Children.Add(MakeSectionHeader("Properties"));

        foreach (KeyValuePair<string, EditorMaterialProperty> kv in _material.Properties)
        {
            string name = kv.Key;
            EditorMaterialProperty prop = kv.Value;

            if (prop.Type == EditorMaterialPropertyType.Texture2D)
            {
                ShaderSections.Children.Add(
                    MakeRow(hasDot: true, name, MakeTextureOnlyControls(name)));
            }
            else if (prop.Type == EditorMaterialPropertyType.Color)
            {
                float[] rgba = prop.Data?.Length >= 4 ? prop.Data : [1f, 1f, 1f, 1f];
                ShaderSections.Children.Add(
                    MakeRow(hasDot: false, name, MakeColorOnlyControls(name, rgba)));
            }
            else if (prop.Type is EditorMaterialPropertyType.Float)
            {
                float val = prop.Data is { Length: > 0 } ? prop.Data[0] : 0f;
                ShaderSections.Children.Add(
                    MakeRow(hasDot: false, name,
                        MakeSliderControls(name, val, 0f, 1f)));
            }
            else
            {
                // Vector2/Vector3/Vector4 — plain steppers
                int count = prop.Type switch
                {
                    EditorMaterialPropertyType.Vector2 => 2,
                    EditorMaterialPropertyType.Vector3 => 3,
                    _                                  => 4,
                };
                ShaderSections.Children.Add(
                    MakeRow(hasDot: false, name,
                        MakeMultiStepperControls(name, prop.Data, count)));
            }

            AddSpacer(2);
        }
    }

    #endregion

    #region Row builders

    private static View MakeSectionHeader(string title)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };

        var header = new Border
        {
            BackgroundColor = (Color)Application.Current!.Resources["PanelBackgroundAlt"],
            StrokeThickness = 0,
            Padding         = new Thickness(10, 4),
        };
        header.Content = new Label
        {
            Text  = title,
            Style = (Style)Application.Current.Resources["SectionTitle"],
        };

        stack.Children.Add(header);
        stack.Children.Add(new BoxView
        {
            Color         = (Color)Application.Current.Resources["Border"],
            HeightRequest = 1,
        });
        return stack;
    }

    /// <summary>Wraps controls in a 3-column property row: [dot] [label] [controls].</summary>
    private View MakeRow(bool hasDot, string labelText, View controls)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(16)),
                new ColumnDefinition(new GridLength(90)),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 0,
            Padding       = new Thickness(10, 3, 10, 3),
        };

        if (hasDot)
        {
            var dot = new Border
            {
                WidthRequest    = 7,
                HeightRequest   = 7,
                BackgroundColor = (Color)Application.Current!.Resources["TextSecondary"],
                StrokeThickness = 0,
                StrokeShape     = new Ellipse(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions   = LayoutOptions.Center,
            };
            Grid.SetColumn(dot, 0);
            grid.Children.Add(dot);
        }

        var label = new Label
        {
            Text  = labelText,
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
        };
        Grid.SetColumn(label, 1);
        grid.Children.Add(label);

        Grid.SetColumn(controls, 2);
        grid.Children.Add(controls);

        return grid;
    }

    private View MakeMetallicSmoothnessRows()
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Children.Add(
            MakeRow(hasDot: true, "Metallic",
                MakeTextureSliderControls("MetallicTexture", "Metallic", 0f, 0f, 1f)));
        stack.Children.Add(
            MakeRow(hasDot: false, "Smoothness",
                MakeSliderControls("Smoothness", 0.5f, 0f, 1f)));
        return stack;
    }

    private View MakeTilingOffsetRows(string tilingKey, string offsetKey)
    {
        var stack = new VerticalStackLayout { Spacing = 0 };
        stack.Children.Add(MakeXYStepperRow("Tiling", tilingKey, defaultX: 1f, defaultY: 1f));
        stack.Children.Add(MakeXYStepperRow("Offset", offsetKey, defaultX: 0f, defaultY: 0f));
        return stack;
    }

    private View MakeXYStepperRow(string label, string propKey, float defaultX, float defaultY)
    {
        float x = GetPropComponent(propKey, 0, defaultX);
        float y = GetPropComponent(propKey, 1, defaultY);

        var xStepper = MakeStepper(x, 0.001);
        var yStepper = MakeStepper(y, 0.001);

        _propGetters[propKey] = () => new EditorMaterialProperty
        {
            Name = propKey,
            Type = EditorMaterialPropertyType.Vector2,
            Data = [(float)xStepper.Value, (float)yStepper.Value],
        };

        var controls = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        controls.Children.Add(new Label
        {
            Text    = "X",
            TextColor = (Color)Application.Current!.Resources["TextSecondary"],
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center,
        });
        controls.Children.Add(xStepper);
        controls.Children.Add(new Label
        {
            Text    = "Y",
            TextColor = (Color)Application.Current!.Resources["TextSecondary"],
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center,
        });
        controls.Children.Add(yStepper);

        return MakeRow(hasDot: false, label, controls);
    }

    private View MakeUVSetRow()
    {
        _uvSet = _material?.UVSet ?? 0;

        var picker = new Picker
        {
            VerticalOptions = LayoutOptions.Center,
        };
        picker.Items.Add("UV0");
        picker.Items.Add("UV1");
        picker.SelectedIndex = _uvSet <= 1 ? _uvSet : 0;
        picker.SelectedIndexChanged += (_, _) => _uvSet = picker.SelectedIndex;

        return MakeRow(hasDot: false, "UV Set", picker);
    }

    private void AddSpacer(double height)
    {
        ShaderSections.Children.Add(new BoxView
        {
            HeightRequest = height,
            Color         = Colors.Transparent,
        });
    }

    #endregion

    #region Control factories

    private View MakeTextureColorControls(string texKey, string colorKey, float[] defaultRgba)
    {
        var texSlot   = MakeTextureSlot(texKey);
        var colorSwatch = MakeColorSwatch(colorKey, defaultRgba);

        var panel = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        panel.Children.Add(texSlot);
        panel.Children.Add(colorSwatch);
        return panel;
    }

    private View MakeTextureOnlyControls(string texKey)
    {
        return MakeTextureSlot(texKey);
    }

    private View MakeColorOnlyControls(string colorKey, float[] defaultRgba)
    {
        return MakeColorSwatch(colorKey, defaultRgba);
    }

    private View MakeTextureSliderControls(string texKey, string sliderKey,
        float initialSlider, float min, float max)
    {
        var texSlot = MakeTextureSlot(texKey);
        var (sliderView, _) = MakeSliderWithLabel(sliderKey, initialSlider, min, max);

        var panel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(36)),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing   = 6,
            VerticalOptions = LayoutOptions.Center,
        };
        Grid.SetColumn(texSlot, 0);
        Grid.SetColumn(sliderView, 1);
        panel.Children.Add(texSlot);
        panel.Children.Add(sliderView);
        return panel;
    }

    private View MakeTextureStepperControls(string texKey, string stepKey,
        float defaultVal, double step)
    {
        var texSlot = MakeTextureSlot(texKey);
        var stepper = MakeStepper(GetPropFloat(stepKey, defaultVal), step);

        _propGetters[stepKey] = () => new EditorMaterialProperty
        {
            Name = stepKey,
            Type = EditorMaterialPropertyType.Float,
            Data = [(float)stepper.Value],
        };

        var panel = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        panel.Children.Add(texSlot);
        panel.Children.Add(stepper);
        return panel;
    }

    private View MakeTextureColorStepperControls(
        string texKey, string colorKey, string stepKey,
        float[] defaultRgba, float defaultIntensity)
    {
        var texSlot     = MakeTextureSlot(texKey);
        var colorSwatch = MakeColorSwatch(colorKey, defaultRgba);
        var stepper     = MakeStepper(GetPropFloat(stepKey, defaultIntensity), 0.001);

        _propGetters[stepKey] = () => new EditorMaterialProperty
        {
            Name = stepKey,
            Type = EditorMaterialPropertyType.Float,
            Data = [(float)stepper.Value],
        };

        var panel = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        panel.Children.Add(texSlot);
        panel.Children.Add(colorSwatch);
        panel.Children.Add(stepper);
        return panel;
    }

    private View MakeSliderControls(string propKey, float initialValue, float min, float max)
    {
        var (view, _) = MakeSliderWithLabel(propKey, initialValue, min, max);
        return view;
    }

    private (View panel, Slider slider) MakeSliderWithLabel(
        string propKey, float initialValue, float min, float max)
    {
        float current = GetPropFloat(propKey, initialValue);

        var slider = new Slider
        {
            Minimum         = min,
            Maximum         = max,
            Value           = current,
            HeightRequest   = 20,
            VerticalOptions = LayoutOptions.Center,
            ThumbColor        = (Color)Application.Current!.Resources["AccentBlue"],
            MinimumTrackColor = (Color)Application.Current!.Resources["AccentBlue"],
            MaximumTrackColor = (Color)Application.Current!.Resources["Border"],
        };

        var valueLabel = new Label
        {
            Text                  = current.ToString("F2"),
            TextColor             = (Color)Application.Current.Resources["TextSecondary"],
            FontSize              = 11,
            WidthRequest          = 34,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions       = LayoutOptions.Center,
        };

        slider.ValueChanged += (_, e) => valueLabel.Text = ((float)e.NewValue).ToString("F2");

        _propGetters[propKey] = () => new EditorMaterialProperty
        {
            Name = propKey,
            Type = EditorMaterialPropertyType.Float,
            Data = [(float)slider.Value],
        };

        var panel = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(38)),
            },
            ColumnSpacing   = 4,
            VerticalOptions = LayoutOptions.Center,
        };
        Grid.SetColumn(slider, 0);
        Grid.SetColumn(valueLabel, 1);
        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);
        return (panel, slider);
    }

    private View MakeMultiStepperControls(string propKey, float[]? data, int count)
    {
        var steppers = new AxisStepper[count];
        var panel = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        for (int i = 0; i < count; i++)
        {
            float val = data is { Length: > 0 } && i < data.Length ? data[i] : 0f;
            var s = MakeStepper(val, 0.001);
            steppers[i] = s;
            panel.Children.Add(s);
        }

        _propGetters[propKey] = () =>
        {
            var type = count switch { 2 => EditorMaterialPropertyType.Vector2, 3 => EditorMaterialPropertyType.Vector3, _ => EditorMaterialPropertyType.Vector4 };
            float[] values = new float[count];
            for (int i = 0; i < count; i++) values[i] = (float)steppers[i].Value;
            return new EditorMaterialProperty { Name = propKey, Type = type, Data = values };
        };
        return panel;
    }

    private Border MakeTextureSlot(string propKey)
    {
        string initialPath = _material?.Properties.TryGetValue(propKey, out var p) == true
            ? (p.TexturePath ?? string.Empty)
            : string.Empty;

        string texPath = initialPath;

        var slot = new Border
        {
            WidthRequest    = 32,
            HeightRequest   = 32,
            StrokeThickness = 1,
            Stroke          = new SolidColorBrush((Color)Application.Current!.Resources["Border"]),
            StrokeShape     = new RoundRectangle { CornerRadius = 2 },
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = string.IsNullOrEmpty(texPath)
                ? Color.FromArgb("#111113")
                : (Color)Application.Current.Resources["AccentBlueDim"],
        };

        _propGetters[propKey] = () => new EditorMaterialProperty
        {
            Name        = propKey,
            Type        = EditorMaterialPropertyType.Texture2D,
            TexturePath = texPath,
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select Texture",
                FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.dds"],
                }),
            });
            if (result is null) return;
            texPath             = result.FullPath;
            slot.BackgroundColor = (Color)Application.Current!.Resources["AccentBlueDim"];
        };
        slot.GestureRecognizers.Add(tap);

        return slot;
    }

    private Border MakeColorSwatch(string propKey, float[] defaultRgba)
    {
        float[] rgba = GetPropColor(propKey, defaultRgba);

        var swatch = new Border
        {
            WidthRequest    = 42,
            HeightRequest   = 22,
            StrokeThickness = 1,
            Stroke          = new SolidColorBrush((Color)Application.Current!.Resources["Border"]),
            StrokeShape     = new RoundRectangle { CornerRadius = 2 },
            BackgroundColor = Color.FromRgba(rgba[0], rgba[1], rgba[2], rgba[3]),
            VerticalOptions = LayoutOptions.Center,
        };

        _propGetters[propKey] = () => new EditorMaterialProperty
        {
            Name = propKey,
            Type = EditorMaterialPropertyType.Color,
            Data = [rgba[0], rgba[1], rgba[2], rgba[3]],
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null) return;

            var current = Color.FromRgba(rgba[0], rgba[1], rgba[2], rgba[3]);
            var picked  = await RgbaColorPickerDialog.ShowAsync(page.Navigation, current);
            if (picked is null) return;

            rgba[0] = picked.Red;
            rgba[1] = picked.Green;
            rgba[2] = picked.Blue;
            rgba[3] = picked.Alpha;
            swatch.BackgroundColor = picked;
        };
        swatch.GestureRecognizers.Add(tap);

        return swatch;
    }

    private static AxisStepper MakeStepper(float initialValue, double step)
    {
        return new AxisStepper
        {
            ShowAxisTag     = false,
            Value           = initialValue,
            Step            = step,
            WidthRequest    = 80,
            VerticalOptions = LayoutOptions.Center,
        };
    }

    #endregion

    #region Helpers

    private float GetPropFloat(string key, float fallback)
    {
        if (_material?.Properties.TryGetValue(key, out var prop) == true
            && prop.Data is { Length: > 0 })
            return prop.Data[0];
        return fallback;
    }

    private float GetPropComponent(string key, int index, float fallback)
    {
        if (_material?.Properties.TryGetValue(key, out var prop) == true
            && prop.Data is not null && index < prop.Data.Length)
            return prop.Data[index];
        return fallback;
    }

    private float[] GetPropColor(string key, float[] fallback)
    {
        if (_material?.Properties.TryGetValue(key, out var prop) == true
            && prop.Data?.Length >= 4)
            return [prop.Data[0], prop.Data[1], prop.Data[2], prop.Data[3]];
        return [fallback[0], fallback[1], fallback[2], fallback.Length > 3 ? fallback[3] : 1f];
    }

    #endregion
}
