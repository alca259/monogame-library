using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Alca.MonoGame.Kernel.Localization;

/// <summary>Manages game localization by loading and serving strings from JSON culture files.</summary>
/// <remarks>
/// JSON files are expected at Content/Localization/{culture}.json and are NOT processed by the
/// Content Pipeline — they are copied raw to the output directory.
/// </remarks>
public sealed class LocalizationManager : IStringLocalizer
{
    private const int InitialCapacity = 256;

    private readonly Dictionary<string, string> _strings = new(InitialCapacity);

    /// <summary>Gets the currently active culture code (e.g., "es", "en").</summary>
    public string CurrentCulture { get; private set; } = string.Empty;

    /// <summary>Raised after the active culture changes and new strings are loaded.</summary>
    public event Action? CultureChanged;

    /// <inheritdoc/>
    public LocalizedString this[string name]
    {
        get
        {
            bool found = _strings.TryGetValue(name, out string? value);
            return new LocalizedString(name, value ?? name, !found);
        }
    }

    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            bool found = _strings.TryGetValue(name, out string? value);
            string formatted = string.Format(value ?? name, arguments);
            return new LocalizedString(name, formatted, !found);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (KeyValuePair<string, string> pair in _strings)
        {
            yield return new LocalizedString(pair.Key, pair.Value);
        }
    }

    /// <summary>Loads the JSON language file for the given culture and raises CultureChanged.</summary>
    /// <param name="culture">Two-letter ISO culture code, e.g., "es" or "en".</param>
    /// <param name="relativePath">The relative path to the localization files.</param>
    public void LoadLanguage(string culture, string relativePath = "Content/Localization")
    {
        if (culture == CurrentCulture) return;

        CurrentCulture = culture;
        _strings.Clear();

        try
        {
            using Stream stream = TitleContainer.OpenStream($"{relativePath}/{culture}.json");
            Dictionary<string, string>? loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);

            if (loaded is not null)
            {
                foreach (KeyValuePair<string, string> pair in loaded)
                {
                    _strings[pair.Key] = pair.Value;
                }
            }
        }
        catch (FileNotFoundException) { }

        CultureChanged?.Invoke();
    }
}
