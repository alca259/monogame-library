using System.Text.Json;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>Modelo editable para una sección NineSlice del tema de UI.</summary>
public sealed class UIThemeSectionModel
{
    public string SectionName  { get; init; } = string.Empty;
    public string TexturePath  { get; set; } = string.Empty;
    public int    BorderLeft   { get; set; }
    public int    BorderRight  { get; set; }
    public int    BorderTop    { get; set; }
    public int    BorderBottom { get; set; }
    public bool   TileEdges    { get; set; }
    public bool   TileCenter   { get; set; }
}

/// <summary>
/// ViewModel del inspector de temas de UI: carga y guarda <see cref="EditorUITheme"/>
/// (.uitheme.json) para el asset de tipo UITheme seleccionado en el browser.
/// </summary>
public sealed class UIThemeInspectorViewModel : ViewModelBase
{
    private string _currentFilePath = string.Empty;

    public event Action? FormUpdated;

    public string ThemeName  { get; private set; } = "No theme selected";
    public bool   CanSave    { get; private set; }
    public string StatusText { get; private set; } = string.Empty;

    public UIThemeSectionModel PanelSection       { get; } = new() { SectionName = "Panel" };
    public UIThemeSectionModel ButtonSection      { get; } = new() { SectionName = "Button" };
    public UIThemeSectionModel DropdownSection    { get; } = new() { SectionName = "Dropdown" };
    public UIThemeSectionModel ProgressBarSection { get; } = new() { SectionName = "ProgressBar" };
    public UIThemeSectionModel TextBoxSection     { get; } = new() { SectionName = "TextBox" };

    public IReadOnlyList<UIThemeSectionModel> Sections { get; }

    public UIThemeInspectorViewModel()
    {
        Sections = [PanelSection, ButtonSection, DropdownSection, ProgressBarSection, TextBoxSection];
    }

    protected override void RegisterEvents()
    {
        On<AssetSelectedEvent>(OnAssetSelected);
    }

    private void OnAssetSelected(AssetSelectedEvent e)
    {
        if (e.Asset is null || e.Asset.Type != AssetType.UITheme)
        {
            ClearForm();
            return;
        }

        _currentFilePath = e.Asset.AbsolutePath;
        ThemeName = e.Asset.Name;
        CanSave   = true;

        EditorUITheme theme = LoadOrCreate(_currentFilePath, e.Asset.Name);
        PopulateForm(theme);
        FormUpdated?.Invoke();
    }

    private static EditorUITheme LoadOrCreate(string path, string name)
    {
        if (!File.Exists(path)) return EditorUITheme.CreateEmpty(name);
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EditorUITheme>(json) ?? EditorUITheme.CreateEmpty(name);
        }
        catch { return EditorUITheme.CreateEmpty(name); }
    }

    private void PopulateForm(EditorUITheme theme)
    {
        CopyEntry(PanelSection,       theme.Panel);
        CopyEntry(ButtonSection,      theme.Button);
        CopyEntry(DropdownSection,    theme.Dropdown);
        CopyEntry(ProgressBarSection, theme.ProgressBar);
        CopyEntry(TextBoxSection,     theme.TextBox);
        StatusText = string.Empty;
    }

    private static void CopyEntry(UIThemeSectionModel s, EditorUIThemeEntry e)
    {
        s.TexturePath  = e.TexturePath;
        s.BorderLeft   = e.BorderLeft;
        s.BorderRight  = e.BorderRight;
        s.BorderTop    = e.BorderTop;
        s.BorderBottom = e.BorderBottom;
        s.TileEdges    = e.TileEdges;
        s.TileCenter   = e.TileCenter;
    }

    private void ClearForm()
    {
        _currentFilePath = string.Empty;
        ThemeName = "No theme selected";
        CanSave   = false;
        foreach (UIThemeSectionModel s in Sections)
        {
            s.TexturePath = string.Empty;
            s.BorderLeft = s.BorderRight = s.BorderTop = s.BorderBottom = 0;
            s.TileEdges  = s.TileCenter = false;
        }
        StatusText = string.Empty;
        FormUpdated?.Invoke();
    }

    public void Save()
    {
        if (!CanSave || string.IsNullOrEmpty(_currentFilePath)) return;

        EditorUITheme theme = EditorUITheme.CreateEmpty(ThemeName);
        theme.Panel       = BuildEntry(PanelSection);
        theme.Button      = BuildEntry(ButtonSection);
        theme.Dropdown    = BuildEntry(DropdownSection);
        theme.ProgressBar = BuildEntry(ProgressBarSection);
        theme.TextBox     = BuildEntry(TextBoxSection);

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

        FormUpdated?.Invoke();
    }

    private static EditorUIThemeEntry BuildEntry(UIThemeSectionModel s) => new()
    {
        TexturePath  = s.TexturePath,
        BorderLeft   = s.BorderLeft,
        BorderRight  = s.BorderRight,
        BorderTop    = s.BorderTop,
        BorderBottom = s.BorderBottom,
        TileEdges    = s.TileEdges,
        TileCenter   = s.TileCenter,
    };
}
