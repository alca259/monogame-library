namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record ScriptCreationResult(string ClassName, string NamespaceName, string RelativeFolder);

/// <summary>Diálogo modal "New Script". La lógica vive en <see cref="ScriptCreationViewModel"/>.</summary>
public sealed partial class ScriptCreationDialog : ContentPage
{
    private readonly ScriptCreationViewModel _vm = new();
    private readonly TaskCompletionSource<ScriptCreationResult?> _tcs = new();

    private ScriptCreationDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<ScriptCreationResult?> ShowAsync(INavigation navigation, string defaultNamespace = "")
    {
        var dialog = new ScriptCreationDialog();
        if (!string.IsNullOrEmpty(defaultNamespace))
            dialog._vm.NamespaceName = defaultNamespace;
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(ScriptCreationResult? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
