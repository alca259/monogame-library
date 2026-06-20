namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña "History": expone la pila de undo (más antiguo → más
/// reciente) y la de redo, más el resumen <c>n / max</c>.
/// </summary>
public sealed class UndoHistoryViewModel : ViewModelBase
{
    /// <summary>Se dispara cuando el estado de la pila cambia y hay que repintar las listas.</summary>
    public event Action? RebuildRequested;

    public IReadOnlyList<string> UndoEntries { get; private set; } = [];
    public IReadOnlyList<string> RedoEntries { get; private set; } = [];
    public string SummaryText { get; private set; } = "0 / 0";

    protected override void RegisterEvents()
    {
        On<UndoPerformedEvent>(_ => Rebuild());
        On<RedoPerformedEvent>(_ => Rebuild());
        On<SceneLoadedEvent>(_ => Rebuild());
        On<ProjectOpenedEvent>(_ => Rebuild());
    }

    protected override void OnAttached() => Rebuild();

    /// <summary>Limpia toda la pila de comandos.</summary>
    public void Clear()
    {
        Context.Commands.Clear();
        Rebuild();
    }

    private void Rebuild()
    {
        CommandStack commands = Context.Commands;
        IReadOnlyList<string> undos = commands.GetUndoDescriptions();
        IReadOnlyList<string> redos = commands.GetRedoDescriptions();

        // GetUndoDescriptions devuelve reciente→antiguo; mostramos antiguo→reciente
        string[] undoArr = new string[undos.Count];
        for (int i = 0; i < undos.Count; i++)
            undoArr[i] = undos[undos.Count - 1 - i];

        UndoEntries = undoArr;
        RedoEntries = redos;
        SummaryText = $"{undos.Count} / {commands.MaxHistory}";
        RebuildRequested?.Invoke();
    }
}
