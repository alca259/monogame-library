using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña "History" del dock. Expone la pila de undo (más antiguo →
/// más reciente) y la de redo, más un resumen <c>n / max</c>. Se reconstruye al
/// recibir <see cref="UndoPerformedEvent"/> / <see cref="RedoPerformedEvent"/>.
/// </summary>
public sealed partial class UndoHistoryViewModel : ViewModelBase
{
    /// <summary>Descripciones de undo, ordenadas de más antigua a más reciente.</summary>
    public ObservableCollection<string> UndoEntries { get; } = [];

    /// <summary>Descripciones de redo (en gris en la vista).</summary>
    public ObservableCollection<string> RedoEntries { get; } = [];

    [ObservableProperty]
    private string _summaryText = "0 / 100";

    protected override void RegisterEvents()
    {
        On<UndoPerformedEvent>(_ => Rebuild());
        On<RedoPerformedEvent>(_ => Rebuild());
    }

    protected override void OnAttached() => Rebuild();

    [RelayCommand]
    private void Clear()
    {
        Context.Commands.Clear();
        Rebuild();
    }

    private void Rebuild()
    {
        CommandStack commands = Context.Commands;
        IReadOnlyList<string> undos = commands.GetUndoDescriptions();
        IReadOnlyList<string> redos = commands.GetRedoDescriptions();

        UndoEntries.Clear();
        // GetUndoDescriptions devuelve reciente→antiguo; mostramos antiguo→reciente.
        for (int i = undos.Count - 1; i >= 0; i--)
            UndoEntries.Add(undos[i]);

        RedoEntries.Clear();
        foreach (string desc in redos)
            RedoEntries.Add(desc);

        SummaryText = $"{undos.Count} / {commands.MaxHistory}";
    }
}
