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
        AssetPathLabel.Text  = string.Empty;
        AssetCountLabel.Text = "0 assets";

        if (e.Project is null)
        {
            _contentRoot       = string.Empty;
            _currentFolderPath = string.Empty;
            return;
        }

        _contentRoot       = e.Project.ContentPath;
        _currentFolderPath = _contentRoot;
        _expandedFolders.Add(_contentRoot);

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
        if (e.CurrentSelection.FirstOrDefault() is not AssetItem item) return;
        AssetPathLabel.Text = item.Info.RelativePath;
        EditorContext.Instance.EventBus.Publish(new AssetSelectedEvent(item.Info));
    }
}
