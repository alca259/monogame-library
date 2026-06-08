namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Scripts". La lógica vive en <see cref="ScriptBrowserViewModel"/>; el
/// code-behind enlaza la VM, gestiona su ciclo de vida y la expone como <see cref="Vm"/>
/// para que los menús contextuales de las plantillas puedan invocar sus comandos.
/// </summary>
public sealed partial class ScriptBrowserView : ContentView
{
    private readonly ScriptBrowserViewModel _vm = new();

    /// <summary>VM tipada, referenciada desde los <c>MenuFlyout</c> de las plantillas.</summary>
    public ScriptBrowserViewModel Vm => _vm;

    public ScriptBrowserView()
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
