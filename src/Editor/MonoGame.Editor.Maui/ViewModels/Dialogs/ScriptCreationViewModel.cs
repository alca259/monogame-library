using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "New Script": clase, namespace y carpeta relativa.</summary>
public sealed partial class ScriptCreationViewModel : DialogViewModel<ScriptCreationResult>
{
    [ObservableProperty]
    private string _className = string.Empty;

    [ObservableProperty]
    private string _namespaceName = string.Empty;

    [ObservableProperty]
    private string _relativeFolder = string.Empty;

    [RelayCommand]
    private void Submit()
    {
        string className = ClassName?.Trim() ?? string.Empty;
        string ns = NamespaceName?.Trim() ?? string.Empty;
        string folder = RelativeFolder?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(className))
        {
            ShowError("Class name is required.");
            return;
        }

        if (!IsValidIdentifier(className))
        {
            ShowError("Class name must be a valid C# identifier.");
            return;
        }

        Close(new ScriptCreationResult(className, ns, folder));
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }
}
