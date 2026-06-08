using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel del dock inferior: gestiona qué pestaña está activa. La visibilidad del
/// contenido la aplica la vista (los paneles hijos tienen su propio BindingContext).
/// </summary>
public sealed partial class DockBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _activeTab = "Scenes";

    [RelayCommand]
    private void SelectTab(string? tab) => ActiveTab = tab ?? "Scenes";
}
