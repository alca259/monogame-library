namespace MonoGame.Editor.Maui.Views.Dialogs;

/// <summary>
/// Diálogo modal para seleccionar una ruta relativa al proyecto. Navega el sistema de
/// ficheros con raíz en una carpeta base y devuelve la ruta relativa elegida.
/// La lógica vive en <see cref="RelativePathPickerViewModel"/>.
/// </summary>
public sealed partial class RelativePathPickerDialog : ContentPage
{
    private readonly RelativePathPickerViewModel _vm = new();
    private readonly TaskCompletionSource<string?> _tcs = new();

    private RelativePathPickerDialog()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.CloseRequested += OnClose;
    }

    /// <summary>
    /// Muestra el diálogo modal y devuelve la ruta relativa seleccionada, o <c>null</c> si se cancela.
    /// </summary>
    /// <param name="navigation">Navegación activa de la página.</param>
    /// <param name="baseFolder">Carpeta raíz desde la que se navega (ruta absoluta).</param>
    /// <param name="filesMode"><c>true</c> para seleccionar un fichero; <c>false</c> para seleccionar una carpeta.</param>
    /// <param name="extensions">Extensiones de fichero permitidas en modo fichero, ej. <c>[".csproj"]</c>. Nulo o vacío = cualquier fichero.</param>
    /// <param name="title">Texto del encabezado del diálogo.</param>
    public static async Task<string?> ShowAsync(
        INavigation navigation,
        string baseFolder,
        bool filesMode = false,
        string[]? extensions = null,
        string title = "Select Path")
    {
        var dialog = new RelativePathPickerDialog();
        dialog._vm.Initialize(baseFolder, filesMode, extensions, title);
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
