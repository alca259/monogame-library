using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "New Behaviour": clase, namespace, carpeta y métodos a sobrescribir.</summary>
public sealed partial class NewBehaviourViewModel : DialogViewModel<NewBehaviourResult>
{
    public ObservableCollection<string> KnownNamespaces { get; }

    [ObservableProperty]
    private int _selectedNamespaceIndex = -1;

    [ObservableProperty]
    private string _className = string.Empty;

    [ObservableProperty]
    private string _namespaceName = string.Empty;

    [ObservableProperty]
    private string _relativeFolder = string.Empty;

    [ObservableProperty] private bool _overrideAwake;
    [ObservableProperty] private bool _overrideStart;
    [ObservableProperty] private bool _overrideUpdate = true;
    [ObservableProperty] private bool _overrideDraw;
    [ObservableProperty] private bool _overrideOnDestroy;

    public NewBehaviourViewModel(IReadOnlyList<string> knownNamespaces)
    {
        KnownNamespaces = [.. knownNamespaces];
    }

    partial void OnSelectedNamespaceIndexChanged(int value)
    {
        if (value >= 0 && value < KnownNamespaces.Count)
            NamespaceName = KnownNamespaces[value];
    }

    [RelayCommand]
    private void Submit()
    {
        string className = ClassName?.Trim() ?? string.Empty;

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

        string ns     = NamespaceName?.Trim()   ?? string.Empty;
        string folder = RelativeFolder?.Trim()  ?? string.Empty;

        List<string> methods = [];
        if (OverrideAwake)     methods.Add("Awake");
        if (OverrideStart)     methods.Add("Start");
        if (OverrideUpdate)    methods.Add("Update");
        if (OverrideDraw)      methods.Add("Draw");
        if (OverrideOnDestroy) methods.Add("OnDestroy");

        Close(new NewBehaviourResult(className, ns, folder, methods));
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
