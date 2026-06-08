namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed record LocaleCreationResult(string LocaleCode, string DisplayName);

/// <summary>Diálogo modal "Add Locale". La lógica vive en <see cref="LocaleCreationViewModel"/>.</summary>
public sealed partial class LocaleCreationDialog : ContentPage
{
    private readonly LocaleCreationViewModel _vm = new();
    private readonly TaskCompletionSource<LocaleCreationResult?> _tcs = new();

    private LocaleCreationDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

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

    private void OnClose(LocaleCreationResult? result)
    {
        _tcs.TrySetResult(result);
        _ = Navigation.PopModalAsync();
    }
}
