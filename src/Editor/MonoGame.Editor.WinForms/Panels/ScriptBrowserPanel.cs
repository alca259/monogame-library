using MonoGame.Editor.Core.Events;
using MonoGame.Editor.Core.Project;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Two-pane script browser: folder tree on the left, <c>.cs</c> file list on the right.
/// Integrates with the editor event bus to reload when a project opens.
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

    private readonly SplitContainer _splitContainer;
    private readonly TreeView       _folderTree;
    private readonly ListView       _fileList;
    private readonly Button         _newScriptButton;
    private readonly ImageList      _icons;

    #endregion

    #region Constructor

    /// <summary>Initializes the panel and builds the UI.</summary>
    public ScriptBrowserPanel()
    {
        _icons = BuildIconList();

        // ── Folder tree (left) ────────────────────────────────────────────
        _folderTree = new TreeView
        {
            Dock          = DockStyle.Fill,
            HideSelection = false,
            ShowLines     = true,
            ShowPlusMinus = true,
            BorderStyle   = BorderStyle.None,
            ImageList     = _icons,
        };

        // ── File list (right) ─────────────────────────────────────────────
        _fileList = new ListView
        {
            Dock           = DockStyle.Fill,
            View           = View.Details,
            FullRowSelect  = true,
            MultiSelect    = false,
            BorderStyle    = BorderStyle.None,
            SmallImageList = _icons,
        };
        _fileList.Columns.Add("Name", 220);
        _fileList.Columns.Add("Size", 70);

        // ── New Script button ─────────────────────────────────────────────
        _newScriptButton = new Button
        {
            Text      = "New Script",
            Dock      = DockStyle.Bottom,
            Height    = 28,
            FlatStyle = FlatStyle.Flat,
        };

        // ── Right panel ───────────────────────────────────────────────────
        Panel rightPanel = new Panel { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(_fileList);
        rightPanel.Controls.Add(_newScriptButton);

        // ── Split container ───────────────────────────────────────────────
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

        _folderTree.AfterSelect  += OnFolderSelected;
        _folderTree.BeforeExpand += OnBeforeExpand;
        _newScriptButton.Click   += OnNewScriptClick;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Subscribes to the event bus to reload when a project opens.
    /// Must be called once after the parent form is constructed.
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

        _scriptsRoot = Path.Combine(evt.Project.RootPath, "src", "GameScripts");
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
                FileInfo fi   = new(files[i]);
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
        list.Images.Add(MakeColorSquare(System.Drawing.Color.SaddleBrown, 16)); // 1 Folder
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
