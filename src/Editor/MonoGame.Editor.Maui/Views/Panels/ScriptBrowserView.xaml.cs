using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "Scripts". Left panel shows a folder tree rooted at
/// <see cref="EditorProject.GameScriptsPath"/>; right panel lists *.cs files
/// in the selected folder. Reuses <see cref="FolderItem"/> from AssetBrowserView.
/// </summary>
public sealed partial class ScriptBrowserView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private readonly ObservableCollection<FolderItem> _folderItems  = [];
    private readonly ObservableCollection<string>     _scriptItems  = [];
    private readonly HashSet<string> _expandedFolders = [];

    private string _scriptsRoot         = string.Empty;
    private string _currentFolderPath   = string.Empty;
    private string _selectedFolderPath  = string.Empty;

    private Action<ProjectOpenedEvent>? _onProjectOpened;

    public ScriptBrowserView()
    {
        InitializeComponent();
        FolderTree.ItemsSource  = _folderItems;
        ScriptList.ItemsSource  = _scriptItems;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _folderItems.Clear();
        _scriptItems.Clear();
        _expandedFolders.Clear();
        ScriptCountLabel.Text     = "0 scripts";
        _selectedFolderPath       = string.Empty;
        FolderRenameBtn.IsEnabled = false;
        FolderDeleteBtn.IsEnabled = false;
        NewFolderBtn.IsEnabled    = false;

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
        NewFolderBtn.IsEnabled = true;
        BuildFolderTree();
        LoadScripts();
    }

    // ── Folder tree ───────────────────────────────────────────────────────────

    private void BuildFolderTree()
    {
        _folderItems.Clear();
        if (!Directory.Exists(_scriptsRoot)) return;
        FlattenFolders(_scriptsRoot, 0);
    }

    private void FlattenFolders(string dir, int depth)
    {
        if (!Directory.Exists(dir)) return;

        string[] subdirs    = Directory.GetDirectories(dir);
        bool     hasChildren = subdirs.Length > 0;
        bool     isExpanded  = _expandedFolders.Contains(dir);

        FolderItem item = null!;
        item = new FolderItem(dir, depth, hasChildren, isExpanded, () =>
        {
            if (item.IsExpanded)
                _expandedFolders.Add(dir);
            else
                _expandedFolders.Remove(dir);

            BuildFolderTree();

            if (item.IsExpanded)
            {
                _currentFolderPath = dir;
                LoadScripts();
            }
        });

        _folderItems.Add(item);

        if (isExpanded)
        {
            foreach (string sub in subdirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                FlattenFolders(sub, depth + 1);
        }
    }

    // ── Script list ───────────────────────────────────────────────────────────

    private void LoadScripts()
    {
        _scriptItems.Clear();
        if (!Directory.Exists(_currentFolderPath)) return;

        int count = 0;
        foreach (string file in Directory.GetFiles(_currentFolderPath, "*.cs")
                                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            _scriptItems.Add(Path.GetFileName(file));
            count++;
        }

        ScriptCountLabel.Text = count == 1 ? "1 script" : $"{count} scripts";
    }

    // ── Rescan ────────────────────────────────────────────────────────────────

    private void OnRescanClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_scriptsRoot)) return;
        BuildFolderTree();
        LoadScripts();
    }

    // ── Folder selection ──────────────────────────────────────────────────────

    private void OnFolderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not FolderItem item)
        {
            _selectedFolderPath       = string.Empty;
            FolderRenameBtn.IsEnabled = false;
            FolderDeleteBtn.IsEnabled = false;
            FolderCtxRenameItem.IsEnabled = false;
            FolderCtxDeleteItem.IsEnabled = false;
            return;
        }

        _selectedFolderPath = item.FullPath;
        bool isRoot = string.Equals(_selectedFolderPath, _scriptsRoot, StringComparison.OrdinalIgnoreCase);
        FolderRenameBtn.IsEnabled     = !isRoot;
        FolderDeleteBtn.IsEnabled     = !isRoot;
        FolderCtxRenameItem.IsEnabled = !isRoot;
        FolderCtxDeleteItem.IsEnabled = !isRoot;
    }

    // ── Folder management ─────────────────────────────────────────────────────

    private async void OnNewFolderClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? name = await page.DisplayPromptAsync(
            "New folder",
            "Enter folder name:",
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(name)) return;

        string newPath = Path.Combine(_currentFolderPath, name);
        try { Directory.CreateDirectory(newPath); }
        catch { return; }

        _expandedFolders.Add(_currentFolderPath);
        BuildFolderTree();
        LoadScripts();
    }

    private async void OnFolderRenameClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFolderPath)) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? newName = await page.DisplayPromptAsync(
            "Rename folder",
            "Enter new folder name:",
            initialValue: Path.GetFileName(_selectedFolderPath),
            maxLength: 128,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName) || newName == Path.GetFileName(_selectedFolderPath)) return;

        string parent  = Path.GetDirectoryName(_selectedFolderPath) ?? _scriptsRoot;
        string newPath = Path.Combine(parent, newName);
        try { Directory.Move(_selectedFolderPath, newPath); }
        catch { return; }

        if (_currentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolderPath = _currentFolderPath.Replace(_selectedFolderPath, newPath, StringComparison.OrdinalIgnoreCase);

        _expandedFolders.Remove(_selectedFolderPath);
        _expandedFolders.Add(newPath);
        _selectedFolderPath           = string.Empty;
        FolderRenameBtn.IsEnabled     = false;
        FolderDeleteBtn.IsEnabled     = false;
        FolderCtxRenameItem.IsEnabled = false;
        FolderCtxDeleteItem.IsEnabled = false;

        BuildFolderTree();
        LoadScripts();
    }

    private async void OnFolderDeleteClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFolderPath)) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string folderName = Path.GetFileName(_selectedFolderPath);
        bool confirmed = await page.DisplayAlertAsync(
            "Delete folder",
            $"Delete '{folderName}' and all its contents? This cannot be undone.",
            "Delete", "Cancel");

        if (!confirmed) return;

        try { Directory.Delete(_selectedFolderPath, recursive: true); }
        catch { return; }

        if (_currentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolderPath = _scriptsRoot;

        _expandedFolders.Remove(_selectedFolderPath);
        _selectedFolderPath           = string.Empty;
        FolderRenameBtn.IsEnabled     = false;
        FolderDeleteBtn.IsEnabled     = false;
        FolderCtxRenameItem.IsEnabled = false;
        FolderCtxDeleteItem.IsEnabled = false;

        BuildFolderTree();
        LoadScripts();
    }

    // ── New script creation ───────────────────────────────────────────────────

    private async void OnNewScriptClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        EditorProject? project = EditorContext.Instance.ActiveProject;
        ProjectSettings? settings = project is not null
            ? await ProjectSettings.LoadAsync(project).ConfigureAwait(true)
            : null;

        string defaultNs = settings?.RootNamespace ?? string.Empty;

        ScriptCreationResult? result = await ScriptCreationDialog
            .ShowAsync(page.Navigation, defaultNs)
            .ConfigureAwait(true);

        if (result is null) return;

        string targetFolder = string.IsNullOrEmpty(result.RelativeFolder)
            ? _currentFolderPath
            : Path.Combine(_currentFolderPath, result.RelativeFolder);

        try { Directory.CreateDirectory(targetFolder); }
        catch { return; }

        string filePath = Path.Combine(targetFolder, result.ClassName + ".cs");
        string ns       = string.IsNullOrEmpty(result.NamespaceName) ? defaultNs : result.NamespaceName;
        string content  = GenerateScriptTemplate(result.ClassName, ns);

        try
        {
            await File.WriteAllTextAsync(filePath, content);
            _expandedFolders.Add(targetFolder);
            BuildFolderTree();
            _currentFolderPath = targetFolder;
            LoadScripts();
        }
        catch { }
    }

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
