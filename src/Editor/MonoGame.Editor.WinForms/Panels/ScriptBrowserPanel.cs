using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Core.Project;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Navegador de scripts de dos paneles: árbol de carpetas a la izquierda, lista de archivos <c>.cs</c> a la derecha.
/// Se integra con el bus de eventos del editor para recargar cuando se abre un proyecto.
/// </summary>
public sealed class ScriptBrowserPanel : UserControl
{
    #region Icon indices

    private const int IconScript = 0;
    private const int IconFolder = 1;

    #endregion

    #region Fields

    private EditorContext? _context;
    private string _scriptsRoot   = string.Empty;
    private string _currentFolder = string.Empty;

    private readonly SplitContainer   _splitContainer;
    private readonly TreeView         _folderTree;
    private readonly ListView         _fileList;
    private readonly Button           _newScriptButton;
    private readonly ImageList        _icons;
    private readonly ContextMenuStrip _folderContextMenu;
    private readonly ContextMenuStrip _fileContextMenu;
    private readonly ToolStripMenuItem _renameFolderItem;
    private readonly ToolStripMenuItem _deleteFolderItem;

    #endregion

    #region Constructor

    /// <summary>Inicializa el panel y construye la interfaz de usuario.</summary>
    public ScriptBrowserPanel()
    {
        _icons = BuildIconList();

        // ── Menú contextual de carpeta ─────────────────────────────────────
        ToolStripMenuItem newFolderItem = new("New Folder");
        _renameFolderItem               = new("Rename");
        _deleteFolderItem               = new("Delete");

        _folderContextMenu = new ContextMenuStrip();
        _folderContextMenu.Items.AddRange(new ToolStripItem[]
        {
            newFolderItem,
            new ToolStripSeparator(),
            _renameFolderItem,
            _deleteFolderItem,
        });

        newFolderItem.Click        += OnNewFolder;
        _renameFolderItem.Click    += OnRenameFolder;
        _deleteFolderItem.Click    += OnDeleteFolder;
        _folderContextMenu.Opening += OnFolderContextMenuOpening;

        // ── Menú contextual de archivo ─────────────────────────────────────
        ToolStripMenuItem openInEditorItem = new("Open in External Editor");
        ToolStripMenuItem renameFileItem   = new("Rename");
        ToolStripMenuItem deleteFileItem   = new("Delete");
        ToolStripMenuItem copyPathItem     = new("Copy Path");

        _fileContextMenu = new ContextMenuStrip();
        _fileContextMenu.Items.AddRange(new ToolStripItem[]
        {
            openInEditorItem,
            new ToolStripSeparator(),
            renameFileItem,
            deleteFileItem,
            new ToolStripSeparator(),
            copyPathItem,
        });

        openInEditorItem.Click   += OnOpenInEditor;
        renameFileItem.Click     += OnRenameFile;
        deleteFileItem.Click     += OnDeleteFile;
        copyPathItem.Click       += OnCopyPath;
        _fileContextMenu.Opening += OnFileContextMenuOpening;

        // ── Árbol de carpetas (izquierda) ─────────────────────────────────
        _folderTree = new TreeView
        {
            Dock             = DockStyle.Fill,
            HideSelection    = false,
            ShowLines        = true,
            ShowPlusMinus    = true,
            BorderStyle      = BorderStyle.None,
            ImageList        = _icons,
            LabelEdit        = true,
            ContextMenuStrip = _folderContextMenu,
        };

        // ── Lista de archivos (derecha) ───────────────────────────────────
        _fileList = new ListView
        {
            Dock             = DockStyle.Fill,
            View             = View.Details,
            FullRowSelect    = true,
            MultiSelect      = false,
            BorderStyle      = BorderStyle.None,
            SmallImageList   = _icons,
            LabelEdit        = true,
            ContextMenuStrip = _fileContextMenu,
        };
        _fileList.Columns.Add("Name", 220);
        _fileList.Columns.Add("Size", 70);

        // ── Botón Nuevo Script ────────────────────────────────────────────
        _newScriptButton = new Button
        {
            Text      = "New Script",
            Dock      = DockStyle.Bottom,
            Height    = 28,
            FlatStyle = FlatStyle.Flat,
        };

        // ── Panel derecho ──────────────────────────────────────────────────
        Panel rightPanel = new Panel { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(_fileList);
        rightPanel.Controls.Add(_newScriptButton);

        // ── Contenedor dividido ────────────────────────────────────────────
        _splitContainer = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Vertical,
            SplitterDistance = 180,
            Panel1MinSize    = 80,
            Panel2MinSize    = 80,
        };
        _splitContainer.Panel1.Controls.Add(_folderTree);
        _splitContainer.Panel2.Controls.Add(rightPanel);

        Controls.Add(_splitContainer);

        _folderTree.AfterSelect    += OnFolderSelected;
        _folderTree.BeforeExpand   += OnBeforeExpand;
        _folderTree.MouseDown      += OnFolderTreeMouseDown;
        _folderTree.AfterLabelEdit += OnFolderAfterLabelEdit;
        _fileList.ItemActivate     += OnFileActivate;
        _fileList.AfterLabelEdit   += OnFileAfterLabelEdit;
        _newScriptButton.Click     += OnNewScriptClick;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Se suscribe al bus de eventos para recargar cuando se abre un proyecto.
    /// Debe llamarse una vez después de que el formulario padre haya sido construido.
    /// </summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    #endregion

    #region Event bus handlers

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }

