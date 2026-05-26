using MonoGame.Editor.Core.Events;

namespace MonoGame.Editor.Core.Assets;

/// <summary>
/// Watches a Content folder for file changes and publishes <see cref="AssetImportedEvent"/>
/// through the editor event bus.
/// </summary>
public sealed class ContentWatcher : IDisposable
{
    private readonly IEditorEventBus _eventBus;
    private FileSystemWatcher? _watcher;
    private string _rootPath = string.Empty;
    private bool _disposed;

    /// <param name="eventBus">Bus used to publish <see cref="AssetImportedEvent"/>.</param>
    public ContentWatcher(IEditorEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        _eventBus = eventBus;
    }

    /// <summary>Gets the path currently being watched, or an empty string if inactive.</summary>
    public string RootPath => _rootPath;

    /// <summary>Starts watching <paramref name="contentPath"/>. Replaces any previous watch.</summary>
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

    /// <summary>Stops watching without disposing the instance.</summary>
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
