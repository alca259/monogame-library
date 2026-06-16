using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>Sección NineSlice editable (Panel / Button / Dropdown / ProgressBar / TextBox).</summary>
public sealed partial class UIThemeSectionViewModel(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty] private string _texturePath = string.Empty;
    [ObservableProperty] private string _left = "8";
    [ObservableProperty] private string _right = "8";
    [ObservableProperty] private string _top = "8";
    [ObservableProperty] private string _bottom = "8";
    [ObservableProperty] private bool _tileEdges;
    [ObservableProperty] private bool _tileCenter;

    public void Populate(EditorUIThemeEntry entry)
    {
        TexturePath = entry.TexturePath;
        Left = entry.BorderLeft.ToString();
        Right = entry.BorderRight.ToString();
        Top = entry.BorderTop.ToString();
        Bottom = entry.BorderBottom.ToString();
        TileEdges = entry.TileEdges;
        TileCenter = entry.TileCenter;
    }

    public EditorUIThemeEntry Read() => new()
    {
        TexturePath = TexturePath,
        BorderLeft = ParseInt(Left),
        BorderRight = ParseInt(Right),
        BorderTop = ParseInt(Top),
        BorderBottom = ParseInt(Bottom),
        TileEdges = TileEdges,
        TileCenter = TileCenter,
    };

    private static int ParseInt(string? text) => int.TryParse(text, out int v) ? v : 0;
}

/// <summary>
/// ViewModel del "UI Theme Editor": cinco secciones NineSlice (Panel / Button / Dropdown /
/// ProgressBar / TextBox), cada una con ruta de textura, bordes y opciones de tiling. Carga
/// y guarda <see cref="EditorUITheme"/> (.uitheme.json) del asset seleccionado.
/// </summary>
public sealed partial class UIThemeInspectorViewModel : ViewModelBase
{
    private static readonly string[] SectionNames = ["Panel", "Button", "Dropdown", "ProgressBar", "TextBox"];

    private string _currentFilePath = string.Empty;

    public ObservableCollection<UIThemeSectionViewModel> Sections { get; } = [];

    [ObservableProperty]
    private string _themeName = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

    public UIThemeInspectorViewModel()
    {
        foreach (string name in SectionNames)
            Sections.Add(new UIThemeSectionViewModel(name));
    }

    protected override void RegisterEvents()
    {
        On<AssetSelectedEvent>(OnAssetSelected);
    }

    private UIThemeSectionViewModel? Section(string name)
        => Sections.FirstOrDefault(s => s.Name == name);

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.UITheme)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        PopulateForm(LoadOrCreate(_currentFilePath));
        CanSave = true;
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
        ThemeName = theme.Name;
        Section("Panel")?.Populate(theme.Panel);
        Section("Button")?.Populate(theme.Button);
        Section("Dropdown")?.Populate(theme.Dropdown);
        Section("ProgressBar")?.Populate(theme.ProgressBar);
        Section("TextBox")?.Populate(theme.TextBox);
        StatusText = string.Empty;
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        ThemeName = string.Empty;
        foreach (UIThemeSectionViewModel section in Sections)
            section.Populate(new EditorUIThemeEntry());
        CanSave = false;
        StatusText = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (string.IsNullOrEmpty(_currentFilePath)) return;

        var theme = new EditorUITheme
        {
            Name = ThemeName,
            Panel = Section("Panel")?.Read() ?? new EditorUIThemeEntry(),
            Button = Section("Button")?.Read() ?? new EditorUIThemeEntry(),
            Dropdown = Section("Dropdown")?.Read() ?? new EditorUIThemeEntry(),
            ProgressBar = Section("ProgressBar")?.Read() ?? new EditorUIThemeEntry(),
            TextBox = Section("TextBox")?.Read() ?? new EditorUIThemeEntry(),
        };

        try
        {
            string json = JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_currentFilePath, json);
            StatusText = "Saved";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }
}
