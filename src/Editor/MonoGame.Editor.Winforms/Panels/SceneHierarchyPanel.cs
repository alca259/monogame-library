using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;
using MonoGame.Editor.Winforms.ViewModels.Panels;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel de jerarquía de escena: TreeView con búsqueda y comandos de entidad.
/// Se comunica con el viewport y el inspector a través del <see cref="EditorContext"/> y
/// el bus de eventos, nunca directamente.
/// </summary>
internal sealed class SceneHierarchyPanel : UserControl
{
    private readonly SceneHierarchyViewModel _vm = new();

    // ── Controles ─────────────────────────────────────────────────────────────

    private readonly TextBox      _searchBox;
    private readonly Button       _btnAdd;
    private readonly Button       _btnDelete;
    private readonly Button       _btnRename;
    private readonly Button       _btnReparent;
    private readonly Label        _statusLabel;
    private readonly TreeView     _treeView;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SceneHierarchyPanel()
    {
        SuspendLayout();

        // Toolbar superior
        Panel toolbar = new()
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2),
        };

        _searchBox = new TextBox
        {
            PlaceholderText = "Search…",
            Dock            = DockStyle.Fill,
            BackColor       = EditorColors.InputBackground,
            ForeColor       = EditorColors.TextPrimary,
            Font            = EditorFonts.Primary,
            BorderStyle     = BorderStyle.FixedSingle,
        };

        Panel rightBtns = new()
        {
            Dock      = DockStyle.Right,
            Width     = 106,
            BackColor = EditorColors.PanelBackgroundAlt,
        };

        _btnAdd      = MakeToolBtn("+",  "Add entity");
        _btnDelete   = MakeToolBtn("🗑", "Delete entity");
        _btnRename   = MakeToolBtn("✎", "Rename entity");
        _btnReparent = MakeToolBtn("↑",  "Reparent entity");

        _btnAdd.Left      = 0;
        _btnDelete.Left   = 26;
        _btnRename.Left   = 52;
        _btnReparent.Left = 78;

        rightBtns.Controls.AddRange([_btnAdd, _btnDelete, _btnRename, _btnReparent]);
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(rightBtns);

        // Etiqueta de estado (bottom)
        _statusLabel = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
            Text      = "0 objects in scene",
        };

        // TreeView
        _treeView = new TreeView
        {
            Dock            = DockStyle.Fill,
            BackColor       = EditorColors.PanelBackground,
            ForeColor       = EditorColors.TextPrimary,
            Font            = EditorFonts.Primary,
            BorderStyle     = BorderStyle.None,
            HideSelection   = false,
            FullRowSelect   = true,
            ShowLines       = true,
            ShowRootLines   = true,
            AllowDrop       = true,
            ShowNodeToolTips = true,
        };

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;
        Controls.AddRange([_treeView, toolbar, _statusLabel]);

        // Eventos
        _searchBox.TextChanged += (_, _) => _vm.SearchFilter = _searchBox.Text;

        _btnAdd.Click      += (_, _) => _vm.AddEntity();
        _btnDelete.Click   += (_, _) => _vm.DeleteEntity();
        _btnRename.Click   += async (_, _) => { if (EditorContext.Instance.SelectedObject is { } o) await _vm.RenameAsync(o); };
        _btnReparent.Click += async (_, _) => await _vm.ReparentAsync();

        _treeView.AfterSelect    += OnAfterSelect;
        _treeView.ItemDrag       += OnItemDrag;
        _treeView.DragEnter      += OnDragEnter;
        _treeView.DragOver       += OnDragOver;
        _treeView.DragDrop       += OnDragDrop;
        _treeView.MouseDown      += (_, _) => EditorContext.Instance.SetFocus(EditorFocusContext.Hierarchy);

        // VM
        _vm.RebuildRequested         += RebuildTree;
        _vm.SelectionSyncRequested   += SyncSelection;
        _vm.PropertyChanged          += OnVmPropertyChanged;

        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Construcción del árbol ─────────────────────────────────────────────────

    private void RebuildTree()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(RebuildTree); return; }

        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is not null)
        {
            bool filtering = !string.IsNullOrEmpty(_vm.SearchFilter);
            foreach (EditorGameObject root in scene.RootGameObjects)
                AddNode(_treeView.Nodes, root, filtering);
        }

        _treeView.EndUpdate();
        _vm.UpdateSceneState();
        _statusLabel.Text = _vm.StatusText;
        RefreshButtonStates();

        SyncSelection(EditorContext.Instance.SelectedObject);
    }

    private void AddNode(TreeNodeCollection parent, EditorGameObject obj, bool filtering)
    {
        if (filtering && !MatchesFilter(obj, _vm.SearchFilter)) return;

        TreeNode node = new(obj.Name) { Tag = obj };
        ApplyNodeStyle(node, obj);

        bool expanded = filtering || _vm.ExpandedIds.Contains(obj.Id);
        parent.Add(node);

        foreach (EditorGameObject child in obj.Children)
            AddNode(node.Nodes, child, filtering);

        if (expanded && node.Nodes.Count > 0)
            node.Expand();
    }

    private static void ApplyNodeStyle(TreeNode node, EditorGameObject obj)
    {
        node.ForeColor = obj.Active
            ? (obj.PrefabPath is not null ? Color.FromArgb(0x7E, 0xB8, 0xF7) : EditorColors.TextPrimary)
            : EditorColors.TextMuted;
    }

    private static bool MatchesFilter(EditorGameObject obj, string filter)
    {
        if (obj.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (EditorGameObject child in obj.Children)
            if (MatchesFilter(child, filter)) return true;
        return false;
    }

    // ── Sincronización de selección ───────────────────────────────────────────

    private void SyncSelection(EditorGameObject? obj)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => SyncSelection(obj)); return; }

        _vm.IsSyncingSelection = true;
        _treeView.SelectedNode = obj is not null ? FindNode(_treeView.Nodes, obj) : null;
        _vm.IsSyncingSelection = false;

        _vm.HasSelection = _treeView.SelectedNode is not null;
        RefreshButtonStates();
    }

    private static TreeNode? FindNode(TreeNodeCollection nodes, EditorGameObject obj)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is EditorGameObject go && go.Id == obj.Id) return node;
            TreeNode? child = FindNode(node.Nodes, obj);
            if (child is not null) return child;
        }
        return null;
    }

    // ── Eventos del TreeView ──────────────────────────────────────────────────

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        EditorGameObject? obj = e.Node?.Tag as EditorGameObject;
        _vm.OnNodeSelected(obj);
        _vm.HasSelection = obj is not null;
        RefreshButtonStates();

        if (e.Node is not null)
        {
            // Actualizar tabla de expansión
            if (e.Node.IsExpanded)
            {
                if (obj is not null) _vm.ExpandedIds.Add(obj.Id);
            }
        }
    }

    private void OnItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode node && node.Tag is EditorGameObject obj)
        {
            _vm.StartDrag(obj);
            _treeView.DoDragDrop(obj, DragDropEffects.Move);
        }
    }

    private static void OnDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = DragDropEffects.Move;

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        Point pt = _treeView.PointToClient(new Point(e.X, e.Y));
        TreeNode? node = _treeView.GetNodeAt(pt);
        if (node is not null) _treeView.SelectedNode = node;
        e.Effect = DragDropEffects.Move;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        Point pt = _treeView.PointToClient(new Point(e.X, e.Y));
        TreeNode? node = _treeView.GetNodeAt(pt);
        if (node?.Tag is EditorGameObject target)
            _vm.HandleDrop(target);
    }

    // ── PropertyChanged ───────────────────────────────────────────────────────

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => OnVmPropertyChanged(sender, e)); return; }

        if (e.PropertyName is nameof(SceneHierarchyViewModel.StatusText))
            _statusLabel.Text = _vm.StatusText;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshButtonStates()
    {
        _btnDelete.Enabled   = _vm.CanDelete;
        _btnRename.Enabled   = _vm.CanRename;
        _btnReparent.Enabled = _vm.CanReparent;
    }

    private static Button MakeToolBtn(string text, string tooltip)
    {
        Button btn = new()
        {
            Text      = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.Primary,
            Size      = new Size(24, 24),
            Top       = 3,
            TabStop   = false,
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}
