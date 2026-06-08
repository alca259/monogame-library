namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Input Maps". La lógica vive en <see cref="InputMapEditorViewModel"/>; el
/// code-behind enlaza la VM y gestiona su ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class InputMapEditorView : ContentView
{
    private readonly InputMapEditorViewModel _vm = new();

    public InputMapEditorView()
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
