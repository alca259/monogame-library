namespace MonoGame.Editor.Maui.Services;

/// <summary>
/// Servicio estático que abstrae las interacciones de UI (alertas, prompts, action
/// sheets y selección de ficheros/carpetas) para que las ViewModels puedan invocarlas
/// sin referenciar una <see cref="Page"/> concreta. Resuelve internamente la página y
/// ventana activas, en línea con el acceso por singleton del resto del editor.
/// </summary>
public static class DialogService
{
    /// <summary>Página actualmente visible (o <c>null</c> si no hay ventana).</summary>
    public static Page? CurrentPage
        => Application.Current?.Windows.FirstOrDefault()?.Page;

    /// <summary>Navegación de la página actual, usada para abrir diálogos modales.</summary>
    public static INavigation? Navigation => CurrentPage?.Navigation;

    /// <summary>Muestra una alerta de confirmación. Devuelve <c>true</c> si se acepta.</summary>
    public static Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Page? page = CurrentPage;
        return page is null
            ? Task.FromResult(false)
            : page.DisplayAlertAsync(title, message, accept, cancel);
    }

    /// <summary>Muestra una alerta informativa con un único botón.</summary>
    public static Task AlertAsync(string title, string message, string cancel = "OK")
    {
        Page? page = CurrentPage;
        return page is null ? Task.CompletedTask : page.DisplayAlertAsync(title, message, cancel);
    }

    /// <summary>Solicita una entrada de texto al usuario. Devuelve <c>null</c> si se cancela.</summary>
    public static Task<string?> PromptAsync(
        string title,
        string message,
        string initialValue = "",
        int maxLength = -1,
        Keyboard? keyboard = null)
    {
        Page? page = CurrentPage;
        return page is null
            ? Task.FromResult<string?>(null)
            : page.DisplayPromptAsync(
                title, message,
                initialValue: initialValue,
                maxLength: maxLength,
                keyboard: keyboard ?? Keyboard.Text);
    }

    /// <summary>Muestra un action sheet con las opciones indicadas.</summary>
    public static Task<string?> ActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        Page? page = CurrentPage;
        return page is null
            ? Task.FromResult<string?>(null)
            : page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
    }

    /// <summary>Selección de un fichero. Devuelve <c>null</c> si se cancela.</summary>
    public static async Task<string?> PickFileAsync(PickOptions? options = null)
    {
        FileResult? result = await FilePicker.Default.PickAsync(options ?? new PickOptions()).ConfigureAwait(true);
        return result?.FullPath;
    }

    /// <summary>
    /// Selección de una carpeta vía picker nativo de WinUI. Devuelve <c>null</c> si se
    /// cancela o si la plataforma no lo soporta.
    /// </summary>
    public static async Task<string?> PickFolderAsync()
    {
#if WINDOWS
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
#else
        await Task.CompletedTask;
        return null;
#endif
    }
}
