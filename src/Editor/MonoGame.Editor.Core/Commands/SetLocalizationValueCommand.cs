using MonoGame.Editor.Core.Localization;

namespace MonoGame.Editor.Core.Commands;

/// <summary>Sets a single localization value; supports undo.</summary>
public sealed class SetLocalizationValueCommand : IEditorCommand
{
    private readonly LocalizationEditorModel _model;
    private readonly string _locale;
    private readonly string _key;
    private readonly string _oldValue;
    private readonly string _newValue;

    /// <summary>Creates a command to set <paramref name="newValue"/> for the given locale and key.</summary>
    public SetLocalizationValueCommand(
        LocalizationEditorModel model,
        string locale,
        string key,
        string oldValue,
        string newValue)
    {
        _model    = model;
        _locale   = locale;
        _key      = key;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    /// <inheritdoc/>
    public string Description => $"Set [{_locale}][{_key}]";

    /// <inheritdoc/>
    public void Execute() => _model.SetValue(_locale, _key, _newValue);

    /// <inheritdoc/>
    public void Undo() => _model.SetValue(_locale, _key, _oldValue);
}
