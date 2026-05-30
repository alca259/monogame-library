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
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win is null) return null;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            var picker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
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
