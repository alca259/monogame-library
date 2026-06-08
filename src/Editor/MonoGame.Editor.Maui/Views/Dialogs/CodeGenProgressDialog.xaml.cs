namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>
/// Diálogo de progreso de generación de código. Muéstralo, llama a <see cref="AddFileResult"/>
/// por cada fichero y a <see cref="MarkComplete"/> al terminar. La lógica vive en
/// <see cref="CodeGenProgressViewModel"/>.
/// </summary>
public sealed partial class CodeGenProgressDialog : ContentPage
{
    private readonly CodeGenProgressViewModel _vm = new();
    private readonly TaskCompletionSource<bool> _tcs = new();

    internal CodeGenProgressDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    public static async Task<bool> ShowAsync(INavigation navigation)
    {
        var dialog = new CodeGenProgressDialog();
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    // ── Public API for the code-gen pipeline ─────────────────────────────────

    public void AddFileResult(string filePath, bool success) => _vm.AddFileResult(filePath, success);

    public void MarkComplete(int successCount, int failedCount) => _vm.MarkComplete(successCount, failedCount);

    // ── Navigation ────────────────────────────────────────────────────────────

    protected override bool OnBackButtonPressed() => true; // block while running

    private void OnClose()
    {
        _tcs.TrySetResult(true);
        _ = Navigation.PopModalAsync();
    }
}
