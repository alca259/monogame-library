namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewProjectResult(string ProjectName, string ParentPath, string GameCsprojPath);

/// <summary>Diálogo modal "New Project". La lógica vive en <see cref="NewProjectViewModel"/>.</summary>
public sealed partial class NewProjectDialog : ContentPage
{
    private readonly NewProjectViewModel _vm = new();
    private readonly TaskCompletionSource<NewProjectResult?> _tcs = new();

    private NewProjectDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<NewProjectResult?> ShowAsync(INavigation navigation)
    {
        var dialog = new NewProjectDialog();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(NewProjectResult? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
