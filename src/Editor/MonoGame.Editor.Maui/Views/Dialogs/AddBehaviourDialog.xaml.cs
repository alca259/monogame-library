namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>Diálogo modal "Add Behaviour". La lógica vive en <see cref="AddBehaviourViewModel"/>.</summary>
public sealed partial class AddBehaviourDialog : ContentPage
{
    private readonly AddBehaviourViewModel _vm = new();
    private readonly TaskCompletionSource<string?> _tcs = new();

    private AddBehaviourDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<string?> ShowAsync(INavigation navigation,
                                                 GameObjectRegistry registry,
                                                 Func<Task>? rescanCallback = null)
    {
        var dialog = new AddBehaviourDialog();
        dialog._vm.Initialize(registry, rescanCallback);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(string? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
