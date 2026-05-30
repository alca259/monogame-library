using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Localization". Left panel lists all translation keys; selecting a key shows
/// per-locale Entry fields on the right. Supports adding/removing keys and locales.
/// Loads all *.json files from <see cref="EditorProject.LocalizationPath"/>.
/// </summary>
public sealed partial class LocalizationBrowserView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private readonly ObservableCollection<string> _keyItems = [];
    private readonly Dictionary<string, Entry> _localeEntries = new(StringComparer.OrdinalIgnoreCase);

    private LocalizationEditorModel? _model;
    private string _selectedKey = string.Empty;

    private Action<ProjectOpenedEvent>? _onProjectOpened;

    public LocalizationBrowserView()
    {
        InitializeComponent();
        KeysList.ItemsSource = _keyItems;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpenedSync(e));
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private async void OnProjectOpenedSync(ProjectOpenedEvent e)
    {
        _model = null;
        _keyItems.Clear();
        _localeEntries.Clear();
        TranslationStack.Children.Clear();
        LocaleSaveButton.IsEnabled = false;
        LocaleCountLabel.Text = "0 locales";
        _selectedKey = string.Empty;

        if (e.Project is null) return;

        _model = await LocalizationEditorModel.LoadAsync(e.Project.LocalizationPath)
                                              .ConfigureAwait(true);
        RebuildKeyList();
    }

    private void RebuildKeyList()
    {
        _keyItems.Clear();
        _selectedKey = string.Empty;
        TranslationStack.Children.Clear();
        _localeEntries.Clear();

        if (_model is null) return;

        foreach (string key in _model.Keys)
            _keyItems.Add(key);

        int localeCount = _model.Locales.Count;
        LocaleCountLabel.Text = localeCount == 1 ? "1 locale" : $"{localeCount} locales";
        LocaleSaveButton.IsEnabled = _model.Locales.Count > 0 || _model.Keys.Count > 0;
    }

    // ── Key selection ─────────────────────────────────────────────────────────

    private void OnKeySelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedKey = e.CurrentSelection.FirstOrDefault() as string ?? string.Empty;

        // Flush pending edits for previous key before switching
        FlushEditsToModel();

        BuildTranslationEntries(_selectedKey);
    }

    private void FlushEditsToModel()
    {
        if (_model is null || string.IsNullOrEmpty(_selectedKey)) return;
        foreach (KeyValuePair<string, Entry> kv in _localeEntries)
            _model.SetValue(kv.Key, _selectedKey, kv.Value.Text ?? string.Empty);
    }

    private void BuildTranslationEntries(string key)
    {
        TranslationStack.Children.Clear();
        _localeEntries.Clear();

        if (_model is null || string.IsNullOrEmpty(key)) return;

        foreach (string locale in _model.Locales)
        {
            Entry entry = new() { Text = _model.GetValue(locale, key) };
            _localeEntries[locale] = entry;

            TranslationStack.Children.Add(new Label
            {
                Text      = locale,
                FontSize  = 11,
                TextColor = Color.FromArgb("#9A9AA2"),
            });
            TranslationStack.Children.Add(entry);
        }
    }

    // ── Toolbar actions ───────────────────────────────────────────────────────

    private async void OnAddKeyClicked(object sender, EventArgs e)
    {
        if (_model is null) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? key = await page.DisplayPromptAsync("New Key", "Translation key:");
        if (string.IsNullOrWhiteSpace(key)) return;

        _model.AddKey(key);
        RebuildKeyList();
    }

    private async void OnAddLocaleClicked(object sender, EventArgs e)
    {
        if (_model is null) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? locale = await page.DisplayPromptAsync("New Locale", "Locale code (e.g. \"en\", \"es\"):");
        if (string.IsNullOrWhiteSpace(locale)) return;

        _model.AddLocale(locale);
        RebuildKeyList();
        if (!string.IsNullOrEmpty(_selectedKey))
            BuildTranslationEntries(_selectedKey);
    }

    private async void OnRemoveKeyClicked(object sender, EventArgs e)
    {
        if (_model is null || string.IsNullOrEmpty(_selectedKey)) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        bool confirmed = await page.DisplayAlertAsync(
            "Remove Key",
            $"Remove key '{_selectedKey}' from all locales?",
            "Remove", "Cancel");

        if (!confirmed) return;

        _model.RemoveKey(_selectedKey);
        RebuildKeyList();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_model is null) return;
        FlushEditsToModel();
        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
            LocalizationStatusLabel.Text = "Saved";
        }
        catch (Exception ex)
        {
            LocalizationStatusLabel.Text = $"Error: {ex.Message}";
        }
    }
}
