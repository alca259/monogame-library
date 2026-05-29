namespace MonoGame.Editor.Core;

/// <summary>
/// Fuente de verdad singleton del estado en tiempo de ejecución del editor: escena activa, selección y modo de reproducción.
/// Los paneles comunican cambios de estado exclusivamente a través de <see cref="EventBus"/>.
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

    /// <summary>Devuelve la instancia global del contexto del editor, creándola en el primer acceso.</summary>
    public static EditorContext Instance
    {
        get
        {
            lock (_instanceLock)
                return _instance ??= new EditorContext(new EditorEventBus());
        }
    }

    /// <summary>Reinicia el singleton (solo para pruebas unitarias).</summary>
    internal static void Reset()
    {
        lock (_instanceLock)
            _instance = null;
    }

    #endregion

    /// <summary>Inicializa con un bus de eventos personalizado (usado en tests o contenedores DI).</summary>
    public EditorContext(IEditorEventBus eventBus)
    {
        EventBus = eventBus;
        Commands = new CommandStack(100, eventBus);
        _logger  = new InternalEditorLogger(eventBus);
    }

    #region Properties

    /// <summary>El bus de eventos compartido para este contexto.</summary>
    public IEditorEventBus EventBus { get; }

    /// <summary>Historial de deshacer/rehacer para todas las operaciones del editor.</summary>
    public CommandStack Commands { get; }

    /// <summary>El logger centralizado del editor. Publica <see cref="LogEntryAddedEvent"/> a través de <see cref="EventBus"/>.</summary>
    public IEditorLogger Logger => _logger;

    /// <summary>Estado actual del editor (Editing o Playing).</summary>
    public EditorState State { get { lock (_stateLock) return _state; } }

    /// <summary>Escena cargada actualmente, o <c>null</c> si no hay ninguna escena abierta.</summary>
    public EditorScene? ActiveScene { get { lock (_stateLock) return _activeScene; } }

    /// <summary>Objeto seleccionado principal, o <c>null</c> si no hay nada seleccionado.</summary>
    public EditorGameObject? SelectedObject { get { lock (_stateLock) return _selectedObject; } }

    /// <summary>Todos los objetos seleccionados actualmente (selección simple o múltiple).</summary>
    public IReadOnlyList<EditorGameObject> MultiSelection
    {
        get { lock (_stateLock) return _multiSelection.ToArray(); }
    }

    /// <summary>Proyecto de juego activo, o <c>null</c> si no hay ningún proyecto abierto.</summary>
    public EditorProject? ActiveProject { get { lock (_stateLock) return _activeProject; } }

    /// <summary>Indica si la escena activa tiene cambios sin guardar.</summary>
    public bool IsSceneDirty { get { lock (_stateLock) return _isSceneDirty; } }

    #endregion

    #region State mutations

    /// <summary>Transiciona el editor al estado <paramref name="state"/> y publica <see cref="EditorStateChangedEvent"/>.</summary>
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

    /// <summary>Establece el objeto seleccionado único y publica <see cref="GameObjectSelectedEvent"/>.</summary>
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
    /// Establece una selección de múltiples objetos. El primer elemento se convierte en <see cref="SelectedObject"/>.
    /// Publica <see cref="GameObjectSelectedEvent"/> con el primer objeto (o <c>null</c>).
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

    /// <summary>Establece la escena activa y publica <see cref="SceneLoadedEvent"/>. Restablece el estado de modificación.</summary>
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

    /// <summary>Marca la escena activa como que tiene cambios sin guardar y publica <see cref="SceneDirtyChangedEvent"/>.</summary>
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

    /// <summary>Elimina la marca de cambios y publica <see cref="SceneDirtyChangedEvent"/>.</summary>
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

    /// <summary>Establece el proyecto activo y publica <see cref="ProjectOpenedEvent"/>.</summary>
    public void SetActiveProject(EditorProject? project)
    {
        lock (_stateLock)
            _activeProject = project;

        EventBus.Publish(new ProjectOpenedEvent(project));
    }

    /// <summary>Serializa la escena activa en una instantánea JSON en memoria para restaurar el modo de reproducción.</summary>
    public void TakePlaySnapshot()
    {
        lock (_stateLock)
            _playSnapshot = _activeScene is null ? null : SceneSerializer.Serialize(_activeScene);
    }

    /// <summary>Deserializa y devuelve la instantánea de reproducción almacenada, o <c>null</c> si no existe ninguna.</summary>
    public EditorScene? RestoreFromSnapshot()
    {
        lock (_stateLock)
            return _playSnapshot is null ? null : SceneSerializer.Deserialize(_playSnapshot);
    }

    /// <summary>Elimina la instantánea de reproducción almacenada.</summary>
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
