namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewSceneResult(string SceneName, float WorldWidth, float WorldHeight);

/// <summary>Diálogo modal "New Scene". La lógica vive en <see cref="NewSceneViewModel"/>.</summary>
public sealed partial class NewSceneDialog : ContentPage
{
    private readonly NewSceneViewModel _vm = new();
    private readonly TaskCompletionSource<NewSceneResult?> _tcs = new();

    private NewSceneDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<NewSceneResult?> ShowAsync(INavigation navigation)
    {
        var dialog = new NewSceneDialog();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(NewSceneResult? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
