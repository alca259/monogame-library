namespace MonoGame.Editor.Core.Localization;

/// <summary>
/// In-memory model of the localization files under a project's <c>LocalizationPath</c>.
/// Each locale maps to a <c>{locale}.json</c> file containing a flat key→value dictionary.
/// </summary>
public sealed class LocalizationEditorModel
{
    private readonly string _localizationPath;
    private readonly List<string> _locales = [];
    private readonly List<string> _keys = [];
    private readonly Dictionary<string, Dictionary<string, string>> _data = [];

    private LocalizationEditorModel(string localizationPath) => _localizationPath = localizationPath;

    /// <summary>Ordered list of locale identifiers detected (e.g. "en", "es").</summary>
    public IReadOnlyList<string> Locales => _locales;

    /// <summary>Ordered list of translation keys present across all locales.</summary>
    public IReadOnlyList<string> Keys => _keys;

    /// <summary>
    /// Loads all <c>*.json</c> files from <paramref name="localizationPath"/> into an in-memory model.
    /// Missing keys in a locale are represented as empty strings.
    /// Returns an empty model if the directory does not exist.
    /// </summary>
    public static async Task<LocalizationEditorModel> LoadAsync(string localizationPath)
    {
        LocalizationEditorModel model = new(localizationPath);

        if (!Directory.Exists(localizationPath))
            return model;

        string[] files = Directory.GetFiles(localizationPath, "*.json");
        HashSet<string> allKeys = [];

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string locale = Path.GetFileNameWithoutExtension(file);

            string json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
            Dictionary<string, string>? entries =
                JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            Dictionary<string, string> dict = entries ?? [];
            model._data[locale] = dict;
            model._locales.Add(locale);

            foreach (string key in dict.Keys)
                allKeys.Add(key);
        }

        model._locales.Sort(StringComparer.OrdinalIgnoreCase);
        model._keys.AddRange(allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));

        return model;
    }

    /// <summary>Returns the translated value for the given <paramref name="locale"/> and <paramref name="key"/>, or empty string if missing.</summary>
    public string GetValue(string locale, string key)
    {
        if (_data.TryGetValue(locale, out Dictionary<string, string>? dict) &&
            dict.TryGetValue(key, out string? value))
            return value;

        return string.Empty;
    }

    /// <summary>Sets the translated value for the given <paramref name="locale"/> and <paramref name="key"/>. Creates the locale dictionary if needed.</summary>
    public void SetValue(string locale, string key, string value)
    {
        if (!_data.TryGetValue(locale, out Dictionary<string, string>? dict))
        {
            dict = [];
            _data[locale] = dict;
        }

        dict[key] = value;
    }

    /// <summary>Writes all locale dictionaries back to their respective <c>{locale}.json</c> files.</summary>
    public async Task SaveAsync()
    {
        if (!Directory.Exists(_localizationPath))
            Directory.CreateDirectory(_localizationPath);

        JsonSerializerOptions options = new() { WriteIndented = true };

        for (int i = 0; i < _locales.Count; i++)
        {
            string locale = _locales[i];
            if (!_data.TryGetValue(locale, out Dictionary<string, string>? dict))
                dict = [];

            string path = Path.Combine(_localizationPath, $"{locale}.json");
            string json = JsonSerializer.Serialize(dict, options);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }
    }

    /// <summary>Adds a new translation key to all locales (value defaults to empty string).</summary>
    public void AddKey(string key)
    {
        if (_keys.Contains(key)) return;

        _keys.Add(key);

        for (int i = 0; i < _locales.Count; i++)
        {
            string locale = _locales[i];
            if (!_data.ContainsKey(locale))
                _data[locale] = [];

            _data[locale].TryAdd(key, string.Empty);
        }
    }

    /// <summary>Removes a translation key from all locales.</summary>
    public void RemoveKey(string key)
    {
        _keys.Remove(key);

        for (int i = 0; i < _locales.Count; i++)
            _data.GetValueOrDefault(_locales[i])?.Remove(key);
    }

    /// <summary>Adds a new locale column with empty values for all existing keys.</summary>
    public void AddLocale(string locale)
    {
        if (_locales.Contains(locale)) return;

        _locales.Add(locale);
        _locales.Sort(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> dict = [];
        for (int i = 0; i < _keys.Count; i++)
            dict[_keys[i]] = string.Empty;

        _data[locale] = dict;
    }
}
