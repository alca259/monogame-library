using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>
/// ViewModel del diálogo "Project Settings": pestañas General / Content / Localization /
/// CodeGen con sus campos. Carga desde <see cref="ProjectSettings"/> y guarda al confirmar.
/// </summary>
public sealed partial class ProjectSettingsViewModel : DialogViewModel<bool>
{
    private EditorProject? _project;
    private ProjectSettings? _settings;

    public ObservableCollection<string> BuildConfigs { get; } = ["Debug", "Release"];

    [ObservableProperty] private string _activeTab = "General";

    // General
    [ObservableProperty] private string _buildConfiguration = "Debug";
    [ObservableProperty] private string _virtualWidth = "1920";
    [ObservableProperty] private string _virtualHeight = "1080";
    [ObservableProperty] private string _gameAppCsproj = string.Empty;
    [ObservableProperty] private string _gameScriptsCsproj = string.Empty;

    // Content
    [ObservableProperty] private string _contentRelPath = "Content";
    [ObservableProperty] private string _localizationRelPath = "Localization";

    // Localization
    [ObservableProperty] private string _defaultLocale = "en-US";
    [ObservableProperty] private string _supportedLocales = "en-US";

    // CodeGen
    [ObservableProperty] private string _rootNamespace = string.Empty;
    [ObservableProperty] private string _generatedCodeFolder = "Generated";
    [ObservableProperty] private bool _generateOnSave;

    /// <summary>Inicializa el diálogo con el proyecto y la configuración a editar.</summary>
    public void Initialize(EditorProject project, ProjectSettings settings)
    {
        _project = project;
        _settings = settings;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        if (_settings is null || _project is null) return;

        BuildConfiguration = BuildConfigs.Contains(_settings.BuildConfiguration)
            ? _settings.BuildConfiguration : "Debug";
        VirtualWidth = _settings.VirtualWidth.ToString();
        VirtualHeight = _settings.VirtualHeight.ToString();

        GameAppCsproj = !string.IsNullOrEmpty(_settings.GameAppCsprojRelPath)
            ? _settings.GameAppCsprojRelPath
            : (!string.IsNullOrEmpty(_project.GameCsprojPath)
                ? Path.GetRelativePath(_project.RootPath, _project.GameCsprojPath)
                : string.Empty);

        GameScriptsCsproj = _settings.GameScriptsCsprojRelPath;
        ContentRelPath = _settings.ContentRelPath;
        LocalizationRelPath = _settings.LocalizationRelPath;
        DefaultLocale = _settings.DefaultLocale;
        SupportedLocales = string.Join(", ", _settings.SupportedLocales);
        RootNamespace = _settings.RootNamespace;
        GeneratedCodeFolder = _settings.GeneratedCodeFolder;
        GenerateOnSave = _settings.GenerateOnSave;
    }

    private void SaveToSettings()
    {
        if (_settings is null) return;

        _settings.BuildConfiguration = BuildConfiguration;
        _settings.VirtualWidth = ParseInt(VirtualWidth, 1920);
        _settings.VirtualHeight = ParseInt(VirtualHeight, 1080);
        _settings.GameAppCsprojRelPath = GameAppCsproj?.Trim() ?? string.Empty;
        _settings.GameScriptsCsprojRelPath = GameScriptsCsproj?.Trim() ?? string.Empty;
        _settings.ContentRelPath = ContentRelPath?.Trim() ?? "Content";
        _settings.LocalizationRelPath = LocalizationRelPath?.Trim() ?? "Localization";
        _settings.DefaultLocale = DefaultLocale?.Trim() ?? "en-US";
        _settings.SupportedLocales = ParseLocaleList(SupportedLocales);
        _settings.RootNamespace = RootNamespace?.Trim() ?? string.Empty;
        _settings.GeneratedCodeFolder = GeneratedCodeFolder?.Trim() ?? "Generated";
        _settings.GenerateOnSave = GenerateOnSave;
    }

    [RelayCommand]
    private void SelectTab(string? tab) => ActiveTab = tab ?? "General";

    [RelayCommand]
    private async Task BrowseLocalizationFolderAsync()
    {
        if (_project is null) return;
        string? abs = await DialogService.PickFolderAsync();
        if (abs is null) return;
        LocalizationRelPath = Path.GetRelativePath(_project.RootPath, abs);
    }

    [RelayCommand]
    private async Task BrowseCsprojAsync()
    {
        string? rel = await PickCsprojRelativeAsync();
        if (rel is not null) GameAppCsproj = rel;
    }

    [RelayCommand]
    private async Task BrowseScriptsCsprojAsync()
    {
        string? rel = await PickCsprojRelativeAsync();
        if (rel is not null) GameScriptsCsproj = rel;
    }

    private async Task<string?> PickCsprojRelativeAsync()
    {
        if (_project is null) return null;
        string? abs = await DialogService.PickFileAsync(new PickOptions { PickerTitle = "Select .csproj file" });
        if (abs is null) return null;
        if (!abs.StartsWith(_project.RootPath, StringComparison.OrdinalIgnoreCase)) return null;
        return Path.GetRelativePath(_project.RootPath, abs);
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (_settings is null || _project is null) return;
        SaveToSettings();
        await _settings.SaveAsync(_project).ConfigureAwait(true);
        Close(true);
    }

    private static int ParseInt(string? text, int fallback)
        => int.TryParse(text, out int v) && v > 0 ? v : fallback;

    private static List<string> ParseLocaleList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ["en-US"];
        List<string> result = [];
        foreach (string part in text.Split(','))
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
        }
        return result.Count > 0 ? result : ["en-US"];
    }
}
