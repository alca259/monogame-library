using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Core.Project;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Navegador de assets de dos paneles: árbol de carpetas a la izquierda, lista de archivos con iconos de tipo y
/// una vista previa de metadatos a la derecha. Se integra con <see cref="ContentWatcher"/> a través del
/// bus de eventos del editor.
/// </summary>
public sealed class AssetBrowserPanel : UserControl
{
    #region Constants

    private const int DefaultSplitterDistance = 180;
    private const int MinPanelSize = 80;
    private const int PreviewHeight = 130;

    #endregion

    #region Icon indices (ImageList)

    private const int IconUnknown   = 0;
    private const int IconTexture   = 1;
    private const int IconAudio     = 2;
    private const int IconFont      = 3;
    private const int IconTiledMap  = 4;
    private const int IconScene     = 5;
    private const int IconPrefab    = 6;
    private const int IconParticles = 7;
    private const int IconAnimation = 8;
    private const int IconInputMap  = 9;
    private const int IconScript    = 10;
    private const int IconFolder    = 11;
    private const int IconGenerated = 12;
    private const int IconMaterial  = 13;
    private const int IconSprite    = 14;
    private const int IconUITheme   = 15;

    #endregion

    #region Fields

    private EditorContext? _context;
    private string _contentRoot   = string.Empty;
    private string _currentFolder = string.Empty;
    private bool _largeIconMode;

    private readonly SplitContainer   _outerSplit;
    private readonly TreeView         _folderTree;
    private readonly SplitContainer   _rightSplit;
    private readonly ListView         _contentView;
    private readonly Panel            _previewPanel;
    private readonly PictureBox       _previewImage;
    private readonly Label            _previewInfo;
    private readonly ImageList        _typeIcons;
    private readonly ImageList        _largeIcons;
    private readonly Panel            _topBar;
    private readonly TextBox          _filterBox;
    private readonly Button           _viewToggleBtn;
    private readonly FlowLayoutPanel  _breadcrumb;
    private readonly ContextMenuStrip _itemContextMenu;
    private readonly ContextMenuStrip _folderContextMenu;
    private readonly ToolStripMenuItem _renameFolderItem;
    private readonly ToolStripMenuItem _deleteFolderItem;
    private readonly ToolStripMenuItem _newMaterialItem;
    private readonly ToolStripMenuItem _newUIThemeItem;
    private readonly System.Windows.Forms.Timer _searchDebounce;

    #endregion

    #region Constructor

    /// <summary>Inicializa el panel y construye la interfaz de usuario.</summary>
    public AssetBrowserPanel()
    {
        _typeIcons  = BuildIconList(16);
        _largeIcons = BuildLargeIconList();

        // ── Ruta de navegación (breadcrumb) ──────────────────────────────
        _breadcrumb = new FlowLayoutPanel
        {
            Dock     = DockStyle.Top,
            Height   = 22,
            AutoSize = false,
            Padding  = new Padding(2, 0, 2, 0),
        };

        // ── Barra superior (filtro + alternancia de vista) ────────────────
        _filterBox = new TextBox
        {
            Dock            = DockStyle.Fill,
            PlaceholderText = "Filter assets...",
            BorderStyle     = BorderStyle.FixedSingle,
        };
        _viewToggleBtn = new Button
        {
            Text      = "⊞",
            Width     = 28,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
        };
        _topBar = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(2) };
        _topBar.Controls.Add(_filterBox);
        _topBar.Controls.Add(_viewToggleBtn);

        // ── Menú contextual de archivo ─────────────────────────────────────
        ToolStripMenuItem openExternalItem     = new("Open with External Editor");
        ToolStripMenuItem revealInExplorerItem = new("Reveal in Explorer");
        ToolStripMenuItem renameItem           = new("Rename");
        ToolStripMenuItem deleteItem           = new("Delete");
        ToolStripMenuItem copyPathItem         = new("Copy Relative Path");

