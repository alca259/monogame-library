namespace MonoGame.Editor.Core.Assets;

/// <summary>
/// Vigila una carpeta de Content para detectar cambios en archivos y publica <see cref="AssetImportedEvent"/>
/// a través del bus de eventos del editor.
/// </summary>
public sealed class ContentWatcher : IDisposable
{
    private readonly IEditorEventBus _eventBus;
    private FileSystemWatcher? _watcher;
    private string _rootPath = string.Empty;
    private bool _disposed;

    /// <param name="eventBus">Bus utilizado para publicar <see cref="AssetImportedEvent"/>.</param>
    public ContentWatcher(IEditorEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _eventBus = eventBus;
    }

    /// <summary>Obtiene la ruta que se está vigilando actualmente, o una cadena vacía si está inactivo.</summary>
    public string RootPath => _rootPath;

    /// <summary>Comienza a vigilar <paramref name="contentPath"/>. Reemplaza cualquier vigilancia anterior.</summary>
    public void Watch(string contentPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        StopWatcher();

        if (!Directory.Exists(contentPath))
            return;

        _rootPath = contentPath;
        _watcher = new FileSystemWatcher(contentPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter         = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents  = true,
        };
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Renamed += OnFileRenamed;
    }

    /// <summary>Detiene la vigilancia sin desechar la instancia.</summary>
    public void Stop() => StopWatcher();

    private void StopWatcher()
    {
        if (_watcher is null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;
        _watcher.Renamed -= OnFileRenamed;
        _watcher.Dispose();
        _watcher   = null;
        _rootPath  = string.Empty;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        AssetInfo info = AssetClassifier.CreateInfo(e.FullPath, _rootPath);
        _eventBus.Publish(new AssetImportedEvent(info));
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        AssetInfo info = AssetClassifier.CreateInfo(e.FullPath, _rootPath);
        _eventBus.Publish(new AssetImportedEvent(info));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopWatcher();
    }
}
