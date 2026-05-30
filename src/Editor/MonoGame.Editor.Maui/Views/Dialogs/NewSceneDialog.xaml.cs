namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewSceneResult(string SceneName, float WorldWidth, float WorldHeight);

public sealed partial class NewSceneDialog : ContentPage
{
    private static readonly Color ErrorColor = Color.FromArgb("#E85050");

    private readonly TaskCompletionSource<NewSceneResult?> _tcs = new();

    private NewSceneDialog() => InitializeComponent();

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

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        string name = SceneNameEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Scene name is required.");
            return;
        }

        if (!float.TryParse(WorldWidthEntry.Text, out float width) || width <= 0f)
        {
            ShowError("World width must be a positive number.");
            return;
        }

        if (!float.TryParse(WorldHeightEntry.Text, out float height) || height <= 0f)
        {
            ShowError("World height must be a positive number.");
            return;
        }

        _tcs.TrySetResult(new NewSceneResult(name, width, height));
        _ = Navigation.PopModalAsync();
    }

    private void ShowError(string message)
    {
        ValidationLabel.Text      = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }
}
