namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewBehaviourResult(
    string ClassName,
    string NamespaceName,
    string RelativeFolder,
    IReadOnlyList<string> SelectedMethods);

public sealed partial class NewBehaviourDialog : ContentPage
{
    private static readonly Color ErrorColor = Color.FromArgb("#E85050");

    private readonly TaskCompletionSource<NewBehaviourResult?> _tcs = new();
    private readonly List<string> _knownNamespaces;

    private NewBehaviourDialog(IReadOnlyList<string> knownNamespaces)
    {
        InitializeComponent();
        _knownNamespaces = [.. knownNamespaces];
        foreach (string ns in _knownNamespaces)
            NamespacePicker.Items.Add(ns);
    }

    public static async Task<NewBehaviourResult?> ShowAsync(INavigation navigation,
                                                             IReadOnlyList<string> knownNamespaces)
    {
        var dialog = new NewBehaviourDialog(knownNamespaces);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    private void OnNamespacePickerChanged(object sender, EventArgs e)
    {
        if (NamespacePicker.SelectedIndex >= 0)
            NamespaceCustomEntry.Text = _knownNamespaces[NamespacePicker.SelectedIndex];
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
        string className = ClassNameEntry.Text?.Trim() ?? string.Empty;

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

        string ns     = NamespaceCustomEntry.Text?.Trim() ?? string.Empty;
        string folder = RelativeFolderEntry.Text?.Trim()  ?? string.Empty;

        List<string> methods = [];
        if (AwakeCheck.IsChecked)    methods.Add("Awake");
        if (StartCheck.IsChecked)    methods.Add("Start");
        if (UpdateCheck.IsChecked)   methods.Add("Update");
        if (DrawCheck.IsChecked)     methods.Add("Draw");
        if (OnDestroyCheck.IsChecked) methods.Add("OnDestroy");

        _tcs.TrySetResult(new NewBehaviourResult(className, ns, folder, methods));
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
