using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Panels;
using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Assets del dock: árbol de carpetas + lista de assets con
/// filtro. El breadcrumb (UI dinámica) lo construye la vista escuchando
/// <see cref="FolderChanged"/>; la navegación se hace vía <see cref="NavigateToFolderCommand"/>.
/// </summary>
public sealed partial class AssetBrowserViewModel : ViewModelBase
{
    private readonly HashSet<string> _expandedFolders = [];

    protected override EditorFocusContext? FocusContext => EditorFocusContext.Assets;

    private string _contentRoot = string.Empty;
    private string _selectedFolderPath = string.Empty;

    public ObservableCollection<FolderItem> FolderItems { get; } = [];
    public ObservableCollection<AssetItem> AssetItems { get; } = [];

    /// <summary>Raíz <c>Content/</c> del proyecto (para el breadcrumb de la vista).</summary>
    public string ContentRoot => _contentRoot;

    /// <summary>Carpeta actualmente mostrada (para el breadcrumb de la vista).</summary>
    public string CurrentFolderPath { get; private set; } = string.Empty;

    /// <summary>Notifica a la vista que debe reconstruir el breadcrumb.</summary>
    public event Action? FolderChanged;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private AssetItem? _selectedAsset;

    [ObservableProperty]
    private string _assetCountText = "0 assets";

