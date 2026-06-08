using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>Par locale → valor editable para la rejilla de traducciones.</summary>
public sealed partial class LocaleValueItem(string locale, string value) : ObservableObject
{
    public string Locale { get; } = locale;

    [ObservableProperty]
    private string _value = value;
}

/// <summary>
/// ViewModel de la pestaña Localization: lista de claves de traducción y, para la clave
/// seleccionada, un campo editable por locale. Permite añadir/eliminar claves y locales y
/// guardar. Carga los <c>*.json</c> de <see cref="EditorProject.LocalizationPath"/>.
/// </summary>
public sealed partial class LocalizationBrowserViewModel : ViewModelBase
{
    private LocalizationEditorModel? _model;

    public ObservableCollection<string> KeyItems { get; } = [];
    public ObservableCollection<LocaleValueItem> Translations { get; } = [];

    [ObservableProperty]
    private string? _selectedKey;

    [ObservableProperty]
    private string _localeCountText = "0 locales";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(e => _ = OnProjectOpenedAsync(e));
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            _ = OnProjectOpenedAsync(new ProjectOpenedEvent(project));
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private async Task OnProjectOpenedAsync(ProjectOpenedEvent e)
    {
        _model = null;
        KeyItems.Clear();
        Translations.Clear();
        SelectedKey      = null;
        CanSave          = false;
        LocaleCountText  = "0 locales";

        if (e.Project is null) return;

        _model = await LocalizationEditorModel.LoadAsync(e.Project.LocalizationPath).ConfigureAwait(true);
        RebuildKeyList();
    }

    private void RebuildKeyList()
    {
        SelectedKey = null;
        Translations.Clear();
        KeyItems.Clear();

        if (_model is null) return;

        foreach (string key in _model.Keys)
            KeyItems.Add(key);

        int localeCount = _model.Locales.Count;
        LocaleCountText = localeCount == 1 ? "1 locale" : $"{localeCount} locales";
        CanSave = _model.Locales.Count > 0 || _model.Keys.Count > 0;
    }

    // ── Key selection ─────────────────────────────────────────────────────────

    partial void OnSelectedKeyChanged(string? oldValue, string? newValue)
    {
        FlushEditsToModel(oldValue);
        BuildTranslationEntries(newValue);
    }

    private void FlushEditsToModel(string? key)
    {
        if (_model is null || string.IsNullOrEmpty(key)) return;
        foreach (LocaleValueItem item in Translations)
            _model.SetValue(item.Locale, key, item.Value ?? string.Empty);
    }

    private void BuildTranslationEntries(string? key)
    {
        Translations.Clear();
        if (_model is null || string.IsNullOrEmpty(key)) return;

        foreach (string locale in _model.Locales)
            Translations.Add(new LocaleValueItem(locale, _model.GetValue(locale, key)));
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddKeyAsync()
    {
        if (_model is null) return;

        string? key = await DialogService.PromptAsync("New Key", "Translation key:");
        if (string.IsNullOrWhiteSpace(key)) return;

        _model.AddKey(key);
        RebuildKeyList();
    }

    [RelayCommand]
    private async Task AddLocaleAsync()
    {
        if (_model is null) return;

        string? locale = await DialogService.PromptAsync("New Locale", "Locale code (e.g. \"en\", \"es\"):");
        if (string.IsNullOrWhiteSpace(locale)) return;

        _model.AddLocale(locale);
        string? key = SelectedKey;
        RebuildKeyList();
        if (!string.IsNullOrEmpty(key)) SelectedKey = key;
    }

    [RelayCommand]
    private async Task RemoveKeyAsync()
    {
        if (_model is null || string.IsNullOrEmpty(SelectedKey)) return;

        bool confirmed = await DialogService.ConfirmAsync(
            "Remove Key", $"Remove key '{SelectedKey}' from all locales?", "Remove", "Cancel");
        if (!confirmed) return;

        _model.RemoveKey(SelectedKey);
        RebuildKeyList();
    }

    [RelayCommand]
    private async Task RemoveLocaleAsync()
    {
        if (_model is null || _model.Locales.Count == 0) return;

        string[] localeOptions = [.. _model.Locales];
        string? choice = await DialogService.ActionSheetAsync("Remove locale:", "Cancel", null, localeOptions);
        if (choice is null or "Cancel") return;

        bool confirmed = await DialogService.ConfirmAsync(
            "Remove Locale",
            $"Remove locale '{choice}' and delete its file? This cannot be undone.",
            "Remove", "Cancel");
        if (!confirmed) return;

        _model.RemoveLocale(choice);
        string? key = SelectedKey;
        RebuildKeyList();
        if (!string.IsNullOrEmpty(key)) SelectedKey = key;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_model is null) return;
        FlushEditsToModel(SelectedKey);
        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
            StatusText = "Saved";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }
}
