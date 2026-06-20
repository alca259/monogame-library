using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Console del dock inferior. Acumula entradas de log y salida de build
/// (hasta <c>1 000</c> líneas) y expone la lista filtrada por nivel.
/// </summary>
public sealed class ConsolePanelViewModel : ViewModelBase
{
    private const int MaxLogEntries = 1000;

    private readonly List<string> _allEntries     = [];
    private readonly List<string> _visibleEntries = [];

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Console;

    public bool ShowInfo  { get; private set; } = true;
    public bool ShowWarn  { get; private set; } = true;
    public bool ShowError { get; private set; } = true;

    /// <summary>Entradas actualmente visibles según los filtros activos (snapshot inmutable).</summary>
    public IReadOnlyList<string> VisibleEntries => _visibleEntries;

    /// <summary>Se dispara cuando se añade una nueva línea visible. El panel la añade al RichTextBox sin reconstruir.</summary>
    public event Action<string, LogLevel>? EntryAppended;

    /// <summary>Se dispara cuando los filtros cambian y hay que reconstruir la vista completa.</summary>
    public event Action? VisibleEntriesRebuilt;

    /// <summary>Se dispara cuando el usuario limpia el log.</summary>
    public event Action? Cleared;

    // ── Eventos del bus ───────────────────────────────────────────────────────

    protected override void RegisterEvents()
    {
        On<LogEntryAddedEvent>(OnLogEntry);
        On<BuildOutputLineEvent>(OnBuildOutput);
    }

    private void OnLogEntry(LogEntryAddedEvent e)
    {
        string prefix = e.Entry.Level switch
        {
            LogLevel.Warning => "[WARN] ",
            LogLevel.Error   => "[ERR]  ",
            LogLevel.Debug   => "[DBG]  ",
            _                => "[INFO] "
        };

        string line = $"{e.Entry.Timestamp:HH:mm:ss} {prefix}{e.Entry.Message}";
        Append(line);

        bool show = e.Entry.Level switch
        {
            LogLevel.Warning => ShowWarn,
            LogLevel.Error   => ShowError,
            _                => ShowInfo
        };

        if (!show) return;
        _visibleEntries.Add(line);
        EntryAppended?.Invoke(line, e.Entry.Level);
    }

    private void OnBuildOutput(BuildOutputLineEvent e)
    {
        string prefix = e.IsError ? "[ERR]  " : "[BLD]  ";
        string line   = $"{DateTime.Now:HH:mm:ss} {prefix}{e.Line}";
        Append(line);

        if (!ShowInfo) return;
        _visibleEntries.Add(line);
        EntryAppended?.Invoke(line, e.IsError ? LogLevel.Error : LogLevel.Info);
    }

    private void Append(string line)
    {
        if (_allEntries.Count >= MaxLogEntries) _allEntries.RemoveAt(0);
        _allEntries.Add(line);
    }

    // ── Comandos del panel ────────────────────────────────────────────────────

    public void ToggleInfo()  { ShowInfo  = !ShowInfo;  RebuildVisible(); }
    public void ToggleWarn()  { ShowWarn  = !ShowWarn;  RebuildVisible(); }
    public void ToggleError() { ShowError = !ShowError; RebuildVisible(); }

    public void Clear()
    {
        _allEntries.Clear();
        _visibleEntries.Clear();
        Cleared?.Invoke();
    }

    public void CopyAll()
    {
        if (_visibleEntries.Count == 0) return;
        try { Clipboard.SetText(string.Join(Environment.NewLine, _visibleEntries)); }
        catch { /* ignorar errores de portapapeles */ }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RebuildVisible()
    {
        _visibleEntries.Clear();

        foreach (string entry in _allEntries)
        {
            bool isWarn  = entry.Contains("[WARN]", StringComparison.Ordinal);
            bool isError = entry.Contains("[ERR]",  StringComparison.Ordinal);

            bool show = (isError && ShowError)
                     || (isWarn  && ShowWarn)
                     || (!isError && !isWarn && ShowInfo);

            if (show) _visibleEntries.Add(entry);
        }

        VisibleEntriesRebuilt?.Invoke();
    }
}