    [ObservableProperty]
    private string _assetPathText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewFolderCommand))]
    private bool _canManage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FolderRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(FolderDeleteCommand))]
    private bool _canModifyFolder;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AssetRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(AssetDeleteCommand))]
    private bool _canModifyAsset;

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
        On<AssetImportedEvent>(_ => LoadAssetsFromFolder());
    }

    protected override void OnAttached()
    {
        if (Context.ActiveProject is { } project)
            OnProjectOpened(new ProjectOpenedEvent(project));
    }

    partial void OnFilterTextChanged(string value) => LoadAssetsFromFolder();

    partial void OnSelectedAssetChanged(AssetItem? value)
    {
        if (value is null)
        {
            AssetPathText = string.Empty;
            CanModifyAsset = false;
            return;
        }

        AssetPathText = value.Info.RelativePath;
        CanModifyAsset = true;
        Bus.Publish(new AssetSelectedEvent(value.Info));
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        FolderItems.Clear();
        AssetItems.Clear();
        _expandedFolders.Clear();
        AssetPathText = string.Empty;
        AssetCountText = "0 assets";
        SelectedAsset = null;
        _selectedFolderPath = string.Empty;
        CanModifyAsset = false;
        CanModifyFolder = false;

        if (e.Project is null)
        {
            _contentRoot = string.Empty;
            CurrentFolderPath = string.Empty;
            CanManage = false;
            FolderChanged?.Invoke();
            return;
        }

        _contentRoot = e.Project.ContentPath;
        CurrentFolderPath = _contentRoot;
        _expandedFolders.Add(_contentRoot);
        CanManage = true;

        BuildFolderTree();
        FolderChanged?.Invoke();
        LoadAssetsFromFolder();
    }

    // ── Folder tree ───────────────────────────────────────────────────────────

    private void BuildFolderTree()
    {
        FolderItems.Clear();
        if (!Directory.Exists(_contentRoot)) return;
        FlattenFolders(_contentRoot, 0);
    }

    private void FlattenFolders(string dir, int depth)
    {
        if (!Directory.Exists(dir)) return;

        string[] subdirs = Directory.GetDirectories(dir);
        bool hasChildren = subdirs.Length > 0;
        bool isExpanded = _expandedFolders.Contains(dir);
        bool isRoot = string.Equals(dir, _contentRoot, StringComparison.OrdinalIgnoreCase);

        FolderItem item = null!;
        item = new FolderItem(dir, depth, hasChildren, isExpanded, isRoot,
            onToggle: () =>
            {
                if (item.IsExpanded) _expandedFolders.Add(dir);
                else _expandedFolders.Remove(dir);

                BuildFolderTree();

                if (item.IsExpanded)
                {
                    CurrentFolderPath = dir;
                    FolderChanged?.Invoke();
                    LoadAssetsFromFolder();
                }
            },
            onRename: () => { _selectedFolderPath = dir; CanModifyFolder = !isRoot; _ = RenameFolderAsync(); },
            onDelete: () => { _selectedFolderPath = dir; CanModifyFolder = !isRoot; _ = DeleteFolderAsync(); });

        FolderItems.Add(item);

        if (isExpanded)
        {
            foreach (string sub in subdirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                FlattenFolders(sub, depth + 1);
        }
    }

    // ── Asset list ────────────────────────────────────────────────────────────

    private void LoadAssetsFromFolder()
    {
        AssetItems.Clear();
        if (!Directory.Exists(CurrentFolderPath)) return;

        int count = 0;
        foreach (string file in Directory.GetFiles(CurrentFolderPath)
                                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            AssetInfo info = AssetClassifier.CreateInfo(file, _contentRoot);
            if (!string.IsNullOrEmpty(FilterText) &&
                !info.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                continue;

            AssetItems.Add(new AssetItem(info));
            count++;
        }

        AssetCountText = count == 1 ? "1 asset" : $"{count} assets";
    }

    // ── Navigation (breadcrumb) ─────────────────────────────────────────────────

    [RelayCommand]
    private void NavigateToFolder(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        CurrentFolderPath = path;

        string current = path;
        while (!string.IsNullOrEmpty(current) &&
               current.StartsWith(_contentRoot, StringComparison.OrdinalIgnoreCase))
        {
            _expandedFolders.Add(current);
            string parent = Path.GetDirectoryName(current) ?? string.Empty;
            if (string.IsNullOrEmpty(parent) || parent == current) break;
            current = parent;
        }

        BuildFolderTree();
        FolderChanged?.Invoke();
        LoadAssetsFromFolder();
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportAssetAsync()
    {
        if (string.IsNullOrEmpty(CurrentFolderPath)) return;

        string? pickedPath = await DialogService.PickFileAsync();
        if (pickedPath is null) return;

        string dest = Path.Combine(CurrentFolderPath, Path.GetFileName(pickedPath));
        try { File.Copy(pickedPath, dest, overwrite: false); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to import asset: {ex.Message}", LogLevel.Error); return; }

        AssetInfo info = AssetClassifier.CreateInfo(dest, _contentRoot);
        Bus.Publish(new AssetImportedEvent(info));
    }

    [RelayCommand(CanExecute = nameof(CanModifyAsset))]
    private async Task AssetRenameAsync()
    {
        if (SelectedAsset is not { } item) return;

        string? newName = await DialogService.PromptAsync(
            "Rename asset", "Enter new file name:", initialValue: item.Info.Name, maxLength: 256);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Info.Name) return;

        string newPath = Path.Combine(CurrentFolderPath, newName);
        try { File.Move(item.Info.AbsolutePath, newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to rename asset: {ex.Message}", LogLevel.Error); return; }

        LoadAssetsFromFolder();
    }

    [RelayCommand(CanExecute = nameof(CanModifyAsset))]
    private async Task AssetDeleteAsync()
    {
        if (SelectedAsset is not { } item) return;

        bool confirmed = await DialogService.ConfirmAsync(
            "Delete asset", $"Delete '{item.Info.Name}'? This cannot be undone.", "Delete", "Cancel");

        if (!confirmed) return;

        try { File.Delete(item.Info.AbsolutePath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to delete asset: {ex.Message}", LogLevel.Error); return; }

        LoadAssetsFromFolder();
    }

    [RelayCommand(CanExecute = nameof(CanManage))]
    private async Task NewFolderAsync()
    {
        if (string.IsNullOrEmpty(CurrentFolderPath)) return;

        string? name = await DialogService.PromptAsync("New folder", "Enter folder name:", maxLength: 128);
        if (string.IsNullOrWhiteSpace(name)) return;

        string newPath = Path.Combine(CurrentFolderPath, name);
        try { Directory.CreateDirectory(newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to create folder: {ex.Message}", LogLevel.Error); return; }

        _expandedFolders.Add(CurrentFolderPath);
        BuildFolderTree();
        LoadAssetsFromFolder();
    }

    [RelayCommand(CanExecute = nameof(CanModifyFolder))]
    private async Task FolderRenameAsync() => await RenameFolderAsync();

    private async Task RenameFolderAsync()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath)) return;

        string? newName = await DialogService.PromptAsync(
            "Rename folder", "Enter new folder name:",
            initialValue: Path.GetFileName(_selectedFolderPath), maxLength: 128);

        if (string.IsNullOrWhiteSpace(newName) || newName == Path.GetFileName(_selectedFolderPath)) return;

        string parent = Path.GetDirectoryName(_selectedFolderPath) ?? _contentRoot;
        string newPath = Path.Combine(parent, newName);
        try { Directory.Move(_selectedFolderPath, newPath); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to rename folder: {ex.Message}", LogLevel.Error); return; }

        if (CurrentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            CurrentFolderPath = CurrentFolderPath.Replace(_selectedFolderPath, newPath, StringComparison.OrdinalIgnoreCase);

        _expandedFolders.Remove(_selectedFolderPath);
        _expandedFolders.Add(newPath);
        _selectedFolderPath = string.Empty;
        CanModifyFolder = false;

        BuildFolderTree();
        FolderChanged?.Invoke();
        LoadAssetsFromFolder();
    }

    [RelayCommand(CanExecute = nameof(CanModifyFolder))]
    private async Task FolderDeleteAsync() => await DeleteFolderAsync();

    private async Task DeleteFolderAsync()
    {
        if (string.IsNullOrEmpty(_selectedFolderPath)) return;

        string folderName = Path.GetFileName(_selectedFolderPath);
        bool confirmed = await DialogService.ConfirmAsync(
            "Delete folder",
            $"Delete '{folderName}' and all its contents? This cannot be undone.",
            "Delete", "Cancel");

        if (!confirmed) return;

        try { Directory.Delete(_selectedFolderPath, recursive: true); }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to delete folder: {ex.Message}", LogLevel.Error); return; }

        if (CurrentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            CurrentFolderPath = _contentRoot;

        _expandedFolders.Remove(_selectedFolderPath);
        _selectedFolderPath = string.Empty;
        CanModifyFolder = false;

        BuildFolderTree();
        FolderChanged?.Invoke();
        LoadAssetsFromFolder();
    }

    // ── New asset creation ──────────────────────────────────────────────────────

    [RelayCommand]
    private Task NewMaterialAsync()
        => CreateAssetFileAsync(".mat.json", """{"ShaderPath":"","Properties":{}}""",
            "New Material", "Enter material name:");

    [RelayCommand]
    private Task NewUIThemeAsync()
        => CreateAssetFileAsync(".uitheme.json", """{"Controls":{}}""",
            "New UI Theme", "Enter UI theme name:");

    [RelayCommand]
    private Task NewSpriteAsync()
        => CreateAssetFileAsync(".sprite.json",
            """{"TexturePath":"","NineSliceBorders":{"Left":0,"Right":0,"Top":0,"Bottom":0}}""",
            "New Sprite NineSlice", "Enter sprite name:");

    private async Task CreateAssetFileAsync(string suffix, string defaultContent, string title, string prompt)
    {
        if (string.IsNullOrEmpty(CurrentFolderPath)) return;

        string? name = await DialogService.PromptAsync(title, prompt, maxLength: 128);
        if (string.IsNullOrWhiteSpace(name)) return;

        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            name = name[..^suffix.Length];

        string filePath = Path.Combine(CurrentFolderPath, name + suffix);
        try
        {
            await File.WriteAllTextAsync(filePath, defaultContent).ConfigureAwait(true);
            AssetInfo info = AssetClassifier.CreateInfo(filePath, _contentRoot);
            Bus.Publish(new AssetImportedEvent(info));
        }
        catch (Exception ex) { Log($"[AssetBrowser] Failed to create asset file: {ex.Message}", LogLevel.Error); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}