        _itemContextMenu = new ContextMenuStrip();
        _itemContextMenu.Items.AddRange(new ToolStripItem[]
        {
            openExternalItem,
            revealInExplorerItem,
            new ToolStripSeparator(),
            renameItem,
            deleteItem,
            new ToolStripSeparator(),
            copyPathItem,
        });

        openExternalItem.Click     += OnOpenExternal;
        revealInExplorerItem.Click += OnRevealInExplorer;
        renameItem.Click           += OnRenameItem;
        deleteItem.Click           += OnDeleteItem;
        copyPathItem.Click         += OnCopyRelativePath;

        // ── Menú contextual de carpeta ──────────────────────────────────────
        ToolStripMenuItem newFolderItem = new("New Folder");
        _newMaterialItem                = new("New Material");
        _newUIThemeItem                 = new("New UI Theme");
        _renameFolderItem               = new("Rename");
        _deleteFolderItem               = new("Delete");

        _folderContextMenu = new ContextMenuStrip();
        _folderContextMenu.Items.AddRange(new ToolStripItem[]
        {
            newFolderItem,
            _newMaterialItem,
            _newUIThemeItem,
            new ToolStripSeparator(),
            _renameFolderItem,
            _deleteFolderItem,
        });

        newFolderItem.Click        += OnNewFolder;
        _newMaterialItem.Click     += OnNewMaterial;
        _newUIThemeItem.Click      += OnNewUITheme;
        _renameFolderItem.Click    += OnRenameFolder;
        _deleteFolderItem.Click    += OnDeleteFolder;
        _folderContextMenu.Opening += OnFolderContextMenuOpening;

