namespace MonoGame.Editor.Core.Localization;

/// <summary>
/// Modelo en memoria de los archivos de localización bajo el <c>LocalizationPath</c> de un proyecto.
/// Cada locale se corresponde con un archivo <c>{locale}.json</c> que contiene un diccionario plano clave→valor.
/// </summary>
public sealed class LocalizationEditorModel
{
    private readonly string _localizationPath;
    private readonly List<string> _locales = [];
    private readonly List<string> _keys = [];
    private readonly Dictionary<string, Dictionary<string, string>> _data = [];

    private LocalizationEditorModel(string localizationPath) => _localizationPath = localizationPath;

    /// <summary>Lista ordenada de identificadores de locale detectados (p. ej. "en", "es").</summary>
    public IReadOnlyList<string> Locales => _locales;

    /// <summary>Lista ordenada de claves de traducción presentes en todos los locales.</summary>
    public IReadOnlyList<string> Keys => _keys;

    /// <summary>
    /// Carga todos los archivos <c>*.json</c> de <paramref name="localizationPath"/> en un modelo en memoria.
    /// Las claves ausentes en un locale se representan como cadenas vacías.
    /// Devuelve un modelo vacío si el directorio no existe.
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

    /// <summary>Devuelve el valor traducido para el <paramref name="locale"/> y la <paramref name="key"/> indicados, o cadena vacía si no existe.</summary>
    public string GetValue(string locale, string key)
    {
        if (_data.TryGetValue(locale, out Dictionary<string, string>? dict) &&
            dict.TryGetValue(key, out string? value))
            return value;

        return string.Empty;
    }

    /// <summary>Establece el valor traducido para el <paramref name="locale"/> y la <paramref name="key"/> indicados. Crea el diccionario del locale si es necesario.</summary>
    public void SetValue(string locale, string key, string value)
    {
        if (!_data.TryGetValue(locale, out Dictionary<string, string>? dict))
        {
            dict = [];
            _data[locale] = dict;
        }

        dict[key] = value;
    }

    /// <summary>Escribe todos los diccionarios de locale de vuelta a sus respectivos archivos <c>{locale}.json</c>.</summary>
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

    /// <summary>Agrega una nueva clave de traducción a todos los locales (el valor por defecto es cadena vacía).</summary>
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

    /// <summary>Elimina una clave de traducción de todos los locales.</summary>
    public void RemoveKey(string key)
    {
        _keys.Remove(key);

        for (int i = 0; i < _locales.Count; i++)
            _data.GetValueOrDefault(_locales[i])?.Remove(key);
    }

    /// <summary>Agrega una nueva columna de locale con valores vacíos para todas las claves existentes.</summary>
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

    /// <summary>Elimina un locale del modelo en memoria y borra su archivo <c>{locale}.json</c> del disco.</summary>
    public void RemoveLocale(string locale)
    {
        if (!_locales.Remove(locale)) return;

        _data.Remove(locale);

        string filePath = Path.Combine(_localizationPath, $"{locale}.json");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
