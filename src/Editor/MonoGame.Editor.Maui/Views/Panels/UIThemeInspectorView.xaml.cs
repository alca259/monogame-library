using System.Text.Json;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Inspector tab "UI Theme Editor". Builds five NineSlice section cards
/// (Panel / Button / Dropdown / ProgressBar / TextBox) in code-behind,
/// each with a texture-path entry, four border entries, and two tile checkboxes.
/// Loads/saves <see cref="EditorUITheme"/> (.uitheme.json) for the selected asset.
/// </summary>
public sealed partial class UIThemeInspectorView : ContentView
{
    private sealed class SectionControls
    {
        public Entry    TexturePath = null!;
        public Entry    Left = null!, Right = null!, Top = null!, Bottom = null!;
        public CheckBox TileEdges = null!, TileCenter = null!;
    }

    private static readonly string[] SectionNames = ["Panel", "Button", "Dropdown", "ProgressBar", "TextBox"];

    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly Dictionary<string, SectionControls> _sections = new(StringComparer.Ordinal);

    private Action<AssetSelectedEvent>? _onAssetSelected;
    private string _currentFilePath = string.Empty;

    public UIThemeInspectorView()
    {
        InitializeComponent();
        BuildSections();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onAssetSelected = e => MainThread.BeginInvokeOnMainThread(() => OnAssetSelected(e));
        _bus.Subscribe(_onAssetSelected);
    }

    private void Unsubscribe()
    {
        if (_onAssetSelected is not null) _bus.Unsubscribe(_onAssetSelected);
    }

    // ── Section builder ───────────────────────────────────────────────────────

    private void BuildSections()
    {
        foreach (string name in SectionNames)
        {
            SectionControls ctrl = new()
            {
                TexturePath = new Entry { Placeholder = "path/to/texture" },
                Left        = new Entry { Keyboard = Keyboard.Numeric, Text = "8", WidthRequest = 50 },
                Right       = new Entry { Keyboard = Keyboard.Numeric, Text = "8", WidthRequest = 50 },
                Top         = new Entry { Keyboard = Keyboard.Numeric, Text = "8", WidthRequest = 50 },
                Bottom      = new Entry { Keyboard = Keyboard.Numeric, Text = "8", WidthRequest = 50 },
                TileEdges   = new CheckBox(),
                TileCenter  = new CheckBox(),
            };

            _sections[name] = ctrl;

            var bordersRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 6,
                Padding       = new Thickness(10, 0, 10, 4),
            };
            Grid.SetColumn(ctrl.Left,   0);
            Grid.SetColumn(ctrl.Right,  1);
            Grid.SetColumn(ctrl.Top,    2);
            Grid.SetColumn(ctrl.Bottom, 3);
            bordersRow.Children.Add(ctrl.Left);
            bordersRow.Children.Add(ctrl.Right);
            bordersRow.Children.Add(ctrl.Top);
            bordersRow.Children.Add(ctrl.Bottom);

            var tileRow = new HorizontalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(10, 0, 10, 6),
                Children =
                {
                    ctrl.TileEdges,
                    new Label { Text = "Tile Edges",  VerticalOptions = LayoutOptions.Center, FontSize = 11 },
                    ctrl.TileCenter,
                    new Label { Text = "Tile Center", VerticalOptions = LayoutOptions.Center, FontSize = 11 },
                },
            };

            var section = new VerticalStackLayout { Spacing = 0 };
            section.Children.Add(new Label
            {
                Text            = name,
                FontSize        = 11,
                FontAttributes  = FontAttributes.Bold,
                Padding         = new Thickness(10, 6),
                BackgroundColor = Color.FromArgb("#252528"),
            });
            section.Children.Add(new VerticalStackLayout
            {
                Padding  = new Thickness(10, 4, 10, 2),
                Spacing  = 2,
                Children =
                {
                    new Label { Text = "Texture Path", FontSize = 10, TextColor = Color.FromArgb("#9A9AA2") },
                    ctrl.TexturePath,
                    new Label { Text = "L / R / T / B", FontSize = 10, TextColor = Color.FromArgb("#9A9AA2"), Margin = new Thickness(0, 4, 0, 0) },
                },
            });
            section.Children.Add(bordersRow);
            section.Children.Add(tileRow);
            section.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#3A3A3E") });

            ThemeStack.Children.Add(section);
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.UITheme)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        EditorUITheme theme = LoadOrCreate(_currentFilePath);
        PopulateForm(theme);
        ThemeSaveButton.IsEnabled = true;
    }

    private static EditorUITheme LoadOrCreate(string path)
    {
        if (!File.Exists(path)) return EditorUITheme.CreateEmpty();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorUITheme>(json) ?? EditorUITheme.CreateEmpty();
        }
        catch
        {
            return EditorUITheme.CreateEmpty();
        }
    }

    private void PopulateForm(EditorUITheme theme)
    {
        ThemeNameEntry.Text = theme.Name;
        PopulateSection("Panel",       theme.Panel);
        PopulateSection("Button",      theme.Button);
        PopulateSection("Dropdown",    theme.Dropdown);
        PopulateSection("ProgressBar", theme.ProgressBar);
        PopulateSection("TextBox",     theme.TextBox);
        ThemeStatusLabel.Text = string.Empty;
    }

    private void PopulateSection(string name, EditorUIThemeEntry entry)
    {
        if (!_sections.TryGetValue(name, out SectionControls? ctrl)) return;
        ctrl.TexturePath.Text     = entry.TexturePath;
        ctrl.Left.Text            = entry.BorderLeft.ToString();
        ctrl.Right.Text           = entry.BorderRight.ToString();
        ctrl.Top.Text             = entry.BorderTop.ToString();
        ctrl.Bottom.Text          = entry.BorderBottom.ToString();
        ctrl.TileEdges.IsChecked  = entry.TileEdges;
        ctrl.TileCenter.IsChecked = entry.TileCenter;
    }

    private EditorUIThemeEntry ReadSection(string name)
    {
        if (!_sections.TryGetValue(name, out SectionControls? ctrl)) return new EditorUIThemeEntry();
        return new EditorUIThemeEntry
        {
            TexturePath  = ctrl.TexturePath.Text ?? string.Empty,
            BorderLeft   = ParseInt(ctrl.Left.Text),
            BorderRight  = ParseInt(ctrl.Right.Text),
            BorderTop    = ParseInt(ctrl.Top.Text),
            BorderBottom = ParseInt(ctrl.Bottom.Text),
            TileEdges    = ctrl.TileEdges.IsChecked,
            TileCenter   = ctrl.TileCenter.IsChecked,
        };
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        ThemeNameEntry.Text = string.Empty;
        foreach (string name in SectionNames)
            PopulateSection(name, new EditorUIThemeEntry());
        ThemeSaveButton.IsEnabled = false;
        ThemeStatusLabel.Text = string.Empty;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath)) return;

        var theme = new EditorUITheme
        {
            Name        = ThemeNameEntry.Text ?? string.Empty,
            Panel       = ReadSection("Panel"),
            Button      = ReadSection("Button"),
            Dropdown    = ReadSection("Dropdown"),
            ProgressBar = ReadSection("ProgressBar"),
            TextBox     = ReadSection("TextBox"),
        };

        try
        {
            string json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            ThemeStatusLabel.Text = "Saved";
        }
        catch (Exception ex)
        {
            ThemeStatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private static int ParseInt(string? text) =>
        int.TryParse(text, out int v) ? v : 0;
}