        if (evt.Project is null)
        {
            _scriptsRoot = string.Empty;
            _folderTree.Nodes.Clear();
            _fileList.Items.Clear();
            return;
        }

        _scriptsRoot = evt.Project.GameScriptsPath;
        LoadFolderTree(_scriptsRoot);
    }

    #endregion

    #region Folder tree

    private void LoadFolderTree(string root)
    {
        _folderTree.BeginUpdate();
        _folderTree.Nodes.Clear();
        _fileList.Items.Clear();
        _currentFolder = string.Empty;

        if (!Directory.Exists(root))
        {
            _folderTree.EndUpdate();
            return;
        }

        string name = Path.GetFileName(root);
        if (string.IsNullOrEmpty(name)) name = root;

        TreeNode rootNode = CreateFolderNode(name, root);
        _folderTree.Nodes.Add(rootNode);
        rootNode.Expand();

        _folderTree.EndUpdate();
        _folderTree.SelectedNode = rootNode;
    }

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
                node.Nodes.Add(new TreeNode()); // marcador de posición diferido
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
                string dirName = Path.GetFileName(dirs[i]);
                if (!string.IsNullOrEmpty(dirName))
                    e.Node.Nodes.Add(CreateFolderNode(dirName, dirs[i]));
            }
        }
        catch (UnauthorizedAccessException) { }
        _folderTree.EndUpdate();
    }

    private void OnFolderSelected(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not string path) return;
        _currentFolder = path;
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
            string parentPath   = parentNode.Tag as string ?? _scriptsRoot;
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

    #endregion

    #region File list

    private void ShowFolderContents(string folderPath)
    {
        _fileList.BeginUpdate();
        _fileList.Items.Clear();

        try
        {
            string[] files = Directory.GetFiles(folderPath, "*.cs");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fi  = new(files[i]);
                ListViewItem item = new(fi.Name)
                {
                    Tag        = files[i],
                    ImageIndex = IconScript,
                };
                item.SubItems.Add(FormatSize(fi.Length));
                _fileList.Items.Add(item);
            }
        }
        catch (UnauthorizedAccessException) { }

        _fileList.EndUpdate();
    }

    #endregion

    #region File context menu

    private void OnFileContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_fileList.SelectedItems.Count == 0) e.Cancel = true;
    }

    private void OnFileActivate(object? sender, EventArgs e)
    {
        if (_fileList.SelectedItems.Count == 0) return;
        string path = _fileList.SelectedItems[0].Tag as string ?? string.Empty;
        OpenScriptFile(path);
    }

    private void OnOpenInEditor(object? sender, EventArgs e)
    {
        if (_fileList.SelectedItems.Count == 0) return;
        string path = _fileList.SelectedItems[0].Tag as string ?? string.Empty;
        OpenScriptFile(path);
    }

    private void OnRenameFile(object? sender, EventArgs e)
    {
        if (_fileList.SelectedItems.Count > 0)
            _fileList.SelectedItems[0].BeginEdit();
    }

    private void OnDeleteFile(object? sender, EventArgs e)
    {
        if (_fileList.SelectedItems.Count == 0) return;
        string path = _fileList.SelectedItems[0].Tag as string ?? string.Empty;
        string name = Path.GetFileName(path);

        if (MessageBox.Show(this, $"Delete '{name}'?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            File.Delete(path);
            ShowFolderContents(_currentFolder);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, ex.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnCopyPath(object? sender, EventArgs e)
    {
        if (_fileList.SelectedItems.Count == 0) return;
        string path = _fileList.SelectedItems[0].Tag as string ?? string.Empty;
        if (!string.IsNullOrEmpty(path))
            Clipboard.SetText(path);
    }

    private void OnFileAfterLabelEdit(object? sender, LabelEditEventArgs e)
    {
        if (e.CancelEdit || string.IsNullOrWhiteSpace(e.Label)) { e.CancelEdit = true; return; }
        string oldPath = _fileList.Items[e.Item].Tag as string ?? string.Empty;
        if (string.IsNullOrEmpty(oldPath)) { e.CancelEdit = true; return; }

        string newName = e.Label.Trim();
        string dir     = Path.GetDirectoryName(oldPath) ?? string.Empty;
        string newPath = Path.Combine(dir, newName);

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) return;

        if (File.Exists(newPath))
        {
            e.CancelEdit = true;
            MessageBox.Show(this, $"A file named '{newName}' already exists.", "Rename Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            File.Move(oldPath, newPath);
            BeginInvoke(() => ShowFolderContents(_currentFolder));
        }
        catch (IOException ex)
        {
            e.CancelEdit = true;
            MessageBox.Show(this, ex.Message, "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region New Script button

    private void OnNewScriptClick(object? sender, EventArgs e)
    {
        string targetDir = string.IsNullOrEmpty(_currentFolder) ? _scriptsRoot : _currentFolder;
        if (string.IsNullOrEmpty(targetDir)) return;

        using ScriptCreationDialog dlg = new(targetDir);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            ShowFolderContents(targetDir);
    }

    #endregion

    #region Helpers

    private static void OpenScriptFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        // Intentar VS Code (PATH o ubicaciones de instalación comunes) — evita el diálogo "Abrir con"
        if (TryOpenWithCode(filePath)) return;

        // Usar el controlador predeterminado del sistema (Visual Studio, Rider, etc. si está registrado)
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = filePath,
                UseShellExecute = true,
            });
        }
        catch
        {
            System.Diagnostics.Process.Start("notepad.exe", $"\"{filePath}\"");
        }
    }

    private static bool TryOpenWithCode(string filePath)
    {
        // Intentar "code" desde PATH primero
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "code",
                Arguments       = $"\"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow  = true,
            });
            return true;
        }
        catch { }

        // Intentar ubicaciones de instalación comunes de VS Code
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string[] candidates =
        {
            Path.Combine(localAppData, "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(programFiles, "Microsoft VS Code", "Code.exe"),
        };

        foreach (string candidate in candidates)
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = candidate,
                    Arguments       = $"\"{filePath}\"",
                    UseShellExecute = false,
                });
                return true;
            }
            catch { }
        }

        return false;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024     => $"{bytes / 1_024.0:F1} KB",
        _            => $"{bytes} B",
    };

    private static ImageList BuildIconList()
    {
        ImageList list = new() { ImageSize = new System.Drawing.Size(16, 16) };
        list.Images.Add(MakeColorSquare(System.Drawing.Color.Teal, 16));         // 0 Script
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SaddleBrown, 16)); // 1 Carpeta
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
                _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
            _icons.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
