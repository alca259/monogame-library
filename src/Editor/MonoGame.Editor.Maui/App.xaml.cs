namespace MonoGame.Editor.Maui;

public sealed partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new EditorWindow())
        {
            Title = "MonoGame Editor",
            MinimumWidth = 1280,
            MinimumHeight = 720,
        };
    }
}
