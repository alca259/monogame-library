namespace MonoGame.Editor.Core;

/// <summary>
/// Singleton source of truth for the editor's runtime state: active scene, selection, and play mode.
/// Panels communicate state changes exclusively through <see cref="EventBus"/>.
/// </summary>
public sealed class EditorContext
{
    private static EditorContext? _instance;
    private static readonly Lock _instanceLock = new();

    private readonly Lock _stateLock = new();
    private EditorState _state = EditorState.Editing;
    private EditorScene? _activeScene;
    private EditorGameObject? _selectedObject;
    private readonly List<EditorGameObject> _multiSelection = [];
    private EditorProject? _activeProject;
    private readonly InternalEditorLogger _logger;
    private bool _isSceneDirty;
    private string? _playSnapshot;

    #region Singleton

    /// <summary>Returns the global editor context instance, creating it on first access.</summary>
    public static EditorContext Instance
    {
        get
        {
            lock (_instanceLock)
                return _instance ??= new EditorContext(new EditorEventBus());
        }
    }

    /// <summary>Resets the singleton (for unit testing only).</summary>
    internal static void Reset()
    {
        lock (_instanceLock)
            _instance = null;
    }

    #endregion

    /// <summary>Initializes with a custom event bus (used in tests or DI containers).</summary>
    public EditorContext(IEditorEventBus eventBus)
    {
        EventBus = eventBus;
        Commands = new CommandStack(100, eventBus);
        _logger  = new InternalEditorLogger(eventBus);
    }

    #region Properties

    /// <summary>The shared event bus for this context.</summary>
    public IEditorEventBus EventBus { get; }

    /// <summary>Undo/redo history for all editor operations.</summary>
    public CommandStack Commands { get; }

    /// <summary>The editor's centralized logger. Publishes <see cref="LogEntryAddedEvent"/> via <see cref="EventBus"/>.</summary>
    public IEditorLogger Logger => _logger;

    /// <summary>Current editor state (Editing, Playing, or Paused).</summary>
    public EditorState State { get { lock (_stateLock) return _state; } }

    /// <summary>Currently loaded scene, or <c>null</c> if no scene is open.</summary>
    public EditorScene? ActiveScene { get { lock (_stateLock) return _activeScene; } }

    /// <summary>Primary selected object, or <c>null</c> if nothing is selected.</summary>
    public EditorGameObject? SelectedObject { get { lock (_stateLock) return _selectedObject; } }

    /// <summary>All currently selected objects (single or multi-selection).</summary>
    public IReadOnlyList<EditorGameObject> MultiSelection
    {
        get { lock (_stateLock) return _multiSelection.ToArray(); }
    }

    /// <summary>Active game project, or <c>null</c> if no project is open.</summary>
    public EditorProject? ActiveProject { get { lock (_stateLock) return _activeProject; } }

    /// <summary>Whether the active scene has unsaved changes.</summary>
    public bool IsSceneDirty { get { lock (_stateLock) return _isSceneDirty; } }

    #endregion

    #region State mutations

    /// <summary>Transitions the editor to <paramref name="state"/> and publishes <see cref="EditorStateChangedEvent"/>.</summary>
    public void SetState(EditorState state)
    {
        EditorState old;
        lock (_stateLock)
        {
            old = _state;
            _state = state;
        }

        EventBus.Publish(new EditorStateChangedEvent(old, state));
    }

    /// <summary>Sets the single selected object and publishes <see cref="GameObjectSelectedEvent"/>.</summary>
    public void SetSelection(EditorGameObject? obj)
    {
        lock (_stateLock)
        {
            _selectedObject = obj;
            _multiSelection.Clear();
            if (obj is not null)
                _multiSelection.Add(obj);
        }

        EventBus.Publish(new GameObjectSelectedEvent(obj));
    }

    /// <summary>
    /// Sets a multi-object selection. The first item becomes <see cref="SelectedObject"/>.
    /// Publishes <see cref="GameObjectSelectedEvent"/> with the first (or <c>null</c>) object.
    /// </summary>
    public void SetMultiSelection(IEnumerable<EditorGameObject> objects)
    {
        EditorGameObject? first;
        lock (_stateLock)
        {
            _multiSelection.Clear();
            _multiSelection.AddRange(objects);
            first = _multiSelection.Count > 0 ? _multiSelection[0] : null;
            _selectedObject = first;
        }

        EventBus.Publish(new GameObjectSelectedEvent(first));
    }

    /// <summary>Sets the active scene and publishes <see cref="SceneLoadedEvent"/>. Resets dirty state.</summary>
    public void SetActiveScene(EditorScene? scene)
    {
        lock (_stateLock)
        {
            _activeScene = scene;
            _isSceneDirty = false;
        }

        EventBus.Publish(new SceneLoadedEvent(scene));
        EventBus.Publish(new SceneDirtyChangedEvent(false));
    }

    /// <summary>Marks the active scene as having unsaved changes and publishes <see cref="SceneDirtyChangedEvent"/>.</summary>
    public void MarkSceneDirty()
    {
        bool changed;
        lock (_stateLock)
        {
            changed = !_isSceneDirty;
            _isSceneDirty = true;
        }

        if (changed)
            EventBus.Publish(new SceneDirtyChangedEvent(true));
    }

    /// <summary>Clears the dirty flag and publishes <see cref="SceneDirtyChangedEvent"/>.</summary>
    public void MarkSceneClean()
    {
        bool changed;
        lock (_stateLock)
        {
            changed = _isSceneDirty;
            _isSceneDirty = false;
        }

        if (changed)
            EventBus.Publish(new SceneDirtyChangedEvent(false));
    }

    /// <summary>Sets the active project and publishes <see cref="ProjectOpenedEvent"/>.</summary>
    public void SetActiveProject(EditorProject? project)
    {
        lock (_stateLock)
            _activeProject = project;

        EventBus.Publish(new ProjectOpenedEvent(project));
    }

    /// <summary>Serializes the active scene to an in-memory JSON snapshot for Play mode restore.</summary>
    public void TakePlaySnapshot()
    {
        lock (_stateLock)
            _playSnapshot = _activeScene is null ? null : SceneSerializer.Serialize(_activeScene);
    }

    /// <summary>Deserializes and returns the stored play snapshot, or <c>null</c> if none exists.</summary>
    public EditorScene? RestoreFromSnapshot()
    {
        lock (_stateLock)
            return _playSnapshot is null ? null : SceneSerializer.Deserialize(_playSnapshot);
    }

    /// <summary>Clears the stored play snapshot.</summary>
    public void ClearPlaySnapshot()
    {
        lock (_stateLock)
            _playSnapshot = null;
    }

    #endregion

    #region Internal logger

    private sealed class InternalEditorLogger : IEditorLogger
    {
        private readonly IEditorEventBus _bus;

        internal InternalEditorLogger(IEditorEventBus bus) => _bus = bus;

        public void Log(string message, LogLevel level = LogLevel.Info)
            => _bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.Now, level, message)));

        public void LogWarning(string message) => Log(message, LogLevel.Warning);
        public void LogError(string message)   => Log(message, LogLevel.Error);
        public void LogDebug(string message)   => Log(message, LogLevel.Debug);
    }

    #endregion
}
