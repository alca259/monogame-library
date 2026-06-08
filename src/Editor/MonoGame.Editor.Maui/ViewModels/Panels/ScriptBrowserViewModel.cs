using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Panels;

namespace MonoGame.Editor.Maui.ViewModels.Panels;

/// <summary>
/// ViewModel de la pestaña Scripts del dock: árbol de carpetas bajo
/// <see cref="EditorProject.GameScriptsPath"/> y lista de ficheros <c>*.cs</c> de la
/// carpeta seleccionada, con creación/renombrado/borrado de carpetas y scripts.
/// </summary>
public sealed partial class ScriptBrowserViewModel : ViewModelBase
{
    private readonly HashSet<string> _expandedFolders = [];

    private string _scriptsRoot        = string.Empty;
    private string _currentFolderPath  = string.Empty;
    private string _selectedFolderPath = string.Empty;
    private string _selectedScriptFile = string.Empty;

    public ObservableCollection<FolderItem> FolderItems { get; } = [];
    public ObservableCollection<ScriptItem> ScriptItems { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewScriptCommand))]
    private bool _canManage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FolderRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(FolderDeleteCommand))]
    private bool _canModifyFolder;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScriptRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScriptDeleteCommand))]
    private bool _canModifyScript;

    [ObservableProperty]
    private string _scriptCountText = "0 scripts";

    protected override void RegisterEvents()
    {
        On<ProjectOpenedEvent>(OnProjectOpened);
    }

    protected override void OnAttached()
    {
        // Si ya hay proyecto activo al engancharse, reconstruir el árbol.
        if (Context.ActiveProject is { } project)
            OnProjectOpened(new ProjectOpenedEvent(project));
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        FolderItems.Clear();
        ScriptItems.Clear();
        _expandedFolders.Clear();
        ScriptCountText     = "0 scripts";
        _selectedFolderPath = string.Empty;
        _selectedScriptFile = string.Empty;
        CanModifyFolder     = false;
        CanModifyScript     = false;
        CanManage           = false;

        if (e.Project is null)
        {
            _scriptsRoot = _currentFolderPath = string.Empty;
            return;
        }

        _scriptsRoot = e.Project.GameScriptsPath;
        if (!Directory.Exists(_scriptsRoot))
        {
            _currentFolderPath = string.Empty;
            return;
        }

        _currentFolderPath = _scriptsRoot;
        _expandedFolders.Add(_scriptsRoot);
        CanManage = true;
        BuildFolderTree();
        LoadScripts();
    }

    // ── Folder tree ───────────────────────────────────────────────────────────

    private void BuildFolderTree()
    {
        FolderItems.Clear();
        if (!Directory.Exists(_scriptsRoot)) return;
        FlattenFolders(_scriptsRoot, 0);
    }

    private void FlattenFolders(string dir, int depth)
    {
        if (!Directory.Exists(dir)) return;

        string[] subdirs     = Directory.GetDirectories(dir);
        bool     hasChildren = subdirs.Length > 0;
        bool     isExpanded  = _expandedFolders.Contains(dir);
        bool     isRoot      = string.Equals(dir, _scriptsRoot, StringComparison.OrdinalIgnoreCase);

        FolderItem item = null!;
        item = new FolderItem(dir, depth, hasChildren, isExpanded, isRoot,
            onToggle: () =>
            {
                if (item.IsExpanded) _expandedFolders.Add(dir);
                else _expandedFolders.Remove(dir);

                BuildFolderTree();

                _selectedFolderPath = dir;
                CanModifyFolder     = !isRoot;

                if (item.IsExpanded)
                {
                    _currentFolderPath = dir;
                    LoadScripts();
                }
            },
            onRename: () => { _selectedFolderPath = dir; _ = RenameFolderAsync(); },
            onDelete: () => { _selectedFolderPath = dir; _ = DeleteFolderAsync(); });

        FolderItems.Add(item);

        if (isExpanded)
        {
            foreach (string sub in subdirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                FlattenFolders(sub, depth + 1);
        }
    }

    // ── Script list ───────────────────────────────────────────────────────────

    private void LoadScripts()
    {
        ScriptItems.Clear();
        _selectedScriptFile = string.Empty;
        CanModifyScript     = false;

        if (!Directory.Exists(_currentFolderPath)) return;

        int count = 0;
        foreach (string file in Directory.GetFiles(_currentFolderPath, "*.cs")
                                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            string fileName = Path.GetFileName(file);
            ScriptItems.Add(new ScriptItem(
                fileName,
                onTap:    () => { _selectedScriptFile = fileName; CanModifyScript = true; },
                onRename: () => { _selectedScriptFile = fileName; _ = RenameScriptAsync(); },
                onDelete: () => { _selectedScriptFile = fileName; _ = DeleteScriptAsync(); }));
            count++;
        }

        ScriptCountText = count == 1 ? "1 script" : $"{count} scripts";
    }

    // ── Commands ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Rescan()
    {
        if (string.IsNullOrEmpty(_scriptsRoot)) return;
        BuildFolderTree();
        LoadScripts();
    }

    [RelayCommand(CanExecute = nameof(CanManage))]
    private async Task NewFolderAsync()
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        string? name = await DialogService.PromptAsync("New folder", "Enter folder name:", maxLength: 128);
        if (string.IsNullOrWhiteSpace(name)) return;

        string newPath = Path.Combine(_currentFolderPath, name);
        try { Directory.CreateDirectory(newPath); }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to create folder: {ex.Message}", LogLevel.Error); return; }

        _expandedFolders.Add(_currentFolderPath);
        BuildFolderTree();
        LoadScripts();
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

        string parent  = Path.GetDirectoryName(_selectedFolderPath) ?? _scriptsRoot;
        string newPath = Path.Combine(parent, newName);
        try { Directory.Move(_selectedFolderPath, newPath); }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to rename folder: {ex.Message}", LogLevel.Error); return; }

        if (_currentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolderPath = _currentFolderPath.Replace(_selectedFolderPath, newPath, StringComparison.OrdinalIgnoreCase);

        _expandedFolders.Remove(_selectedFolderPath);
        _expandedFolders.Add(newPath);
        _selectedFolderPath = string.Empty;
        CanModifyFolder     = false;

        BuildFolderTree();
        LoadScripts();
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
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to delete folder: {ex.Message}", LogLevel.Error); return; }

        if (_currentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolderPath = _scriptsRoot;

        _expandedFolders.Remove(_selectedFolderPath);
        _selectedFolderPath = string.Empty;
        CanModifyFolder     = false;

        BuildFolderTree();
        LoadScripts();
    }

    [RelayCommand(CanExecute = nameof(CanModifyScript))]
    private async Task ScriptRenameAsync() => await RenameScriptAsync();

    private async Task RenameScriptAsync()
    {
        if (string.IsNullOrEmpty(_selectedScriptFile)) return;

        string oldClassName = Path.GetFileNameWithoutExtension(_selectedScriptFile);
        string? newClassName = await DialogService.PromptAsync(
            "Rename script", "Enter new class name (file will be renamed to match):",
            initialValue: oldClassName, maxLength: 128);

        if (string.IsNullOrWhiteSpace(newClassName) || newClassName == oldClassName) return;

        string oldPath = Path.Combine(_currentFolderPath, _selectedScriptFile);
        string newPath = Path.Combine(_currentFolderPath, newClassName + ".cs");

        try
        {
            string content = await File.ReadAllTextAsync(oldPath).ConfigureAwait(true);
            content = content.Replace($"class {oldClassName}", $"class {newClassName}");
            await File.WriteAllTextAsync(newPath, content).ConfigureAwait(true);
            if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldPath);
        }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to rename script: {ex.Message}", LogLevel.Error); return; }

        _selectedScriptFile = string.Empty;
        CanModifyScript     = false;
        LoadScripts();
    }

    [RelayCommand(CanExecute = nameof(CanModifyScript))]
    private async Task ScriptDeleteAsync() => await DeleteScriptAsync();

    private async Task DeleteScriptAsync()
    {
        if (string.IsNullOrEmpty(_selectedScriptFile)) return;

        bool confirmed = await DialogService.ConfirmAsync(
            "Delete script", $"Delete '{_selectedScriptFile}'? This cannot be undone.", "Delete", "Cancel");

        if (!confirmed) return;

        string path = Path.Combine(_currentFolderPath, _selectedScriptFile);
        try { File.Delete(path); }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to delete script: {ex.Message}", LogLevel.Error); return; }

        _selectedScriptFile = string.Empty;
        CanModifyScript     = false;
        LoadScripts();
    }

    [RelayCommand(CanExecute = nameof(CanManage))]
    private async Task NewScriptAsync()
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;
        if (DialogService.Navigation is not { } navigation) return;

        EditorProject? project = Context.ActiveProject;
        ProjectSettings? settings = project is not null
            ? await ProjectSettings.LoadAsync(project).ConfigureAwait(true)
            : null;

        string defaultNs = settings?.RootNamespace ?? string.Empty;

        ScriptCreationResult? result = await ScriptCreationDialog.ShowAsync(navigation, defaultNs).ConfigureAwait(true);
        if (result is null) return;

        string targetFolder = string.IsNullOrEmpty(result.RelativeFolder)
            ? _currentFolderPath
            : Path.Combine(_currentFolderPath, result.RelativeFolder);

        try { Directory.CreateDirectory(targetFolder); }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to create target folder: {ex.Message}", LogLevel.Error); return; }

        string filePath = Path.Combine(targetFolder, result.ClassName + ".cs");
        string ns       = string.IsNullOrEmpty(result.NamespaceName) ? defaultNs : result.NamespaceName;
        string content  = GenerateScriptTemplate(result.ClassName, ns);

        try
        {
            await File.WriteAllTextAsync(filePath, content).ConfigureAwait(true);
            _expandedFolders.Add(targetFolder);
            BuildFolderTree();
            _currentFolderPath = targetFolder;
            LoadScripts();
        }
        catch (Exception ex) { Log($"[ScriptBrowser] Failed to create script: {ex.Message}", LogLevel.Error); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => Bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));

    private static string GenerateScriptTemplate(string className, string ns) =>
        string.IsNullOrEmpty(ns)
            ? $$"""
              using Alca.MonoGame.Kernel.ECS;

              public sealed class {{className}} : GameBehaviour
              {
                  public override void Update(float deltaTime)
                  {
                  }
              }
              """
            : $$"""
              using Alca.MonoGame.Kernel.ECS;

              namespace {{ns}};

              public sealed class {{className}} : GameBehaviour
              {
                  public override void Update(float deltaTime)
                  {
                  }
              }
              """;
}
