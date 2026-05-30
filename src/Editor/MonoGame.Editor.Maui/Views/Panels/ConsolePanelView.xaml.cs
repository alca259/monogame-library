using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Console del dock inferior. Muestra entradas de log y salida de build.
/// Fase 0: log plano con colores por nivel. Fase 6: filtros activos + límite de líneas.
/// </summary>
public sealed partial class ConsolePanelView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<string> _allEntries  = [];
    private readonly ObservableCollection<string> _visible = [];

    private bool _showInfo  = true;
    private bool _showWarn  = true;
    private bool _showError = true;

    private Action<LogEntryAddedEvent>?  _onLogEntry;
    private Action<BuildOutputLineEvent>? _onBuildOutput;

    private static readonly Color ActiveFilterFg   = Color.FromArgb("#d6d6d8");
    private static readonly Color InactiveFilterFg = Color.FromArgb("#646468");

    public ConsolePanelView()
    {
        InitializeComponent();
        LogList.ItemsSource = _visible;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onLogEntry    = e => MainThread.BeginInvokeOnMainThread(() => OnLogEntry(e));
        _onBuildOutput = e => MainThread.BeginInvokeOnMainThread(() => OnBuildOutput(e));
        _bus.Subscribe(_onLogEntry);
        _bus.Subscribe(_onBuildOutput);
    }

    private void Unsubscribe()
    {
        if (_onLogEntry is not null)    _bus.Unsubscribe(_onLogEntry);
        if (_onBuildOutput is not null) _bus.Unsubscribe(_onBuildOutput);
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
        _allEntries.Add(line);

        bool show = e.Entry.Level switch
        {
            LogLevel.Warning => _showWarn,
            LogLevel.Error   => _showError,
            _                => _showInfo
        };

        if (show) _visible.Add(line);
    }

    private void OnBuildOutput(BuildOutputLineEvent e)
    {
        string prefix = e.IsError ? "[ERR]  " : "[BLD]  ";
        string line = $"{DateTime.Now:HH:mm:ss} {prefix}{e.Line}";
        _allEntries.Add(line);
        if (_showInfo) _visible.Add(line);
    }

    private void OnFilterInfoClicked(object sender, EventArgs e)
    {
        _showInfo = !_showInfo;
        FilterInfoBtn.TextColor = _showInfo ? ActiveFilterFg : InactiveFilterFg;
        RebuildVisible();
    }

    private void OnFilterWarnClicked(object sender, EventArgs e)
    {
        _showWarn = !_showWarn;
        FilterWarnBtn.TextColor = _showWarn ? ActiveFilterFg : InactiveFilterFg;
        RebuildVisible();
    }

    private void OnFilterErrorClicked(object sender, EventArgs e)
    {
        _showError = !_showError;
        FilterErrorBtn.TextColor = _showError ? ActiveFilterFg : InactiveFilterFg;
        RebuildVisible();
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        _allEntries.Clear();
        _visible.Clear();
    }

    private void RebuildVisible()
    {
        _visible.Clear();
        foreach (string entry in _allEntries)
        {
            bool isBuild = entry.Contains("[BLD]");
            bool isWarn  = entry.Contains("[WARN]");
            bool isError = entry.Contains("[ERR]");

            bool show = (isError && _showError)
                     || (isWarn  && _showWarn)
                     || (!isError && !isWarn && _showInfo);

            if (show) _visible.Add(entry);
        }
    }
}
