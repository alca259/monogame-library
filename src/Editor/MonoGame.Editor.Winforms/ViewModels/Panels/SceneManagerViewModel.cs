using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>ViewModel de la pestaña Scenes del dock inferior.</summary>
public sealed partial class SceneManagerViewModel : ViewModelBase
{
    private string _scenesPath = string.Empty;
    private string _activeScenePath = string.Empty;
    private readonly List<SceneData> _scenes = [];

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Scenes;

    /// <summary>Lista estable de escenas del proyecto. Se actualiza in-place antes de disparar <see cref="RebuildRequested"/>.</summary>
    public IReadOnlyList<SceneData> Scenes => _scenes;

    [ObservableProperty] private bool _hasProject;
    [ObservableProperty] private string _sceneCountText = "0 scenes";
    [ObservableProperty] private string _activeSceneText = "No active scene";

    /// <summary>Señal para que el panel reconstruya la lista de escenas.</summary>
    public event Action? RebuildRequested;

    // ── Eventos del bus ───────────────────────────────────────────────────────

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
        On<SceneLoadedEvent>(OnSceneLoaded);
        On<SceneCreatedEvent>(OnSceneCreated);
        On<SceneDirtyChangedEvent>(OnSceneDirtyChanged);
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _scenes.Clear();
        _activeScenePath = string.Empty;
        ActiveSceneText = "No active scene";
        HasProject = e.Project is not null;

        if (e.Project is null)
        {
            _scenesPath = string.Empty;
            SceneCountText = "0 scenes";
            RebuildRequested?.Invoke();
            return;
        }

        _scenesPath = e.Project.ScenesPath;

        if (Directory.Exists(_scenesPath))
        {
            foreach (string file in Directory.GetFiles(_scenesPath, "*.scene.json")
                                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                _scenes.Add(new SceneData(file, false));
        }

        UpdateSceneCount();
        RebuildRequested?.Invoke();
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        ActiveSceneText = e.Scene is not null
            ? $"Active: {e.Scene.Name}"
            : "No active scene";

        if (e.Scene is null) { _activeScenePath = string.Empty; return; }

        for (int i = 0; i < _scenes.Count; i++)
        {
            if (_scenes[i].Name == e.Scene.Name)
            {
                _activeScenePath = _scenes[i].FilePath;
                return;
            }
        }
        _activeScenePath = string.Empty;
    }

    private void OnSceneCreated(SceneCreatedEvent e)
    {
        if (string.IsNullOrEmpty(_scenesPath)) return;

        string filePath = Path.Combine(_scenesPath, $"{e.Scene.Name}.scene.json");
        if (_scenes.Exists(s => s.FilePath == filePath)) return;

        _scenes.Add(new SceneData(filePath, false));
        UpdateSceneCount();
        RebuildRequested?.Invoke();
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent e)
    {
        if (string.IsNullOrEmpty(_activeScenePath)) return;

        for (int i = 0; i < _scenes.Count; i++)
        {
            if (_scenes[i].FilePath == _activeScenePath)
            {
                _scenes[i] = new SceneData(_scenes[i].FilePath, e.IsDirty);
                RebuildRequested?.Invoke();
                return;
            }
        }
    }

    // ── Métodos invocados por el panel ────────────────────────────────────────

    /// <summary>Carga la escena indicada como escena activa.</summary>
    public async Task LoadSceneAsync(SceneData data)
    {
        EditorScene? scene = await SceneSerializer.LoadAsync(data.FilePath).ConfigureAwait(true);
        if (scene is null) return;
        Context.SetActiveScene(scene);
    }

    /// <summary>Crea una nueva escena con el nombre dado y la persiste en disco.</summary>
    public async Task NewSceneAsync(string name)
    {
        if (string.IsNullOrEmpty(_scenesPath)) return;

        EditorScene scene = new() { Name = name };
        string filePath = Path.Combine(_scenesPath, $"{name}.scene.json");
        await SceneSerializer.SaveAsync(scene, filePath).ConfigureAwait(true);
        Bus.Publish(new SceneCreatedEvent(scene));
    }

    /// <summary>Renombra una escena moviendo su fichero. Actualiza la lista si tiene éxito.</summary>
    public void RenameScene(SceneData data, string newName)
    {
        string newPath = Path.Combine(_scenesPath, $"{newName}.scene.json");
        try { File.Move(data.FilePath, newPath); }
        catch (Exception ex)
        {
            Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, LogLevel.Error,
                $"[SceneManager] Failed to rename scene: {ex.Message}")));
            return;
        }

        for (int i = 0; i < _scenes.Count; i++)
        {
            if (_scenes[i].FilePath != data.FilePath) continue;
            _scenes[i] = new SceneData(newPath, _scenes[i].IsDirty);
            RebuildRequested?.Invoke();
            return;
        }
    }

    /// <summary>Elimina el fichero de escena y la quita de la lista.</summary>
    public void DeleteScene(SceneData data)
    {
        try { if (File.Exists(data.FilePath)) File.Delete(data.FilePath); }
        catch { return; }

        _scenes.Remove(data);
        UpdateSceneCount();
        RebuildRequested?.Invoke();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void UpdateSceneCount()
        => SceneCountText = _scenes.Count == 1 ? "1 scene" : $"{_scenes.Count} scenes";
}

/// <summary>Ítem inmutable de escena para el <see cref="SceneManagerPanel"/>.</summary>
public sealed record SceneData(string FilePath, bool IsDirty)
{
    /// <summary>Nombre de la escena (sin la extensión <c>.scene.json</c>).</summary>
    public string Name => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(FilePath));

    /// <summary>Nombre con indicador dirty <c>●</c> si hay cambios sin guardar.</summary>
    public string DisplayName => IsDirty ? $"{Name} ●" : Name;
}
