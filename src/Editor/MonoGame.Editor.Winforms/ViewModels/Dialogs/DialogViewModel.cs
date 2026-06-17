using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Winforms.ViewModels.Dialogs;

/// <summary>
/// Base de las ViewModels de diálogo modal (port directo de MAUI sin referencias a tipos MAUI).
/// La VM gestiona validación y cierre; el Form code-behind conserva la navegación modal y el
/// <c>TaskCompletionSource</c>.
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
        ValidationMessage = message;
        HasValidationError = true;
    }

    /// <summary>Limpia el estado de validación.</summary>
    protected void ClearError()
    {
        ValidationMessage  = string.Empty;
        HasValidationError = false;
    }

    /// <summary>Cierra el diálogo devolviendo <paramref name="result"/>.</summary>
    protected void Close(TResult? result) => CloseRequested?.Invoke(result);

    [RelayCommand]
    protected void Cancel() => Close(default);
}
