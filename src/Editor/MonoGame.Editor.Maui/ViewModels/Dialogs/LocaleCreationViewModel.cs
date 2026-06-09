using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "Add Locale": código de locale y nombre visible.</summary>
public sealed partial class LocaleCreationViewModel : DialogViewModel<LocaleCreationResult>
{
    [ObservableProperty]
    private string _localeCode = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [RelayCommand]
    private void Submit()
    {
        string code        = LocaleCode?.Trim()  ?? string.Empty;
        string displayName = DisplayName?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Locale code is required (e.g. en-US).");
            return;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            ShowError("Display name is required.");
            return;
        }

        Close(new LocaleCreationResult(code, displayName));
    }
}
