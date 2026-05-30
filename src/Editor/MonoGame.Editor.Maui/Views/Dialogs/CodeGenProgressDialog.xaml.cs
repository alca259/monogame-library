namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>
/// Display-only dialog showing per-file code generation progress.
/// Show it first, then call <see cref="AddFileResult"/> for each generated file,
/// and finally <see cref="MarkComplete"/> when the pipeline finishes.
/// </summary>
public sealed partial class CodeGenProgressDialog : ContentPage
{
    private static readonly Color SuccessColor = Color.FromArgb("#50C878");
    private static readonly Color FailureColor = Color.FromArgb("#E85050");
    private static readonly Color DimColor     = Color.FromArgb("#6A6A72");

    private readonly TaskCompletionSource<bool> _tcs = new();

    internal CodeGenProgressDialog() => InitializeComponent();

    public static async Task<bool> ShowAsync(INavigation navigation)
    {
        var dialog = new CodeGenProgressDialog();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    // ── Public API for the code-gen pipeline ─────────────────────────────────

    public void AddFileResult(string filePath, bool success)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)),
                ColumnSpacing = 8,
                Padding       = new Thickness(4, 3),
            };

            var icon = new Label
            {
                Text      = success ? "✓" : "✗",
                TextColor = success ? SuccessColor : FailureColor,
                FontSize  = 12,
                VerticalOptions = LayoutOptions.Center,
            };
            Grid.SetColumn(icon, 0);

            var fileLabel = new Label
            {
                Text      = filePath,
                TextColor = success ? SuccessColor : FailureColor,
                FontSize  = 12,
                VerticalOptions = LayoutOptions.Center,
            };
            Grid.SetColumn(fileLabel, 1);

            row.Children.Add(icon);
            row.Children.Add(fileLabel);
            FileListStack.Children.Add(row);
        });
    }

    public void MarkComplete(int successCount, int failedCount)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SummaryLabel.Text      = failedCount == 0
                ? $"Done — {successCount} file(s) generated."
                : $"Done — {successCount} succeeded, {failedCount} failed.";
            SummaryLabel.TextColor = failedCount == 0 ? SuccessColor : FailureColor;
            CloseButton.IsEnabled  = true;
        });
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    protected override bool OnBackButtonPressed() => true; // block while running

    private void OnClose(object sender, EventArgs e)
    {
        _tcs.TrySetResult(true);
        _ = Navigation.PopModalAsync();
    }
}
