namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Sprite Editor". La lógica vive en <see cref="SpriteInspectorViewModel"/>;
/// el code-behind enlaza la VM y gestiona su ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class SpriteInspectorView : ContentView
{
    private readonly SpriteInspectorViewModel _vm = new();

    public SpriteInspectorView()
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
