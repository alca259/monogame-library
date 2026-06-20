namespace MonoGame.Editor.Winforms.ViewModels.Panels;

/// <summary>ViewModel del panel Assets del dock inferior: árbol de carpetas y lista de assets.</summary>
public sealed class AssetBrowserViewModel : ViewModelBase
{
    private readonly HashSet<string> _expandedFolders = [];
    private string _contentRoot     = string.Empty;
    private string _currentFolder   = string.Empty;
    private string _filterText      = string.Empty;
    private bool   _canManage;

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Assets;

    public string ContentRoot     => _contentRoot;
    public string CurrentFolder   => _currentFolder;
    public bool   CanManage       => _canManage;

    public string FilterText
    {
        get => _filterText;
        set { _filterText = value; AssetListRebuildRequested?.Invoke(); }
    }

    /// <summary>Señal para que el panel reconstruya el TreeView de carpetas.</summary>
    public event Action? FolderTreeRebuildRequested;

    /// <summary>Señal para que el panel reconstruya el ListView de assets.</summary>
    public event Action? AssetListRebuildRequested;

    // ── Eventos del bus ───────────────────────────────────────────────────────

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
        On<AssetImportedEvent>(_ => AssetListRebuildRequested?.Invoke());
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            OnProjectOpened(new ProjectOpenedEvent(project));
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _expandedFolders.Clear();
        _canManage = false;

        if (e.Project is null)
        {
            _contentRoot   = string.Empty;
            _currentFolder = string.Empty;
            FolderTreeRebuildRequested?.Invoke();
            AssetListRebuildRequested?.Invoke();
            return;
        }

        _contentRoot   = e.Project.ContentPath;
        _currentFolder = _contentRoot;
        _expandedFolders.Add(_contentRoot);
        _canManage = true;

