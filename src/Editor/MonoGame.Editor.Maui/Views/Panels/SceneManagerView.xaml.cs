namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Scenes del dock inferior. La lógica vive en <see cref="SceneManagerViewModel"/>;
/// el code-behind enlaza la VM y gestiona su ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class SceneManagerView : ContentView
{
    private readonly SceneManagerViewModel _vm = new();

    public SceneManagerView()
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
