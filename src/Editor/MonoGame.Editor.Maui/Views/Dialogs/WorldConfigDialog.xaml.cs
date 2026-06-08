namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>Diálogo modal "World Config". La lógica vive en <see cref="WorldConfigViewModel"/>.</summary>
public sealed partial class WorldConfigDialog : ContentPage
{
    private readonly WorldConfigViewModel _vm = new();
    private readonly TaskCompletionSource<EditorWorldConfig?> _tcs = new();

    private WorldConfigDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<EditorWorldConfig?> ShowAsync(INavigation navigation,
                                                            EditorWorldConfig? existing = null)
    {
        var dialog = new WorldConfigDialog();
        if (existing is not null) dialog._vm.LoadFrom(existing);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnClose(EditorWorldConfig? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
