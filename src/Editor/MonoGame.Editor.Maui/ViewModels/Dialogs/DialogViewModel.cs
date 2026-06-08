using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>
/// Base de las ViewModels de diálogo modal. Gestiona el mensaje de validación y el cierre del
/// diálogo: la VM expone la lógica/validación y, al confirmar o cancelar, dispara
/// <see cref="CloseRequested"/> con el resultado (o <c>null</c>). El code-behind del diálogo
/// conserva el <c>TaskCompletionSource</c> y la navegación modal.
/// </summary>
/// <typeparam name="TResult">Tipo del resultado devuelto por el diálogo.</typeparam>
public abstract partial class DialogViewModel<TResult> : ObservableObject
{
    /// <summary>Se dispara cuando el diálogo debe cerrarse, con el resultado (o <c>null</c>).</summary>
    public event Action<TResult?>? CloseRequested;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationError;

    /// <summary>Muestra un error de validación y mantiene el diálogo abierto.</summary>
    protected void ShowError(string message)
    {
        ValidationMessage  = message;
        HasValidationError = true;
    }

    /// <summary>Cierra el diálogo devolviendo <paramref name="result"/>.</summary>
    protected void Close(TResult? result) => CloseRequested?.Invoke(result);

    [RelayCommand]
    protected void Cancel() => Close(default);
}
