using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Infrastructure;
using MonoGame.Editor.Winforms.Theme;
using MonoGame.Editor.Winforms.ViewModels.Panels;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>Panel de assets del DockBar inferior: árbol de carpetas + lista de assets con filtro.</summary>
internal sealed class AssetBrowserPanel : UserControl
{
    private readonly AssetBrowserViewModel _vm = new();

    private readonly TreeView  _treeView;
    private readonly ListView  _listView;
    private readonly TextBox   _txtFilter;
    private readonly Label     _lblStatus;

    private bool _rebuilding;

    public AssetBrowserPanel()
    {
        SuspendLayout();

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        ToolTip tip = new();

        // ── Toolbar superior ──────────────────────────────────────────────────
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2, 2, 2, 2),
        };

        Button btnImport    = MakeToolButton("Import",    tip, "Import file into current folder");
        Button btnNewFolder = MakeToolButton("New Folder",tip, "Create subfolder");
        Button btnRename    = MakeToolButton("Rename",    tip, "Rename selected asset or folder");
        Button btnDelete    = MakeToolButton("Delete",    tip, "Delete selected asset or folder");

        _txtFilter = new TextBox
        {
            Width       = 120,
            Dock        = DockStyle.Right,
            Font        = EditorFonts.Primary,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Filter…",
        };
        tip.SetToolTip(_txtFilter, "Filter assets by name");

        // Dock=Left: primero = más a la izquierda
        toolbar.Controls.Add(_txtFilter);
        toolbar.Controls.Add(btnDelete);
        toolbar.Controls.Add(btnRename);
        toolbar.Controls.Add(new Panel { Width = 6, Dock = DockStyle.Left, BackColor = EditorColors.PanelBackgroundAlt });
        toolbar.Controls.Add(btnNewFolder);
        toolbar.Controls.Add(btnImport);

        // ── SplitContainer: árbol de carpetas | lista de assets ────────────────
        SplitContainer split = new()
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Vertical,
            BackColor     = EditorColors.Border,
            Width = 500,
            SplitterWidth = 2,
            Panel1MinSize = 100,
            Panel2MinSize = 150,
            SplitterDistance = 130,
        };

        // ── TreeView de carpetas ──────────────────────────────────────────────
        _treeView = new TreeView
        {
            Dock         = DockStyle.Fill,
            BackColor    = EditorColors.PanelBackground,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.Primary,
            BorderStyle  = BorderStyle.None,
            ShowLines    = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            HideSelection = false,
            ItemHeight   = 20,
        };
        split.Panel1.Controls.Add(_treeView);

        // ── ListView de assets ────────────────────────────────────────────────
        _listView = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            HeaderStyle   = ColumnHeaderStyle.Clickable,
            BackColor     = EditorColors.PanelBackground,
            ForeColor     = EditorColors.TextPrimary,
            Font          = EditorFonts.Primary,
            BorderStyle   = BorderStyle.None,
            GridLines     = false,
            MultiSelect   = false,
        };
        _listView.Columns.Add("Name", 200, HorizontalAlignment.Left);
        _listView.Columns.Add("Type",  70, HorizontalAlignment.Left);
        _listView.Columns.Add("Size",  60, HorizontalAlignment.Right);
        split.Panel2.Controls.Add(_listView);

        // ── Barra de estado ────────────────────────────────────────────────────
        _lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
            Text      = "No project",
        };

        Controls.Add(split);
        Controls.Add(toolbar);
        Controls.Add(_lblStatus);

        // ── Eventos de toolbar ─────────────────────────────────────────────────
        btnImport.Click += (_, _) =>
        {
            if (!_vm.CanManage) return;
            string? src = WinFormsDialogService.PickFile(FindForm());
            if (src is null) return;
            _vm.ImportAsset(src);
        };

        btnNewFolder.Click += (_, _) =>
        {
            if (!_vm.CanManage) return;
            string? name = WinFormsDialogService.Prompt(FindForm(), "New Folder", "Folder name:");
            if (string.IsNullOrWhiteSpace(name)) return;
            _vm.NewFolder(name);
        };

        btnRename.Click += (_, _) =>
        {
            // Prioridad: asset seleccionado, luego carpeta seleccionada
            if (_listView.SelectedItems.Count > 0)
            {
                AssetInfo info = (AssetInfo)_listView.SelectedItems[0].Tag!;
                string? newName = WinFormsDialogService.Prompt(FindForm(), "Rename Asset",
                    "Enter new file name:", initialValue: info.Name);
                if (string.IsNullOrWhiteSpace(newName) || newName == info.Name) return;
                _vm.RenameAsset(info, newName);
                return;
            }

            if (_treeView.SelectedNode?.Tag is string folderPath
                && !string.Equals(folderPath, _vm.ContentRoot, StringComparison.OrdinalIgnoreCase))
            {
                string currentName = Path.GetFileName(folderPath);
                string? newName = WinFormsDialogService.Prompt(FindForm(), "Rename Folder",
                    "Enter new folder name:", initialValue: currentName);
                if (string.IsNullOrWhiteSpace(newName) || newName == currentName) return;
                _vm.RenameFolder(folderPath, newName);
            }
        };

        btnDelete.Click += (_, _) =>
        {
            if (_listView.SelectedItems.Count > 0)
            {
                AssetInfo info = (AssetInfo)_listView.SelectedItems[0].Tag!;
                bool ok = WinFormsDialogService.Confirm(FindForm(), "Delete Asset",
                    $"Delete '{info.Name}'? This cannot be undone.", "Delete", "Cancel");
                if (ok) _vm.DeleteAsset(info);
                return;
            }

            if (_treeView.SelectedNode?.Tag is string folderPath
                && !string.Equals(folderPath, _vm.ContentRoot, StringComparison.OrdinalIgnoreCase))
            {
                string folderName = Path.GetFileName(folderPath);
                bool ok = WinFormsDialogService.Confirm(FindForm(), "Delete Folder",
                    $"Delete '{folderName}' and all its contents? This cannot be undone.", "Delete", "Cancel");
                if (ok) _vm.DeleteFolder(folderPath);
            }
        };

        // ── Eventos de TreeView ────────────────────────────────────────────────
        _treeView.BeforeExpand += (_, e) =>
        {
            if (_rebuilding) return;
            e.Cancel = true;
            if (e.Node?.Tag is string path) _vm.ToggleFolderExpansion(path);
        };

        _treeView.BeforeCollapse += (_, e) =>
        {
            if (_rebuilding) return;
            e.Cancel = true;
            if (e.Node?.Tag is string path) _vm.ToggleFolderExpansion(path);
        };

        _treeView.AfterSelect += (_, e) =>
        {
            if (_rebuilding) return;
            if (e.Node?.Tag is string path) _vm.SelectFolder(path);
        };

        // ── Eventos de ListView ────────────────────────────────────────────────
        _listView.ItemSelectionChanged += (_, _) =>
        {
            if (_listView.SelectedItems.Count == 0) return;
            AssetInfo info = (AssetInfo)_listView.SelectedItems[0].Tag!;
            EditorContext.Instance.EventBus.Publish(new AssetSelectedEvent(info));
        };

        // ── Filtro ────────────────────────────────────────────────────────────
        _txtFilter.TextChanged += (_, _) => _vm.FilterText = _txtFilter.Text;

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.FolderTreeRebuildRequested += OnFolderTreeRebuild;
        _vm.AssetListRebuildRequested  += OnAssetListRebuild;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Reconstrucción del árbol de carpetas ──────────────────────────────────

    private void OnFolderTreeRebuild()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnFolderTreeRebuild); return; }

        _rebuilding = true;
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        IReadOnlyList<FolderEntry> entries = _vm.GetFolderEntries();
        Stack<(TreeNode node, int depth)> stack = new();

        foreach (FolderEntry entry in entries)
        {
            string displayName = entry.IsRoot
                ? "Content"
                : Path.GetFileName(entry.Path);

            TreeNode node = new(displayName) { Tag = entry.Path };
            node.ForeColor = entry.IsRoot ? EditorColors.AccentBlue : EditorColors.TextPrimary;

            if (stack.Count == 0)
            {
                _treeView.Nodes.Add(node);
            }
            else
            {
                while (stack.Count > 0 && stack.Peek().depth >= entry.Depth)
                    stack.Pop();

                if (stack.Count > 0) stack.Peek().node.Nodes.Add(node);
                else _treeView.Nodes.Add(node);
            }

            // Si está expandido en el VM, expandir también el nodo visual
            if (entry.IsExpanded)
                node.Expand();

            // Si tiene hijos pero no está expandido, añadir nodo dummy para mostrar el "+"
            if (entry.HasChildren && !entry.IsExpanded)
                node.Nodes.Add(new TreeNode("_") { Tag = string.Empty });

            stack.Push((node, entry.Depth));
        }

        // Seleccionar la carpeta actual
        SelectNodeByPath(_vm.CurrentFolder);

        _treeView.EndUpdate();
        _rebuilding = false;
    }

    private void SelectNodeByPath(string path)
    {
        TreeNode? found = FindNode(_treeView.Nodes, path);
        if (found is not null) _treeView.SelectedNode = found;
    }

    private static TreeNode? FindNode(TreeNodeCollection nodes, string path)
    {
        foreach (TreeNode n in nodes)
        {
            if (n.Tag is string p && string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
                return n;
            TreeNode? found = FindNode(n.Nodes, path);
            if (found is not null) return found;
        }
        return null;
    }

    // ── Reconstrucción de la lista de assets ──────────────────────────────────

    private void OnAssetListRebuild()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnAssetListRebuild); return; }

        _listView.BeginUpdate();
        _listView.Items.Clear();

        IReadOnlyList<AssetInfo> items = _vm.GetAssetItems();
        foreach (AssetInfo info in items)
        {
            ListViewItem item = new(info.Name) { Tag = info };
            item.SubItems.Add(info.Type.ToString());
            item.SubItems.Add(FormatSize(info.SizeBytes));
            _listView.Items.Add(item);
        }

        _listView.EndUpdate();
        _lblStatus.Text = items.Count == 1 ? "1 asset" : $"{items.Count} assets";
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)        return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / (1024 * 1024)} MB";
    }

    private static Button MakeToolButton(string text, ToolTip tip, string tooltip)
    {
        Button btn = new()
        {
            Text      = text,
            Width     = text.Length * 7 + 10,
            Dock      = DockStyle.Left,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
        };
        btn.FlatAppearance.BorderColor = EditorColors.Border;
        tip.SetToolTip(btn, tooltip);
        return btn;
    }
}
