namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>Diálogo modal "Project Settings". La lógica vive en <see cref="ProjectSettingsViewModel"/>.</summary>
public sealed partial class ProjectSettingsDialog : ContentPage
{
    private readonly ProjectSettingsViewModel _vm = new();
    private readonly TaskCompletionSource<bool> _tcs = new();

    private ProjectSettingsDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<bool> ShowAsync(INavigation navigation,
                                              EditorProject project,
                                              ProjectSettings settings)
    {
        var dialog = new ProjectSettingsDialog();
        dialog._vm.Initialize(project, settings);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(false);
        return base.OnBackButtonPressed();
    }

    private void OnClose(bool result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
