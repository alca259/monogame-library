using System.Collections.Specialized;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Console del dock inferior. La lógica de filtrado y acumulación vive en
/// <see cref="ConsolePanelViewModel"/>; el code-behind enlaza la VM, gestiona su ciclo
/// de vida y mantiene el auto-scroll al final (responsabilidad de la vista).
/// </summary>
public sealed partial class ConsolePanelView : ContentView
{
    private readonly ConsolePanelViewModel _vm = new();

    public ConsolePanelView()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.VisibleEntries.CollectionChanged += OnVisibleEntriesChanged;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }

    private void OnVisibleEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        int count = _vm.VisibleEntries.Count;
        if (count == 0) return;
        LogList.ScrollTo(count - 1, position: ScrollToPosition.End, animate: false);
    }
}
