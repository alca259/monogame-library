namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewProjectResult(string ProjectName, string ParentPath, string GameCsprojPath);

public sealed partial class NewProjectDialog : ContentPage
{
    private static readonly Color ErrorColor = Color.FromArgb("#E85050");

    private readonly TaskCompletionSource<NewProjectResult?> _tcs = new();

    private NewProjectDialog() => InitializeComponent();

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

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        string name   = ProjectNameEntry.Text?.Trim() ?? string.Empty;
        string parent = ParentPathEntry.Text?.Trim()  ?? string.Empty;
        string csproj = GameCsprojEntry.Text?.Trim()  ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Project name is required.");
            return;
        }

        if (string.IsNullOrEmpty(parent))
        {
            ShowError("Parent folder is required.");
            return;
        }

        _tcs.TrySetResult(new NewProjectResult(name, parent, csproj));
        _ = Navigation.PopModalAsync();
    }

    private async void OnBrowseParentClicked(object sender, EventArgs e)
    {
        string? picked = await PickFolderAsync();
        if (picked is not null)
            ParentPathEntry.Text = picked;
    }

    private async void OnBrowseCsprojClicked(object sender, EventArgs e)
    {
        string? picked = await PickFileAsync("*.csproj");
        if (picked is not null)
            GameCsprojEntry.Text = picked;
    }

    private void ShowError(string message)
    {
        ValidationLabel.Text      = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }

    // ── File/folder pickers ───────────────────────────────────────────────────

    private static async Task<string?> PickFolderAsync()
    {
        try
        {
            FolderPickerResult result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            return result.IsSuccessful ? result.Folder.Path : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> PickFileAsync(string pattern)
    {
        try
        {
            FileResult? result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select .csproj file"
            });
            return result?.FullPath;
        }
        catch
        {
            return null;
        }
    }
}
