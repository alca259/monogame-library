namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record ScriptCreationResult(string ClassName, string NamespaceName, string RelativeFolder);

public sealed partial class ScriptCreationDialog : ContentPage
{
    private static readonly Color ErrorColor = Color.FromArgb("#E85050");

    private readonly TaskCompletionSource<ScriptCreationResult?> _tcs = new();

    private ScriptCreationDialog() => InitializeComponent();

    public static async Task<ScriptCreationResult?> ShowAsync(INavigation navigation,
                                                               string defaultNamespace = "")
    {
        var dialog = new ScriptCreationDialog();
        if (!string.IsNullOrEmpty(defaultNamespace))
            dialog.NamespaceEntry.Text = defaultNamespace;
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
        string className  = ClassNameEntry.Text?.Trim()        ?? string.Empty;
        string ns         = NamespaceEntry.Text?.Trim()        ?? string.Empty;
        string folder     = RelativeFolderEntry.Text?.Trim()   ?? string.Empty;

        if (string.IsNullOrEmpty(className))
        {
            ShowError("Class name is required.");
            return;
        }

        if (!IsValidIdentifier(className))
        {
            ShowError("Class name must be a valid C# identifier.");
            return;
        }

        _tcs.TrySetResult(new ScriptCreationResult(className, ns, folder));
        _ = Navigation.PopModalAsync();
    }

    private void ShowError(string message)
    {
        ValidationLabel.Text      = message;
        ValidationLabel.TextColor = ErrorColor;
        ValidationLabel.IsVisible = true;
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }
}
