using System.ComponentModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock inferior: barra de pestañas. La pestaña activa la decide
/// <see cref="DockBarViewModel"/>; la vista aplica la visibilidad del contenido,
/// ya que cada panel hijo tiene su propio BindingContext.
/// </summary>
public sealed partial class DockBarView : ContentView
{
    private readonly DockBarViewModel _vm = new();

    public DockBarView()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        UpdateTabContent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockBarViewModel.ActiveTab))
            UpdateTabContent();
    }

    private void UpdateTabContent()
    {
        string tab = _vm.ActiveTab;
        ScenesContent.IsVisible = tab == "Scenes";
        AssetsContent.IsVisible = tab == "Assets";
        ConsoleContent.IsVisible = tab == "Console";
        LocalizationContent.IsVisible = tab == "Localization";
        InputMapsContent.IsVisible = tab == "InputMaps";
        TilemapContent.IsVisible = tab == "Tilemap";
        HistoryContent.IsVisible = tab == "History";
        ScriptsContent.IsVisible = tab == "Scripts";
    }
}
