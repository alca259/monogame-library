namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record NewBehaviourResult(
    string ClassName,
    string NamespaceName,
    string RelativeFolder,
    IReadOnlyList<string> SelectedMethods);

/// <summary>Diálogo modal "New Behaviour". La lógica vive en <see cref="NewBehaviourViewModel"/>.</summary>
public sealed partial class NewBehaviourDialog : ContentPage
{
    private readonly NewBehaviourViewModel _vm;
    private readonly TaskCompletionSource<NewBehaviourResult?> _tcs = new();

    private NewBehaviourDialog(IReadOnlyList<string> knownNamespaces)
    {
        InitializeComponent();
        _vm = new NewBehaviourViewModel(knownNamespaces);
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<NewBehaviourResult?> ShowAsync(INavigation navigation,
                                                             IReadOnlyList<string> knownNamespaces)
    {
        var dialog = new NewBehaviourDialog(knownNamespaces);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(NewBehaviourResult? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
