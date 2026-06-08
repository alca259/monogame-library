namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Inspector tab "UI Theme Editor". La lógica y las cinco secciones NineSlice viven en
/// <see cref="UIThemeInspectorViewModel"/>; el code-behind enlaza la VM y gestiona su ciclo
/// de vida (Attach/Detach).
/// </summary>
public sealed partial class UIThemeInspectorView : ContentView
{
    private readonly UIThemeInspectorViewModel _vm = new();

    public UIThemeInspectorView()
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
