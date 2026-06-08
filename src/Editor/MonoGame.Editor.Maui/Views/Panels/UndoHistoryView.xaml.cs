namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "History". La lógica vive en <see cref="UndoHistoryViewModel"/>; el
/// code-behind solo enlaza la VM y gestiona su ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class UndoHistoryView : ContentView
{
    private readonly UndoHistoryViewModel _vm = new();

    public UndoHistoryView()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }
}
