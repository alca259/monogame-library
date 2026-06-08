using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Console del dock. Acumula entradas de log y salida de
/// build (con límite de líneas) y expone la lista filtrada por nivel. El scroll al
/// final es responsabilidad de la vista (depende del <c>CollectionView</c>).
/// </summary>
public sealed partial class ConsolePanelViewModel : ViewModelBase
{
    private const int MaxLogEntries = 1000;

    private readonly List<string> _allEntries = [];

    /// <summary>Entradas actualmente visibles según los filtros activos.</summary>
    public ObservableCollection<string> VisibleEntries { get; } = [];

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarn = true;

    [ObservableProperty]
    private bool _showError = true;

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

        if (show) VisibleEntries.Add(line);
    }

    private void OnBuildOutput(BuildOutputLineEvent e)
    {
        string prefix = e.IsError ? "[ERR]  " : "[BLD]  ";
        string line = $"{DateTime.Now:HH:mm:ss} {prefix}{e.Line}";
        Append(line);
        if (ShowInfo) VisibleEntries.Add(line);
    }

    private void Append(string line)
    {
        if (_allEntries.Count >= MaxLogEntries) _allEntries.RemoveAt(0);
        _allEntries.Add(line);
    }

    [RelayCommand]
    private void ToggleInfo()
    {
        ShowInfo = !ShowInfo;
        RebuildVisible();
    }

    [RelayCommand]
    private void ToggleWarn()
    {
        ShowWarn = !ShowWarn;
        RebuildVisible();
    }

    [RelayCommand]
    private void ToggleError()
    {
        ShowError = !ShowError;
        RebuildVisible();
    }

    [RelayCommand]
    private void Clear()
    {
        _allEntries.Clear();
        VisibleEntries.Clear();
    }

    [RelayCommand]
    private async Task CopyAllAsync()
    {
        if (VisibleEntries.Count == 0) return;
        await Clipboard.SetTextAsync(string.Join(Environment.NewLine, VisibleEntries)).ConfigureAwait(false);
    }

    private void RebuildVisible()
    {
        VisibleEntries.Clear();
        foreach (string entry in _allEntries)
        {
            bool isWarn  = entry.Contains("[WARN]");
            bool isError = entry.Contains("[ERR]");

            bool show = (isError && ShowError)
                     || (isWarn  && ShowWarn)
                     || (!isError && !isWarn && ShowInfo);

            if (show) VisibleEntries.Add(entry);
        }
    }
}