        // ── Temporizador de antirrebote ───────────────────────────────────
        _searchDebounce = new System.Windows.Forms.Timer { Interval = 150 };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            if (!string.IsNullOrEmpty(_currentFolder))
                ShowFolderContents(_currentFolder);
        };
        _filterBox.TextChanged += (_, _) => { _searchDebounce.Stop(); _searchDebounce.Start(); };

        // ── Árbol de carpetas (izquierda) ─────────────────────────────────
        _folderTree = new TreeView
        {
            Dock             = DockStyle.Fill,
            HideSelection    = false,
            ShowLines        = true,
            ShowPlusMinus    = true,
            BorderStyle      = BorderStyle.None,
            ImageList        = _typeIcons,
            LabelEdit        = true,
            ContextMenuStrip = _folderContextMenu,
        };

        // ── Lista de archivos (arriba-derecha) ────────────────────────────
        _contentView = new ListView
        {
            Dock             = DockStyle.Fill,
            View             = View.Details,
            FullRowSelect    = true,
            MultiSelect      = false,
            ShowItemToolTips = true,
            BorderStyle      = BorderStyle.None,
            SmallImageList   = _typeIcons,
            LargeImageList   = _largeIcons,
            AllowDrop        = true,
            LabelEdit        = true,
            ContextMenuStrip = _itemContextMenu,
        };
        _contentView.Columns.Add("Name", 200);
        _contentView.Columns.Add("Type", 80);
        _contentView.Columns.Add("Size", 70);

        // ── Vista previa (abajo-derecha) ──────────────────────────────────
        _previewImage = new PictureBox
        {
            Dock        = DockStyle.Left,
            Width       = 120,
            Height      = PreviewHeight,
            SizeMode    = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.None,
        };
        _previewInfo = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.TopLeft,
            Padding   = new Padding(4),
            Font      = new System.Drawing.Font("Segoe UI", 8.5f),
        };

        _previewPanel = new Panel
        {
            Dock   = DockStyle.Fill,
            Height = PreviewHeight,
        };
        _previewPanel.Controls.Add(_previewInfo);
        _previewPanel.Controls.Add(_previewImage);

        // ── División derecha (lista / vista previa) ───────────────────────
        Panel rightTopPanel = new Panel { Dock = DockStyle.Fill };
        rightTopPanel.Controls.Add(_contentView);
        rightTopPanel.Controls.Add(_topBar);
        rightTopPanel.Controls.Add(_breadcrumb);

        _rightSplit = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Horizontal,
            SplitterDistance = 200,
            Panel1MinSize    = 80,
            Panel2MinSize    = PreviewHeight,
            FixedPanel       = FixedPanel.Panel2,
        };
        _rightSplit.Panel1.Controls.Add(rightTopPanel);
        _rightSplit.Panel2.Controls.Add(_previewPanel);
        _rightSplit.Panel2Collapsed = true;

        // ── División exterior (árbol / derecha) ───────────────────────────
        _outerSplit = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            SplitterDistance = DefaultSplitterDistance,
            Panel1MinSize    = MinPanelSize,
            Panel2MinSize    = MinPanelSize,
        };
        _outerSplit.Panel1.Controls.Add(_folderTree);
        _outerSplit.Panel2.Controls.Add(_rightSplit);

        Controls.Add(_outerSplit);
        AllowDrop = true;

        // Conectar eventos
        _folderTree.AfterSelect           += OnFolderSelected;
        _folderTree.BeforeExpand          += OnBeforeExpand;
        _folderTree.MouseDown             += OnFolderTreeMouseDown;
        _folderTree.AfterLabelEdit        += OnFolderAfterLabelEdit;
        _contentView.SelectedIndexChanged += OnContentSelectionChanged;
        _contentView.ItemDrag             += OnItemDrag;
        _contentView.DragEnter            += OnDragEnter;
        _contentView.DragDrop             += OnDragDrop;
        _contentView.AfterLabelEdit       += OnFileAfterLabelEdit;
        _viewToggleBtn.Click              += OnViewToggle;
        DragEnter                         += OnDragEnter;
        DragDrop                          += OnDragDrop;
    }

    #endregion

    #region Public API

    /// <summary>Obtiene o establece la distancia del divisor carpeta/contenido en píxeles.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SplitterDistance
    {
        get => _outerSplit.SplitterDistance;
        set { if (value >= MinPanelSize) _outerSplit.SplitterDistance = value; }
    }

    /// <summary>
    /// Se suscribe al bus de eventos para actualizar el panel cuando los assets cambian o se abre un proyecto.
    /// Debe llamarse una vez después de que el formulario padre haya sido construido.
    /// </summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<AssetImportedEvent>(OnAssetImported);
        _context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    /// <summary>Carga el árbol de directorios con raíz en <paramref name="rootPath"/>.</summary>
    public void SetRootDirectory(string rootPath)
    {
        _contentRoot = Directory.Exists(rootPath) ? rootPath : string.Empty;

        _folderTree.BeginUpdate();
        _folderTree.Nodes.Clear();
        _contentView.Items.Clear();
        ClearPreview();

        if (string.IsNullOrEmpty(_contentRoot))
        {
            _folderTree.EndUpdate();
            return;
        }

        string displayName = Path.GetFileName(rootPath);
        if (string.IsNullOrEmpty(displayName)) displayName = rootPath;

        TreeNode root = CreateFolderNode(displayName, rootPath);
        _folderTree.Nodes.Add(root);
        root.Expand();

        _folderTree.EndUpdate();
    }

    /// <summary>Vuelve a leer la carpeta seleccionada actualmente, actualizando la lista de archivos.</summary>
    public new void Refresh()
    {
        if (_folderTree.SelectedNode?.Tag is string path)
            ShowFolderContents(path);
    }

    #endregion

    #region Event bus handlers

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }
        SetRootDirectory(evt.Project?.ContentPath ?? string.Empty);
    }

    private void OnAssetImported(AssetImportedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnAssetImported(evt)); return; }

        if (_folderTree.SelectedNode?.Tag is string currentFolder)
        {
            string dir = Path.GetDirectoryName(evt.Asset.AbsolutePath) ?? string.Empty;
            if (string.Equals(dir, currentFolder, StringComparison.OrdinalIgnoreCase))
                ShowFolderContents(currentFolder);
        }
    }

    #endregion

    #region Folder tree — lazy load

    private static TreeNode CreateFolderNode(string label, string path)
    {
        TreeNode node = new(label)
        {
            Tag                = path,
            ImageIndex         = IconFolder,
            SelectedImageIndex = IconFolder,
        };
        try
        {
            if (Directory.GetDirectories(path).Length > 0)
                node.Nodes.Add(new TreeNode()); // lazy placeholder
        }
        catch (UnauthorizedAccessException) { }
        return node;
    }

    private void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node is not { Tag: string path }) return;
        if (e.Node.Nodes.Count != 1 || e.Node.Nodes[0].Tag is not null) return;

        _folderTree.BeginUpdate();
        e.Node.Nodes.Clear();
        try
        {
            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dirs.Length; i++)
            {
                string name = Path.GetFileName(dirs[i]);
                if (!string.IsNullOrEmpty(name))
                    e.Node.Nodes.Add(CreateFolderNode(name, dirs[i]));
            }
        }
        catch (UnauthorizedAccessException) { }
        _folderTree.EndUpdate();
    }

    private void OnFolderSelected(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not string path) return;
        ShowFolderContents(path);
    }

    private void OnFolderTreeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        TreeNode? node = _folderTree.GetNodeAt(e.Location);
        if (node is not null) _folderTree.SelectedNode = node;
    }

    #endregion

    #region Folder context menu

    private void OnFolderContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        TreeNode? node = _folderTree.SelectedNode;
        if (node is null) { e.Cancel = true; return; }

        bool isRoot = node.Parent is null;
        _renameFolderItem.Enabled = !isRoot;
        _deleteFolderItem.Enabled = !isRoot;
    }

    private void OnNewFolder(object? sender, EventArgs e)
    {
        TreeNode? parent = _folderTree.SelectedNode;
        if (parent?.Tag is not string parentPath) return;

        string baseName = "New Folder";
        string newPath  = Path.Combine(parentPath, baseName);
        int count = 1;
        while (Directory.Exists(newPath))
            newPath = Path.Combine(parentPath, $"{baseName} ({count++})");

        try
        {
            Directory.CreateDirectory(newPath);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, ex.Message, "Create Folder Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Eliminar marcador de posición diferido si está presente
        if (parent.Nodes.Count == 1 && parent.Nodes[0].Tag is null)
            parent.Nodes.Clear();

        string name = Path.GetFileName(newPath);
        TreeNode newNode = CreateFolderNode(name, newPath);
        parent.Nodes.Add(newNode);

        if (!parent.IsExpanded)
            parent.Expand();

        _folderTree.SelectedNode = newNode;
        newNode.BeginEdit();
    }

    private void OnNewMaterial(object? sender, EventArgs e) => CreateAssetFile(
        "NewMaterial", ".mat.json",
        path => JsonSerializer.Serialize(EditorMaterial.CreateEmpty(Path.GetFileNameWithoutExtension(path)),
            new JsonSerializerOptions { WriteIndented = true }));

    private void OnNewUITheme(object? sender, EventArgs e) => CreateAssetFile(
        "NewUITheme", ".uitheme.json",
        path => JsonSerializer.Serialize(EditorUITheme.CreateEmpty(Path.GetFileNameWithoutExtension(path)),
            new JsonSerializerOptions { WriteIndented = true }));

    private void CreateAssetFile(string baseName, string extension, Func<string, string> contentFactory)
    {
        if (string.IsNullOrEmpty(_currentFolder)) return;

        string newPath = Path.Combine(_currentFolder, baseName + extension);
        int count = 1;
        while (File.Exists(newPath))
            newPath = Path.Combine(_currentFolder, $"{baseName}{count++}{extension}");

        try
        {
            File.WriteAllText(newPath, contentFactory(newPath));
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, ex.Message, "Create File Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        ShowFolderContents(_currentFolder);

        for (int i = 0; i < _contentView.Items.Count; i++)
        {
            if (_contentView.Items[i].Tag is AssetInfo info && info.AbsolutePath == newPath)
            {
                _contentView.Items[i].Selected = true;
                _contentView.Items[i].Focused  = true;
                _contentView.EnsureVisible(i);
                break;
            }
        }
    }

    private void OnRenameFolder(object? sender, EventArgs e)
    {
        TreeNode? node = _folderTree.SelectedNode;
        if (node is null || node.Parent is null) return;
        node.BeginEdit();
    }

    private void OnDeleteFolder(object? sender, EventArgs e)
    {
        TreeNode? node = _folderTree.SelectedNode;
        if (node?.Tag is not string path) return;
        if (node.Parent is null) return;

        if (MessageBox.Show(this, $"Delete folder '{node.Text}' and all its contents?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            Directory.Delete(path, recursive: true);
            TreeNode parentNode = node.Parent;
            string parentPath   = parentNode.Tag as string ?? _contentRoot;
            node.Remove();
            _folderTree.SelectedNode = parentNode;
            if (_currentFolder.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                _currentFolder = parentPath;
            ShowFolderContents(parentPath);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFolderAfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        if (e.CancelEdit || string.IsNullOrWhiteSpace(e.Label)) { e.CancelEdit = true; return; }
        if (e.Node?.Tag is not string oldPath) { e.CancelEdit = true; return; }
        if (e.Node.Parent is null) { e.CancelEdit = true; return; }

        string newName   = e.Label.Trim();
        string parentDir = Path.GetDirectoryName(oldPath) ?? string.Empty;
        string newPath   = Path.Combine(parentDir, newName);

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) return;

        if (Directory.Exists(newPath))
        {
            e.CancelEdit = true;
            MessageBox.Show(this, $"A folder named '{newName}' already exists.", "Rename Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Directory.Move(oldPath, newPath);
            e.Node.Tag = newPath;
            UpdateChildNodePaths(e.Node, oldPath, newPath);

            if (_currentFolder.StartsWith(oldPath, StringComparison.OrdinalIgnoreCase))
            {
                _currentFolder = newPath + _currentFolder[oldPath.Length..];
                BeginInvoke(() => ShowFolderContents(_currentFolder));
            }
        }
        catch (IOException ex)
        {
            e.CancelEdit = true;
            MessageBox.Show(this, ex.Message, "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Content list

    private void ShowFolderContents(string folderPath)
    {
        _currentFolder = folderPath;
        UpdateBreadcrumb(folderPath);

        string filter = _filterBox.Text?.Trim() ?? string.Empty;

        _contentView.BeginUpdate();
        _contentView.Items.Clear();
        ClearPreview();

        try
        {
            string[] files = Directory.GetFiles(folderPath);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                AssetInfo info = AssetClassifier.CreateInfo(files[i], _contentRoot);
                if (!string.IsNullOrEmpty(filter) && !info.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                ListViewItem item = new(info.Name)
                {
                    Tag         = info,
                    ToolTipText = info.RelativePath,
                };

                bool isGenerated = info.Name.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase)
                    || (info.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                        && info.Name.Contains(".Generated", StringComparison.OrdinalIgnoreCase));

                if (isGenerated)
                {
                    item.ImageIndex  = IconGenerated;
                    item.ForeColor   = System.Drawing.Color.Turquoise;
                    item.ToolTipText = "Auto-generated by MonoGame Editor — do not edit manually";
                }
                else
                {
                    item.ImageIndex = TypeToIconIndex(info.Type);
                }
                item.SubItems.Add(TypeLabel(info.Type));
                item.SubItems.Add(FormatSize(info.SizeBytes));
                _contentView.Items.Add(item);
            }
        }
        catch (UnauthorizedAccessException) { }

        _contentView.EndUpdate();
    }

    private void OnContentSelectionChanged(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count == 0)
        {
            _rightSplit.Panel2Collapsed = true;
            ClearPreview();
            _context?.EventBus.Publish(new AssetSelectedEvent(null));
            return;
        }

        if (_contentView.SelectedItems[0].Tag is not AssetInfo info)
        {
            _rightSplit.Panel2Collapsed = true;
            ClearPreview();
            _context?.EventBus.Publish(new AssetSelectedEvent(null));
            return;
        }

        _rightSplit.Panel2Collapsed = false;
        ShowPreview(info);
        _context?.EventBus.Publish(new AssetSelectedEvent(info));
    }

    private void OnFileAfterLabelEdit(object? sender, LabelEditEventArgs e)
    {
        if (e.CancelEdit || string.IsNullOrWhiteSpace(e.Label)) { e.CancelEdit = true; return; }
        if (_contentView.Items[e.Item].Tag is not AssetInfo info) { e.CancelEdit = true; return; }

        string newName = e.Label.Trim();
        string dir     = Path.GetDirectoryName(info.AbsolutePath) ?? string.Empty;
        string newPath = Path.Combine(dir, newName);

        if (string.Equals(info.AbsolutePath, newPath, StringComparison.OrdinalIgnoreCase)) return;

        if (File.Exists(newPath))
        {
            e.CancelEdit = true;
            MessageBox.Show(this, $"A file named '{newName}' already exists.", "Rename Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            File.Move(info.AbsolutePath, newPath);
            BeginInvoke(() => ShowFolderContents(_currentFolder));
        }
        catch (IOException ex)
        {
            e.CancelEdit = true;
            MessageBox.Show(this, ex.Message, "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Preview

    private void ShowPreview(AssetInfo info)
    {
        _previewInfo.Text = BuildPreviewText(info);

        if (info.Type == AssetType.Texture && File.Exists(info.AbsolutePath))
        {
            try
            {
                _previewImage.Image?.Dispose();
                _previewImage.Image = System.Drawing.Image.FromFile(info.AbsolutePath);

                string dims = string.Empty;
                if (_previewImage.Image is System.Drawing.Bitmap bmp)
                    dims = $"\nDimensions: {bmp.Width} × {bmp.Height} px";

                _previewInfo.Text = BuildPreviewText(info) + dims;
            }
            catch
            {
                _previewImage.Image = null;
            }
        }
        else
        {
            _previewImage.Image?.Dispose();
            _previewImage.Image = null;
        }
    }

    private void ClearPreview()
    {
        _previewImage.Image?.Dispose();
        _previewImage.Image = null;
        _previewInfo.Text   = string.Empty;
    }

    private static string BuildPreviewText(AssetInfo info)
        => $"{info.Name}{info.Extension}\nType: {TypeLabel(info.Type)}\nSize: {FormatSize(info.SizeBytes)}\n{info.RelativePath}";

    #endregion

    #region Drag & drop — from ListView to other panels / viewport

    private void OnItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is ListViewItem { Tag: AssetInfo info })
            _contentView.DoDragDrop(info.AbsolutePath, DragDropEffects.Copy);
    }

    #endregion

    #region Drag & drop — from Windows Explorer into panel

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] droppedFiles) return;
        if (string.IsNullOrEmpty(_contentRoot)) return;

        string targetDir = _folderTree.SelectedNode?.Tag as string ?? _contentRoot;

        for (int i = 0; i < droppedFiles.Length; i++)
        {
            string src = droppedFiles[i];
            if (!File.Exists(src)) continue;

            string dest = Path.Combine(targetDir, Path.GetFileName(src));
            try
            {
                File.Copy(src, dest, overwrite: true);
                AssetInfo info = AssetClassifier.CreateInfo(dest, _contentRoot);
                _context?.EventBus.Publish(new AssetImportedEvent(info));
            }
            catch (IOException) { }
        }

        ShowFolderContents(targetDir);
    }

    #endregion

    #region Helpers

    private void UpdateBreadcrumb(string folderPath)
    {
        _breadcrumb.Controls.Clear();
        if (string.IsNullOrEmpty(_contentRoot)) return;

        bool insideRoot = folderPath.StartsWith(_contentRoot, StringComparison.OrdinalIgnoreCase);
        string rootName = Path.GetFileName(_contentRoot);
        if (string.IsNullOrEmpty(rootName)) rootName = _contentRoot;

        AddBreadcrumbLink(rootName, _contentRoot);

        if (!insideRoot) return;

        string relative = folderPath[_contentRoot.Length..].TrimStart(Path.DirectorySeparatorChar);
        if (string.IsNullOrEmpty(relative)) return;

        string[] segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        string accumulated = _contentRoot;
        for (int i = 0; i < segments.Length; i++)
        {
            accumulated = Path.Combine(accumulated, segments[i]);
            Label sep = new Label { Text = "›", AutoSize = true, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };
            _breadcrumb.Controls.Add(sep);
            string captured = accumulated;
            AddBreadcrumbLink(segments[i], captured);
        }
    }

    private void AddBreadcrumbLink(string text, string path)
    {
        LinkLabel lnk = new LinkLabel
        {
            Text             = text,
            AutoSize         = true,
            Tag              = path,
            TextAlign        = System.Drawing.ContentAlignment.MiddleLeft,
            LinkColor        = System.Drawing.Color.FromArgb(180, 180, 180),
            VisitedLinkColor = System.Drawing.Color.FromArgb(140, 140, 140),
            ActiveLinkColor  = System.Drawing.Color.White,
        };
        lnk.LinkClicked += (_, _) =>
        {
            if (lnk.Tag is not string target) return;
            TreeNode? node = FindFolderNode(_folderTree.Nodes, target);
            if (node is not null)
            {
                _folderTree.SelectedNode = node;
                ShowFolderContents(target);
            }
        };
        _breadcrumb.Controls.Add(lnk);
    }

    private static TreeNode? FindFolderNode(TreeNodeCollection nodes, string path)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Tag is string p && string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
                return nodes[i];
            TreeNode? found = FindFolderNode(nodes[i].Nodes, path);
            if (found is not null) return found;
        }
        return null;
    }

    private static void UpdateChildNodePaths(TreeNode node, string oldBase, string newBase)
    {
        for (int i = 0; i < node.Nodes.Count; i++)
        {
            if (node.Nodes[i].Tag is string childPath)
            {
                string newChildPath = newBase + childPath[oldBase.Length..];
                node.Nodes[i].Tag = newChildPath;
                UpdateChildNodePaths(node.Nodes[i], childPath, newChildPath);
            }
        }
    }

    private void OnViewToggle(object? sender, EventArgs e)
    {
        _largeIconMode = !_largeIconMode;
        _contentView.View   = _largeIconMode ? View.LargeIcon : View.Details;
        _viewToggleBtn.Text = _largeIconMode ? "☰" : "⊞";
        if (!string.IsNullOrEmpty(_currentFolder))
            ShowFolderContents(_currentFolder);
    }

    private void OnOpenExternal(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count == 0) return;
        if (_contentView.SelectedItems[0].Tag is not AssetInfo info) return;
        if (!File.Exists(info.AbsolutePath)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = info.AbsolutePath,
            UseShellExecute = true,
        });
    }

    private void OnRevealInExplorer(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count == 0) return;
        if (_contentView.SelectedItems[0].Tag is not AssetInfo info) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "explorer.exe",
            Arguments       = $"/select,\"{info.AbsolutePath}\"",
            UseShellExecute = true,
        });
    }

    private void OnRenameItem(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count > 0)
            _contentView.SelectedItems[0].BeginEdit();
    }

    private void OnDeleteItem(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count == 0) return;
        if (_contentView.SelectedItems[0].Tag is not AssetInfo info) return;
        if (MessageBox.Show(this, $"Delete '{info.Name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            File.Delete(info.AbsolutePath);
            ShowFolderContents(_currentFolder);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnCopyRelativePath(object? sender, EventArgs e)
    {
        if (_contentView.SelectedItems.Count == 0) return;
        if (_contentView.SelectedItems[0].Tag is not AssetInfo info) return;
        Clipboard.SetText(info.RelativePath);
    }

    private static int TypeToIconIndex(AssetType type) => type switch
    {
        AssetType.Texture   => IconTexture,
        AssetType.Audio     => IconAudio,
        AssetType.Font      => IconFont,
        AssetType.TiledMap  => IconTiledMap,
        AssetType.Scene     => IconScene,
        AssetType.Prefab    => IconPrefab,
        AssetType.Particles => IconParticles,
        AssetType.Animation => IconAnimation,
        AssetType.InputMap  => IconInputMap,
        AssetType.Script    => IconScript,
        AssetType.Material  => IconMaterial,
        AssetType.Sprite    => IconSprite,
        AssetType.UITheme   => IconUITheme,
        _                   => IconUnknown,
    };

    private static string TypeLabel(AssetType type) => type switch
    {
        AssetType.Texture   => "Texture",
        AssetType.Audio     => "Audio",
        AssetType.Font      => "Font",
        AssetType.TiledMap  => "Tiled Map",
        AssetType.Scene     => "Scene",
        AssetType.Prefab    => "Prefab",
        AssetType.Particles => "Particles",
        AssetType.Animation => "Animation",
        AssetType.InputMap  => "Input Map",
        AssetType.Script    => "Script",
        AssetType.Material  => "Material",
        AssetType.Sprite    => "Sprite",
        AssetType.UITheme   => "UI Theme",
        _                   => "Unknown",
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024     => $"{bytes / 1_024.0:F1} KB",
        _            => $"{bytes} B",
    };

    /// <summary>Construye la lista de imágenes de iconos con cuadros de color por tipo de asset.</summary>
    private static ImageList BuildIconList(int size)
    {
        ImageList list = new() { ImageSize = new System.Drawing.Size(size, size) };
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Gray, size));           // 0 Desconocido
        list.Images.Add(MakeColorSquare(System.Drawing.Color.MediumPurple, size));   // 1 Texture
        list.Images.Add(MakeColorSquare(System.Drawing.Color.DodgerBlue, size));     // 2 Audio
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Goldenrod, size));      // 3 Fuente
        list.Images.Add(MakeColorSquare(System.Drawing.Color.ForestGreen, size));    // 4 TiledMap
        list.Images.Add(MakeColorSquare(System.Drawing.Color.CornflowerBlue, size)); // 5 Escena
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Orange, size));         // 6 Prefab
        list.Images.Add(MakeColorSquare(System.Drawing.Color.HotPink, size));        // 7 Partículas
        list.Images.Add(MakeColorSquare(System.Drawing.Color.LimeGreen, size));      // 8 Animación
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Tomato, size));         // 9 InputMap
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Teal, size));           // 10 Script
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SaddleBrown, size));    // 11 Carpeta
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Turquoise, size));      // 12 Generado
        list.Images.Add(MakeColorSquare(System.Drawing.Color.IndianRed, size));       // 13 Material
        list.Images.Add(MakeColorSquare(System.Drawing.Color.MediumOrchid, size));    // 14 Sprite
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SteelBlue, size));       // 15 UITheme
        return list;
    }

    private static ImageList BuildLargeIconList()
    {
        ImageList list = new() { ImageSize = new System.Drawing.Size(64, 64) };
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Gray, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.MediumPurple, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.DodgerBlue, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Goldenrod, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.ForestGreen, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.CornflowerBlue, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Orange, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.HotPink, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.LimeGreen, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Tomato, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Teal, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SaddleBrown, 64));
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Turquoise, 64));        // 12 Generado
        list.Images.Add(MakeColorSquare(System.Drawing.Color.IndianRed, 64));        // 13 Material
        list.Images.Add(MakeColorSquare(System.Drawing.Color.MediumOrchid, 64));     // 14 Sprite
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SteelBlue, 64));        // 15 UITheme
        return list;
    }

    private static System.Drawing.Bitmap MakeColorSquare(System.Drawing.Color color, int size)
    {
        System.Drawing.Bitmap bmp = new(size, size);
        using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(color);
        return bmp;
    }

    #endregion

    #region Dispose

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_context is not null)
            {
                _context.EventBus.Unsubscribe<AssetImportedEvent>(OnAssetImported);
                _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
            }
            _previewImage.Image?.Dispose();
            _typeIcons.Dispose();
            _largeIcons.Dispose();
            _searchDebounce.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
