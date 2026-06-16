namespace MonoGame.Editor.Maui;

public sealed partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        Drawers.BehaviourEditorRegistry.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new EditorWindow())
        {
            MinimumWidth = 1280,
            MinimumHeight = 720,
            TitleBar = new TitleBarView(),
        };
    }
}
