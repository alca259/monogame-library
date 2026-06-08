using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>Fila de resultado por fichero generado en el diálogo de code-gen.</summary>
public sealed class CodeGenFileItem(string icon, string text, Color color)
{
    public string Icon  { get; } = icon;
    public string Text  { get; } = text;
    public Color  Color { get; } = color;
}

/// <summary>
/// ViewModel del diálogo de progreso de generación de código. La pipeline llama a
/// <see cref="AddFileResult"/> por cada fichero y a <see cref="MarkComplete"/> al terminar.
/// </summary>
public sealed partial class CodeGenProgressViewModel : ObservableObject
{
    private static readonly Color SuccessColor = Color.FromArgb("#50C878");
    private static readonly Color FailureColor = Color.FromArgb("#E85050");
    private static readonly Color DimColor     = Color.FromArgb("#6A6A72");

    /// <summary>Se dispara cuando el usuario cierra el diálogo.</summary>
    public event Action? CloseRequested;

    public ObservableCollection<CodeGenFileItem> Files { get; } = [];

    [ObservableProperty]
    private string _summaryText = "Running…";

    [ObservableProperty]
    private Color _summaryColor = DimColor;

    [ObservableProperty]
    private bool _canClose;

    public void AddFileResult(string filePath, bool success)
        => MainThread.BeginInvokeOnMainThread(() =>
            Files.Add(new CodeGenFileItem(success ? "✓" : "✗", filePath, success ? SuccessColor : FailureColor)));

    public void MarkComplete(int successCount, int failedCount)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            SummaryText  = failedCount == 0
                ? $"Done — {successCount} file(s) generated."
                : $"Done — {successCount} succeeded, {failedCount} failed.";
            SummaryColor = failedCount == 0 ? SuccessColor : FailureColor;
            CanClose     = true;
        });

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
