namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>Par locale → valor editable para la rejilla de traducciones.</summary>
public sealed class LocaleValueItem
{
    public string Locale { get; }
    public string Value  { get; set; } = string.Empty;

    public LocaleValueItem(string locale, string value)
    {
        Locale = locale;
        Value  = value;
    }
}

/// <summary>
/// ViewModel de la pestaña Localization: lista de claves de traducción y, para la
/// clave seleccionada, un campo editable por locale.
/// </summary>
public sealed class LocalizationBrowserViewModel : ViewModelBase
{
    private LocalizationEditorModel? _model;
    private string? _selectedKey;

    public event Action? KeyListChanged;
    public event Action? TranslationGridChanged;

    public IReadOnlyList<string>          KeyItems      { get; private set; } = [];
    public IReadOnlyList<LocaleValueItem> Translations  { get; private set; } = [];
    public string LocaleCountText { get; private set; } = "0 locales";
    public string StatusText      { get; private set; } = string.Empty;
    public bool   CanSave         { get; private set; }

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(e => _ = OnProjectOpenedAsync(e));
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            _ = OnProjectOpenedAsync(new ProjectOpenedEvent(project));
    }

    private async Task OnProjectOpenedAsync(ProjectOpenedEvent e)
    {
        _model       = null;
        _selectedKey = null;
        KeyItems     = [];
        Translations = [];
        CanSave      = false;
        LocaleCountText = "0 locales";
        StatusText   = string.Empty;

        if (e.Project is null)
        {
            KeyListChanged?.Invoke();
            TranslationGridChanged?.Invoke();
            return;
        }

        _model = await LocalizationEditorModel.LoadAsync(e.Project.LocalizationPath).ConfigureAwait(true);
        RebuildKeyList();
    }

    private void RebuildKeyList()
    {
        _selectedKey = null;
        Translations = [];

        if (_model is null)
        {
            KeyItems = [];
            KeyListChanged?.Invoke();
            TranslationGridChanged?.Invoke();
            return;
        }

        KeyItems = [.. _model.Keys];
        int count = _model.Locales.Count;
        LocaleCountText = count == 1 ? "1 locale" : $"{count} locales";
        CanSave = _model.Locales.Count > 0 || _model.Keys.Count > 0;

        KeyListChanged?.Invoke();
        TranslationGridChanged?.Invoke();
    }

    public void SelectKey(string? key)
    {
        FlushEditsToModel(_selectedKey);
        _selectedKey = key;
        BuildTranslationEntries(key);
    }

    private void FlushEditsToModel(string? key)
    {
        if (_model is null || string.IsNullOrEmpty(key)) return;
        foreach (LocaleValueItem item in Translations)
            _model.SetValue(item.Locale, key, item.Value ?? string.Empty);
    }

    private void BuildTranslationEntries(string? key)
    {
        if (_model is null || string.IsNullOrEmpty(key))
        {
            Translations = [];
            TranslationGridChanged?.Invoke();
            return;
        }

        List<LocaleValueItem> rows = [];
        foreach (string locale in _model.Locales)
            rows.Add(new LocaleValueItem(locale, _model.GetValue(locale, key)));

        Translations = rows;
        TranslationGridChanged?.Invoke();
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public void AddKey(string key)
    {
        if (_model is null || string.IsNullOrWhiteSpace(key)) return;
        _model.AddKey(key.Trim());
        RebuildKeyList();
    }

    public void AddLocale(string locale)
    {
        if (_model is null || string.IsNullOrWhiteSpace(locale)) return;
        string? savedKey = _selectedKey;
        _model.AddLocale(locale.Trim());
        RebuildKeyList();
        if (!string.IsNullOrEmpty(savedKey)) SelectKey(savedKey);
    }

    public void RemoveKey()
    {
        if (_model is null || string.IsNullOrEmpty(_selectedKey)) return;
        _model.RemoveKey(_selectedKey);
        RebuildKeyList();
    }

    public void RemoveLocale(string locale)
    {
        if (_model is null) return;
        string? savedKey = _selectedKey;
        _model.RemoveLocale(locale);
        RebuildKeyList();
        if (!string.IsNullOrEmpty(savedKey)) SelectKey(savedKey);
    }

    public async Task SaveAsync()
    {
        if (_model is null) return;
        FlushEditsToModel(_selectedKey);
        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
            StatusText = "Saved";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }

        TranslationGridChanged?.Invoke();
    }
}
