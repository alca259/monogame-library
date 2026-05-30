namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record LocaleCreationResult(string LocaleCode, string DisplayName);

public sealed partial class LocaleCreationDialog : ContentPage
{
    private static readonly Color ErrorColor = Color.FromArgb("#E85050");

    private readonly TaskCompletionSource<LocaleCreationResult?> _tcs = new();

    private LocaleCreationDialog() => InitializeComponent();

    public static async Task<LocaleCreationResult?> ShowAsync(INavigation navigation)
    {
        var dialog = new LocaleCreationDialog();
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
        string code        = LocaleCodeEntry.Text?.Trim()   ?? string.Empty;
        string displayName = DisplayNameEntry.Text?.Trim()  ?? string.Empty;

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Locale code is required (e.g. en-US).");
            return;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            ShowError("Display name is required.");
            return;
        }

        _tcs.TrySetResult(new LocaleCreationResult(code, displayName));
        _ = Navigation.PopModalAsync();
    }

    private void ShowError(string message)
    {
        ValidationLabel.Text      = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }
}
