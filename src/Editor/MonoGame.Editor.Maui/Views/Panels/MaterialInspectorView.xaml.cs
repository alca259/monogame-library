using System.Text.Json;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Inspector tab "Material Editor". Shows fixed fields (Name, ShaderPath, RenderingMode, UVSet)
/// plus a dynamic property list built from <see cref="EditorMaterial.Properties"/>.
/// Loads/saves <see cref="EditorMaterial"/> (.mat.json) for the selected asset.
/// </summary>
public sealed partial class MaterialInspectorView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    // property name → Entry list (Float=1, Vec2=2, Vec3=3, Vec4/Color=4, Texture2D=1 path entry)
    private readonly Dictionary<string, List<Entry>> _propEntries = new(StringComparer.Ordinal);

    private EditorMaterial? _material;
    private Action<AssetSelectedEvent>?   _onAssetSelected;
    private Action<ProjectOpenedEvent>?   _onProjectOpened;
    private string _currentFilePath  = string.Empty;
    private string _projectContentRoot = string.Empty;

    private static readonly string[] BuiltInShaders = ["(Default)", "SpriteEffect", "AlphaTest"];

    public MaterialInspectorView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onAssetSelected  = e => MainThread.BeginInvokeOnMainThread(() => OnAssetSelected(e));
        _onProjectOpened  = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _bus.Subscribe(_onAssetSelected);
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onAssetSelected is not null) _bus.Unsubscribe(_onAssetSelected);
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _projectContentRoot = e.Project?.ContentPath ?? string.Empty;
        PopulateShaderPicker();
    }

    private void PopulateShaderPicker()
    {
        string? current = ShaderPicker.SelectedItem as string;
        ShaderPicker.Items.Clear();

        foreach (string s in BuiltInShaders)
            ShaderPicker.Items.Add(s);

        if (!string.IsNullOrEmpty(_projectContentRoot) && Directory.Exists(_projectContentRoot))
        {
            foreach (string fx in Directory.GetFiles(_projectContentRoot, "*.fx", SearchOption.AllDirectories)
                                           .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string rel = Path.GetRelativePath(_projectContentRoot, fx);
                ShaderPicker.Items.Add(rel);
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            int idx = ShaderPicker.Items.IndexOf(current);
            ShaderPicker.SelectedIndex = idx >= 0 ? idx : -1;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.Material)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        _material = LoadOrCreate(_currentFilePath, e.Asset.Name);
        PopulateForm(_material);
        MaterialSaveButton.IsEnabled = true;
    }

    private static EditorMaterial LoadOrCreate(string path, string fallbackName)
    {
        if (!File.Exists(path)) return EditorMaterial.CreateEmpty(fallbackName);
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorMaterial>(json) ?? EditorMaterial.CreateEmpty(fallbackName);
        }
        catch
        {
            return EditorMaterial.CreateEmpty(fallbackName);
        }
    }

    private void PopulateForm(EditorMaterial mat)
    {
        MaterialNameEntry.Text = mat.Name;

        string shader = mat.ShaderPath ?? string.Empty;
        int shaderIdx = ShaderPicker.Items.IndexOf(shader);
        if (shaderIdx < 0 && !string.IsNullOrEmpty(shader))
        {
            ShaderPicker.Items.Add(shader);
            shaderIdx = ShaderPicker.Items.Count - 1;
        }
        ShaderPicker.SelectedIndex = shaderIdx;

        RenderingModePicker.SelectedIndex = mat.RenderingMode switch
        {
            "Cutout"      => 1,
            "Fade"        => 2,
            "Transparent" => 3,
            _             => 0,
        };
        UVSetEntry.Text = mat.UVSet.ToString();
        BuildPropertyRows(mat);
        MaterialStatusLabel.Text = string.Empty;
    }

    private void BuildPropertyRows(EditorMaterial mat)
    {
        PropsStack.Children.Clear();
        _propEntries.Clear();

        foreach (KeyValuePair<string, EditorMaterialProperty> kv in mat.Properties)
        {
            string name = kv.Key;
            EditorMaterialProperty prop = kv.Value;

            List<Entry> entries = [];

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(70)),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 8,
            };

            var nameLabel = new Label { Text = name, FontSize = 11, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(nameLabel, 0);
            row.Children.Add(nameLabel);

            if (prop.Type == EditorMaterialPropertyType.Texture2D)
            {
                Entry e = new() { Text = prop.TexturePath ?? string.Empty, Placeholder = "path/to/texture" };
                entries.Add(e);
                Grid.SetColumn(e, 1);
                row.Children.Add(e);
            }
            else
            {
                int components = prop.Type switch
                {
                    EditorMaterialPropertyType.Float   => 1,
                    EditorMaterialPropertyType.Vector2 => 2,
                    EditorMaterialPropertyType.Vector3 => 3,
                    EditorMaterialPropertyType.Vector4 => 4,
                    EditorMaterialPropertyType.Color   => 4,
                    _                                  => 1,
                };

                var fieldRow = new HorizontalStackLayout { Spacing = 4 };
                for (int i = 0; i < components; i++)
                {
                    float val = (prop.Data != null && i < prop.Data.Length) ? prop.Data[i] : 0f;
                    Entry e = new()
                    {
                        Text         = val.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                        Keyboard     = Keyboard.Numeric,
                        WidthRequest = 56,
                    };
                    entries.Add(e);
                    fieldRow.Children.Add(e);
                }

                Grid.SetColumn(fieldRow, 1);
                row.Children.Add(fieldRow);
            }

            _propEntries[name] = entries;
            PropsStack.Children.Add(row);
        }
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        _material        = null;
        MaterialNameEntry.Text    = string.Empty;
        ShaderPicker.SelectedIndex = -1;
        RenderingModePicker.SelectedIndex = 0;
        UVSetEntry.Text = "0";
        PropsStack.Children.Clear();
        _propEntries.Clear();
        MaterialSaveButton.IsEnabled = false;
        MaterialStatusLabel.Text = string.Empty;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || _material is null) return;

        _material.Name          = MaterialNameEntry.Text ?? string.Empty;
        _material.ShaderPath    = ShaderPicker.SelectedItem as string ?? string.Empty;
        _material.RenderingMode = RenderingModePicker.SelectedIndex switch
        {
            1 => "Cutout",
            2 => "Fade",
            3 => "Transparent",
            _ => "Opaque",
        };
        _material.UVSet = ParseInt(UVSetEntry.Text);

        foreach (KeyValuePair<string, List<Entry>> kv in _propEntries)
        {
            if (!_material.Properties.TryGetValue(kv.Key, out EditorMaterialProperty? prop)) continue;

            if (prop.Type == EditorMaterialPropertyType.Texture2D)
            {
                prop.TexturePath = kv.Value.Count > 0 ? (kv.Value[0].Text ?? string.Empty) : string.Empty;
            }
            else
            {
                prop.Data = new float[kv.Value.Count];
                for (int i = 0; i < kv.Value.Count; i++)
                    prop.Data[i] = ParseFloat(kv.Value[i].Text);
            }
        }

        try
        {
            string json = JsonSerializer.Serialize(_material, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            MaterialStatusLabel.Text = "Saved";
        }
        catch (Exception ex)
        {
            MaterialStatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int v) ? v : 0;

    private static float ParseFloat(string? text) =>
        float.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;

    private void OnShaderPickerChanged(object sender, EventArgs e)
    {
        if (_material is null) return;
        _material.ShaderPath = ShaderPicker.SelectedItem as string ?? string.Empty;
    }
}