        FolderTreeRebuildRequested?.Invoke();
        AssetListRebuildRequested?.Invoke();
    }

    // ── Datos del árbol de carpetas ────────────────────────────────────────────

    /// <summary>Devuelve las entradas del árbol de carpetas en orden DFS.</summary>
    public IReadOnlyList<FolderEntry> GetFolderEntries()
    {
        List<FolderEntry> result = [];
        if (!Directory.Exists(_contentRoot)) return result;
        CollectEntries(_contentRoot, 0, result);
        return result;
    }

    private void CollectEntries(string dir, int depth, List<FolderEntry> result)
    {
        if (!Directory.Exists(dir)) return;

        string[] subdirs     = Directory.GetDirectories(dir);
        bool     hasChildren = subdirs.Length > 0;
        bool     isExpanded  = _expandedFolders.Contains(dir);
        bool     isRoot      = string.Equals(dir, _contentRoot, StringComparison.OrdinalIgnoreCase);

        result.Add(new FolderEntry(dir, depth, hasChildren, isExpanded, isRoot));

        if (isExpanded)
        {
            foreach (string sub in subdirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                CollectEntries(sub, depth + 1, result);
        }
    }

    // ── Datos de la lista de assets ────────────────────────────────────────────

    /// <summary>Devuelve los assets de la carpeta actual que pasan el filtro.</summary>
    public IReadOnlyList<AssetInfo> GetAssetItems()
    {
        List<AssetInfo> result = [];
        if (!Directory.Exists(_currentFolder)) return result;

        foreach (string file in Directory.GetFiles(_currentFolder)
                                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            AssetInfo info = AssetClassifier.CreateInfo(file, _contentRoot);
            if (!string.IsNullOrEmpty(_filterText) &&
                !info.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(info);
        }

        return result;
    }

    // ── Navegación ────────────────────────────────────────────────────────────

    /// <summary>Selecciona una carpeta como carpeta actual y recarga la lista de assets.</summary>
    public void SelectFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        _currentFolder = path;
        AssetListRebuildRequested?.Invoke();
    }

    /// <summary>Alterna el estado expandido de una carpeta y reconstruye el árbol.</summary>
    public void ToggleFolderExpansion(string path)
    {
        if (_expandedFolders.Contains(path))
            _expandedFolders.Remove(path);
        else
        {
            _expandedFolders.Add(path);
            _currentFolder = path;
        }

        FolderTreeRebuildRequested?.Invoke();
        AssetListRebuildRequested?.Invoke();
    }

    // ── Operaciones de assets ─────────────────────────────────────────────────

    /// <summary>Importa un fichero copiándolo a la carpeta actual.</summary>
    public void ImportAsset(string sourcePath)
    {
        if (string.IsNullOrEmpty(_currentFolder)) return;

        string dest = Path.Combine(_currentFolder, Path.GetFileName(sourcePath));
        try { File.Copy(sourcePath, dest, overwrite: false); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to import asset: {ex.Message}", LogLevel.Error); return; }

        Bus.Publish(new AssetImportedEvent(AssetClassifier.CreateInfo(dest, _contentRoot)));
    }

    /// <summary>Renombra un asset. El panel ya debe haber pedido confirmación.</summary>
    public void RenameAsset(AssetInfo info, string newName)
    {
        string newPath = Path.Combine(_currentFolder, newName);
        try { File.Move(info.AbsolutePath, newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to rename asset: {ex.Message}", LogLevel.Error); return; }
        AssetListRebuildRequested?.Invoke();
    }

    /// <summary>Elimina un asset. El panel ya debe haber pedido confirmación.</summary>
    public void DeleteAsset(AssetInfo info)
    {
        try { File.Delete(info.AbsolutePath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to delete asset: {ex.Message}", LogLevel.Error); return; }
        AssetListRebuildRequested?.Invoke();
    }

    // ── Operaciones de carpetas ───────────────────────────────────────────────

    /// <summary>Crea una subcarpeta en la carpeta actual.</summary>
    public void NewFolder(string name)
    {
        if (string.IsNullOrEmpty(_currentFolder)) return;

        string newPath = Path.Combine(_currentFolder, name);
        try { Directory.CreateDirectory(newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to create folder: {ex.Message}", LogLevel.Error); return; }

        _expandedFolders.Add(_currentFolder);
        FolderTreeRebuildRequested?.Invoke();
    }

    /// <summary>Renombra una carpeta. El panel ya debe haber pedido confirmación.</summary>
    public void RenameFolder(string folderPath, string newName)
    {
        string parent  = Path.GetDirectoryName(folderPath) ?? _contentRoot;
        string newPath = Path.Combine(parent, newName);

        try { Directory.Move(folderPath, newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to rename folder: {ex.Message}", LogLevel.Error); return; }

        if (_currentFolder.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolder = _currentFolder.Replace(folderPath, newPath, StringComparison.OrdinalIgnoreCase);

        _expandedFolders.Remove(folderPath);
        _expandedFolders.Add(newPath);

        FolderTreeRebuildRequested?.Invoke();
        AssetListRebuildRequested?.Invoke();
    }

    /// <summary>Elimina una carpeta y su contenido recursivamente. El panel ya debe haber pedido confirmación.</summary>
    public void DeleteFolder(string folderPath)
    {
        try { Directory.Delete(folderPath, recursive: true); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to delete folder: {ex.Message}", LogLevel.Error); return; }

        if (_currentFolder.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolder = _contentRoot;

        _expandedFolders.Remove(folderPath);

        FolderTreeRebuildRequested?.Invoke();
        AssetListRebuildRequested?.Invoke();
    }

    /// <summary>Crea un fichero de asset con el contenido por defecto indicado.</summary>
    public void CreateAssetFile(string suffix, string defaultContent, string name)
    {
        if (string.IsNullOrEmpty(_currentFolder)) return;

        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            name = name[..^suffix.Length];

        string filePath = Path.Combine(_currentFolder, name + suffix);
        try
        {
            File.WriteAllText(filePath, defaultContent, System.Text.Encoding.UTF8);
            Bus.Publish(new AssetImportedEvent(AssetClassifier.CreateInfo(filePath, _contentRoot)));
        }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to create asset: {ex.Message}", LogLevel.Error); }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}

/// <summary>Entrada de carpeta para el árbol de carpetas del <see cref="AssetBrowserPanel"/>.</summary>
public sealed record FolderEntry(string Path, int Depth, bool HasChildren, bool IsExpanded, bool IsRoot);
