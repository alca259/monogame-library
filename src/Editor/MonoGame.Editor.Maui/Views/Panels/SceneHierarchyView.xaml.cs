namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel izquierdo: árbol de jerarquía de entidades. La lógica vive en
/// <see cref="SceneHierarchyViewModel"/>; el code-behind enlaza la VM y gestiona su
/// ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class SceneHierarchyView : ContentView
{
    private readonly SceneHierarchyViewModel _vm = new();

    public SceneHierarchyView()
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
