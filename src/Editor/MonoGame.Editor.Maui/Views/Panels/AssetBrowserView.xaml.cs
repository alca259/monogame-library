using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Assets del dock inferior. Árbol de carpetas (izquierda) + lista de assets con
/// breadcrumb, cabeceras de columna y filtro de texto (derecha).
/// </summary>
public sealed partial class AssetBrowserView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private readonly ObservableCollection<FolderItem> _folderItems = [];
    private readonly ObservableCollection<AssetItem>  _assetItems  = [];
    private readonly HashSet<string> _expandedFolders = [];

    private string _contentRoot      = string.Empty;
    private string _currentFolderPath = string.Empty;
    private string _filterText        = string.Empty;
    private string _selectedFolderPath = string.Empty;

    private Action<ProjectOpenedEvent>? _onProjectOpened;
    private Action<AssetImportedEvent>? _onAssetImported;

    public AssetBrowserView()
    {
        InitializeComponent();
        FolderTree.ItemsSource = _folderItems;
        AssetList.ItemsSource  = _assetItems;
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
        _onAssetImported = _ => MainThread.BeginInvokeOnMainThread(() => LoadAssetsFromFolder());
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onAssetImported);
    }

    private void Unsubscribe()
    {
        if (_onProjectOpened is not null) _bus.Unsubscribe(_onProjectOpened);
        if (_onAssetImported is not null)  _bus.Unsubscribe(_onAssetImported);
    }

    // ── Project opened ────────────────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        _folderItems.Clear();
        _assetItems.Clear();
        _expandedFolders.Clear();
        BreadcrumbLayout.Children.Clear();
        AssetPathLabel.Text      = string.Empty;
        AssetCountLabel.Text     = "0 assets";
        AssetRenameBtn.IsEnabled = false;
        AssetDeleteBtn.IsEnabled = false;

        if (e.Project is null)
        {
            _contentRoot       = string.Empty;
            _currentFolderPath = string.Empty;
            NewFolderBtn.IsEnabled = false;
            return;
        }

        _contentRoot       = e.Project.ContentPath;
        _currentFolderPath = _contentRoot;
        _expandedFolders.Add(_contentRoot);
        NewFolderBtn.IsEnabled = true;

        BuildFolderTree();
        BuildBreadcrumb(_currentFolderPath);
        LoadAssetsFromFolder();
    }

    // ── Folder tree ───────────────────────────────────────────────────────────

    private void BuildFolderTree()
    {
        _folderItems.Clear();
        if (!Directory.Exists(_contentRoot)) return;
        FlattenFolders(_contentRoot, 0);
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

            // Assign AFTER BuildFolderTree(): clearing _folderItems fires SelectionChanged(empty)
            // which would reset _selectedFolderPath and disable the menu items.
            _selectedFolderPath = dir;
            bool isRoot = string.Equals(dir, _contentRoot, StringComparison.OrdinalIgnoreCase);
            FolderCtxRenameItem.IsEnabled = !isRoot;
            FolderCtxDeleteItem.IsEnabled = !isRoot;

            if (item.IsExpanded)
            {
                _currentFolderPath = dir;
                BuildBreadcrumb(dir);
                LoadAssetsFromFolder();
            }
        });

        _folderItems.Add(item);

        if (isExpanded)
        {
            foreach (string sub in subdirs.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                FlattenFolders(sub, depth + 1);
        }
    }

    // ── Asset list ────────────────────────────────────────────────────────────

    private void LoadAssetsFromFolder()
    {
        _assetItems.Clear();
        if (!Directory.Exists(_currentFolderPath)) return;

        int count = 0;
        foreach (string file in Directory.GetFiles(_currentFolderPath)
                                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
        {
            AssetInfo info = AssetClassifier.CreateInfo(file, _contentRoot);
            if (!string.IsNullOrEmpty(_filterText) &&
                !info.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                continue;

            _assetItems.Add(new AssetItem(info));
            count++;
        }

        AssetCountLabel.Text = count == 1 ? "1 asset" : $"{count} assets";
    }

    // ── Breadcrumb ────────────────────────────────────────────────────────────

    private void BuildBreadcrumb(string folderPath)
    {
        BreadcrumbLayout.Children.Clear();
        if (string.IsNullOrEmpty(_contentRoot)) return;

        var segments = new List<(string Label, string Path)> { ("Content", _contentRoot) };

        string relative = Path.GetRelativePath(_contentRoot, folderPath);
        if (relative != ".")
        {
            string accumulated = _contentRoot;
            foreach (string part in relative.Split(Path.DirectorySeparatorChar))
            {
                accumulated = Path.Combine(accumulated, part);
                segments.Add((part, accumulated));
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            if (i > 0)
                BreadcrumbLayout.Children.Add(new Label
                {
                    Text            = " › ",
                    Style           = (Style)Application.Current!.Resources["DimLabel"],
                    VerticalOptions = LayoutOptions.Center,
                });

            (string segLabel, string segPath) = segments[i];
            bool isLast = i == segments.Count - 1;

            if (isLast)
            {
                BreadcrumbLayout.Children.Add(new Label
                {
                    Text            = segLabel,
                    Style           = (Style)Application.Current!.Resources["PrimaryLabel"],
                    VerticalOptions = LayoutOptions.Center,
                    Padding         = new Thickness(4, 0),
                });
            }
            else
            {
                string captured = segPath;
                Button btn = new()
                {
                    Text            = segLabel,
                    FontSize        = 11,
                    Padding         = new Thickness(4, 0),
                    HeightRequest   = 22,
                    BackgroundColor = Colors.Transparent,
                    TextColor       = Color.FromArgb("#9A9AA2"),
                    VerticalOptions = LayoutOptions.Center,
                };
                btn.Clicked += (_, _) => NavigateToFolder(captured);
                BreadcrumbLayout.Children.Add(btn);
            }
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void NavigateToFolder(string path)
    {
        _currentFolderPath = path;

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
        BuildBreadcrumb(path);
        LoadAssetsFromFolder();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnFilterChanged(object sender, TextChangedEventArgs e)
    {
        _filterText = e.NewTextValue ?? string.Empty;
        LoadAssetsFromFolder();
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not AssetItem item)
        {
            AssetPathLabel.Text       = string.Empty;
            AssetRenameBtn.IsEnabled  = false;
            AssetDeleteBtn.IsEnabled  = false;
            AssetCtxRenameItem.IsEnabled = false;
            AssetCtxDeleteItem.IsEnabled = false;
            return;
        }

        AssetPathLabel.Text          = item.Info.RelativePath;
        AssetRenameBtn.IsEnabled     = true;
        AssetDeleteBtn.IsEnabled     = true;
        AssetCtxRenameItem.IsEnabled = true;
        AssetCtxDeleteItem.IsEnabled = true;
        EditorContext.Instance.EventBus.Publish(new AssetSelectedEvent(item.Info));
    }

    private async void OnImportAssetClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        FileResult? picked = await FilePicker.PickAsync().ConfigureAwait(true);
        if (picked is null) return;

        string dest = Path.Combine(_currentFolderPath, picked.FileName);
        try
        {
            File.Copy(picked.FullPath, dest, overwrite: false);
        }
        catch { return; }

        AssetInfo info = AssetClassifier.CreateInfo(dest, _contentRoot);
        _bus.Publish(new AssetImportedEvent(info));
    }

    private async void OnRenameAssetClicked(object sender, EventArgs e)
    {
        if (AssetList.SelectedItem is not AssetItem item) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? newName = await page.DisplayPromptAsync(
            "Rename asset",
            "Enter new file name:",
            initialValue: item.Info.Name,
            maxLength: 256,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName) || newName == item.Info.Name) return;

        string newPath = Path.Combine(_currentFolderPath, newName);
        try { File.Move(item.Info.AbsolutePath, newPath); }
        catch { return; }

        LoadAssetsFromFolder();
    }

    private async void OnDeleteAssetClicked(object sender, EventArgs e)
    {
        if (AssetList.SelectedItem is not AssetItem item) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        bool confirmed = await page.DisplayAlertAsync(
            "Delete asset",
            $"Delete '{item.Info.Name}'? This cannot be undone.",
            "Delete", "Cancel");

        if (!confirmed) return;

        try { File.Delete(item.Info.AbsolutePath); }
        catch { return; }

        LoadAssetsFromFolder();
    }

    // ── Folder selection ──────────────────────────────────────────────────────

    private void OnFolderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not FolderItem item)
        {
            _selectedFolderPath          = string.Empty;
            FolderCtxRenameItem.IsEnabled = false;
            FolderCtxDeleteItem.IsEnabled = false;
            return;
        }

        _selectedFolderPath = item.FullPath;
        bool isRoot = string.Equals(_selectedFolderPath, _contentRoot, StringComparison.OrdinalIgnoreCase);
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
        LoadAssetsFromFolder();
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

        string parent  = Path.GetDirectoryName(_selectedFolderPath) ?? _contentRoot;
        string newPath = Path.Combine(parent, newName);
        try { Directory.Move(_selectedFolderPath, newPath); }
        catch { return; }

        if (_currentFolderPath.StartsWith(_selectedFolderPath, StringComparison.OrdinalIgnoreCase))
            _currentFolderPath = _currentFolderPath.Replace(_selectedFolderPath, newPath, StringComparison.OrdinalIgnoreCase);

        _expandedFolders.Remove(_selectedFolderPath);
        _expandedFolders.Add(newPath);
        _selectedFolderPath           = string.Empty;
        FolderCtxRenameItem.IsEnabled = false;
        FolderCtxDeleteItem.IsEnabled = false;

        BuildFolderTree();
        BuildBreadcrumb(_currentFolderPath);
        LoadAssetsFromFolder();
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
            _currentFolderPath = _contentRoot;

        _expandedFolders.Remove(_selectedFolderPath);
        _selectedFolderPath           = string.Empty;
        FolderCtxRenameItem.IsEnabled = false;
        FolderCtxDeleteItem.IsEnabled = false;

        BuildFolderTree();
        BuildBreadcrumb(_currentFolderPath);
        LoadAssetsFromFolder();
    }

    // ── New asset creation ────────────────────────────────────────────────────

    private async void OnNewMaterialClicked(object sender, EventArgs e)
        => await CreateAssetFileAsync(".mat.json",
            """{"ShaderPath":"","Properties":{}}""",
            "New Material", "Enter material name:");

    private async void OnNewUIThemeClicked(object sender, EventArgs e)
        => await CreateAssetFileAsync(".uitheme.json",
            """{"Controls":{}}""",
            "New UI Theme", "Enter UI theme name:");

    private async void OnNewSpriteClicked(object sender, EventArgs e)
        => await CreateAssetFileAsync(".sprite.json",
            """{"TexturePath":"","NineSliceBorders":{"Left":0,"Right":0,"Top":0,"Bottom":0}}""",
            "New Sprite NineSlice", "Enter sprite name:");

    private async Task CreateAssetFileAsync(string suffix, string defaultContent,
                                             string title, string prompt)
    {
        if (string.IsNullOrEmpty(_currentFolderPath)) return;

        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        string? name = await page.DisplayPromptAsync(title, prompt,
            maxLength: 128, keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(name)) return;

        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            name = name[..^suffix.Length];

        string filePath = Path.Combine(_currentFolderPath, name + suffix);
        try
        {
            await File.WriteAllTextAsync(filePath, defaultContent);
            AssetInfo info = AssetClassifier.CreateInfo(filePath, _contentRoot);
            _bus.Publish(new AssetImportedEvent(info));
        }
        catch { }
    }
}
