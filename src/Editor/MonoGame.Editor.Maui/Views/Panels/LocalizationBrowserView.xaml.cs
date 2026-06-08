namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Localization". La lógica vive en <see cref="LocalizationBrowserViewModel"/>;
/// el code-behind enlaza la VM y gestiona su ciclo de vida (Attach/Detach).
/// </summary>
public sealed partial class LocalizationBrowserView : ContentView
{
    private readonly LocalizationBrowserViewModel _vm = new();

    public LocalizationBrowserView()
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
