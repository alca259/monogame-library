using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Scenes del dock inferior. Lista las escenas del proyecto; doble clic para cargar;
/// indicador ● de dirty; botones Nueva / Eliminar.
/// </summary>
public sealed partial class SceneManagerView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<SceneItem> _items = [];

    private string _scenesPath      = string.Empty;
    private string _activeScenePath = string.Empty;

    private Action<ProjectOpenedEvent>?      _onProjectOpened;
    private Action<SceneLoadedEvent>?        _onSceneLoaded;
    private Action<SceneCreatedEvent>?       _onSceneCreated;
    private Action<SceneDirtyChangedEvent>?  _onSceneDirty;

    public SceneManagerView()
    {
        InitializeComponent();
        SceneList.ItemsSource = _items;
        SceneList.SelectionChanged += (_, _) => UpdateSelectionButtonStates();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    // ── EventBus ─────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _onSceneLoaded   = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onSceneCreated  = e => MainThread.BeginInvokeOnMainThread(() => OnSceneCreated(e));
        _onSceneDirty    = e => MainThread.BeginInvokeOnMainThread(() => OnSceneDirtyChanged(e));
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onSceneCreated);
        _bus.Subscribe(_onSceneDirty);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
        if (_onSceneLoaded   is not null) _bus.Unsubscribe(_onSceneLoaded);
        if (_onSceneCreated  is not null) _bus.Unsubscribe(_onSceneCreated);
        if (_onSceneDirty    is not null) _bus.Unsubscribe(_onSceneDirty);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _items.Clear();
        _activeScenePath  = string.Empty;
        ActiveSceneLabel.Text = "No active scene";

        bool hasProject = e.Project is not null;
        NewSceneBtn.IsEnabled = hasProject;
        UpdateSelectionButtonStates();

        if (e.Project is null)
        {
            _scenesPath = string.Empty;
            SceneCountLabel.Text = "0 scenes";
            return;
        }

        _scenesPath = e.Project.ScenesPath;

        if (!Directory.Exists(_scenesPath))
        {
            SceneCountLabel.Text = "0 scenes";
            return;
        }

        foreach (string file in Directory.GetFiles(_scenesPath, "*.scene.json")
                                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            _items.Add(new SceneItem(file, LoadSceneAsync));

        UpdateSceneCount();
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        ActiveSceneLabel.Text = e.Scene is not null
            ? $"Active: {e.Scene.Name}"
            : "No active scene";

        if (e.Scene is null)
        {
            _activeScenePath = string.Empty;
            return;
        }

        SceneItem? match = null;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Name == e.Scene.Name)
            {
                match = _items[i];
                break;
            }
        }
        _activeScenePath = match?.FilePath ?? string.Empty;
    }

    private void OnSceneCreated(SceneCreatedEvent e)
    {
        if (string.IsNullOrEmpty(_scenesPath)) return;

        string filePath = Path.Combine(_scenesPath, $"{e.Scene.Name}.scene.json");
        if (_items.Any(i => i.FilePath == filePath)) return;

        _items.Add(new SceneItem(filePath, LoadSceneAsync));
        UpdateSceneCount();
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent e)
    {
        if (string.IsNullOrEmpty(_activeScenePath)) return;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].FilePath == _activeScenePath)
            {
                _items[i] = new SceneItem(_items[i].FilePath, LoadSceneAsync, e.IsDirty);
                return;
            }
        }
    }

    // ── Load scene ────────────────────────────────────────────────────────────

    private async Task LoadSceneAsync(SceneItem item)
    {
        EditorScene? scene = await SceneSerializer.LoadAsync(item.FilePath).ConfigureAwait(false);
        if (scene is null) return;
        await MainThread.InvokeOnMainThreadAsync(() => EditorContext.Instance.SetActiveScene(scene))
                        .ConfigureAwait(false);
    }

    // ── Toolbar buttons ───────────────────────────────────────────────────────

    private async void OnNewSceneClicked(object sender, EventArgs e)
    {
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        NewSceneResult? result = await NewSceneDialog.ShowAsync(page.Navigation);
        if (result is null) return;

        if (string.IsNullOrEmpty(_scenesPath)) return;

        EditorScene scene = new()
        {
            Name      = result.SceneName,
            WorldSize = new EditorVector2(result.WorldWidth, result.WorldHeight),
        };

        string filePath = Path.Combine(_scenesPath, $"{result.SceneName}.scene.json");
        await SceneSerializer.SaveAsync(scene, filePath).ConfigureAwait(true);

        _bus.Publish(new SceneCreatedEvent(scene));
    }

    private async void OnRenameSceneClicked(object sender, EventArgs e)
    {
        if (SceneList.SelectedItem is not SceneItem item) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? newName = await page.DisplayPromptAsync(
            "Rename scene",
            "Enter new name:",
            initialValue: item.Name,
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

        string newPath = Path.Combine(_scenesPath, $"{newName}.scene.json");
        try { File.Move(item.FilePath, newPath); }
        catch (Exception ex) { Log($"[SceneManager] Failed to rename scene: {ex.Message}", LogLevel.Error); return; }

        int idx = -1;
        for (int i = 0; i < _items.Count; i++)
            if (_items[i].FilePath == item.FilePath) { idx = i; break; }

        if (idx >= 0)
            _items[idx] = new SceneItem(newPath, LoadSceneAsync);
    }

    private async void OnDeleteSceneClicked(object sender, EventArgs e)
    {
        if (SceneList.SelectedItem is not SceneItem item) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        bool confirmed = await page.DisplayAlertAsync(
            "Delete scene",
            $"Delete '{item.Name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            if (File.Exists(item.FilePath))
                File.Delete(item.FilePath);
        }
        catch
        {
            return;
        }

        _items.Remove(item);
        UpdateSceneCount();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void UpdateSelectionButtonStates()
    {
        bool hasProject   = EditorContext.Instance.ActiveProject is not null;
        bool hasSelection = SceneList.SelectedItem is not null;
        SceneRenameBtn.IsEnabled = hasProject && hasSelection;
        DeleteSceneBtn.IsEnabled = hasProject && hasSelection;
    }

    private void UpdateSceneCount()
    {
        int count = _items.Count;
        SceneCountLabel.Text = count == 1 ? "1 scene" : $"{count} scenes";
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private void Log(string message, LogLevel level = LogLevel.Info)
        => _bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}
