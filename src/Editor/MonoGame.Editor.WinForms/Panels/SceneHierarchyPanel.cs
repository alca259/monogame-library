namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Muestra la jerarquía de objetos de juego de la escena en un <see cref="TreeView"/>.
/// Se suscribe a <see cref="SceneLoadedEvent"/> y <see cref="GameObjectSelectedEvent"/> a través del bus de eventos.
/// Todas las modificaciones se enrutan a través de <see cref="CommandStack"/> para soporte completo de deshacer/rehacer.
/// </summary>
public sealed class SceneHierarchyPanel : UserControl
{
    #region Fields

    private EditorContext? _context;
    private PrefabManager? _prefabManager;
    private bool _suppressSelectionEvent;
    private bool _suppressCheckEvent;
    private readonly List<EditorGameObject> _multiSelected = [];

    private readonly ToolStrip       _toolbar;
    private readonly ToolStripButton _addBtn;
    private readonly ToolStripButton _deleteBtn;
    private readonly ToolStripTextBox _searchBox;
    private readonly ToolStripLabel  _counterLabel;
    private readonly TreeView        _tree;
    private readonly ImageList       _entityIcons;
    private readonly Label           _statusLabel;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _createEmptyItem;
    private readonly ToolStripMenuItem _createChildItem;
    private readonly ToolStripMenuItem _duplicateItem;
    private readonly ToolStripMenuItem _renameItem;
    private readonly ToolStripMenuItem _deleteItem;
    private readonly ToolStripMenuItem _setActiveItem;
    private readonly ToolStripMenuItem _saveAsPrefabItem;
    private readonly ToolStripMenuItem _applyPrefabItem;
    private readonly ToolStripMenuItem _revertPrefabItem;

    private Action<UndoPerformedEvent>? _onUndo;
    private Action<RedoPerformedEvent>? _onRedo;

    #endregion

    #region Constructor

    /// <summary>Crea el panel con un TreeView y un menú contextual. Llama a <see cref="Initialize"/> para conectar con el contexto del editor.</summary>
    public SceneHierarchyPanel()
    {
        // ── Lista de iconos de entidad (16×16) ─────────────────────────────
        _entityIcons = new ImageList { ImageSize = new System.Drawing.Size(16, 16) };
        _entityIcons.Images.Add(MakeColorSquare(System.Drawing.Color.Gray));           // 0 Genérico
        _entityIcons.Images.Add(MakeColorSquare(System.Drawing.Color.CornflowerBlue)); // 1 Cámara
        _entityIcons.Images.Add(MakeColorSquare(System.Drawing.Color.Goldenrod));      // 2 Luz
        _entityIcons.Images.Add(MakeColorSquare(System.Drawing.Color.HotPink));        // 3 Partículas
        _entityIcons.Images.Add(MakeColorSquare(System.Drawing.Color.ForestGreen));    // 4 Tilemap

        // ── Barra de herramientas ────────────────────────────────────────
        _addBtn    = new ToolStripButton("+") { ToolTipText = "Create empty entity", DisplayStyle = ToolStripItemDisplayStyle.Text };
        _deleteBtn = new ToolStripButton("🗑") { ToolTipText = "Delete selected entity", DisplayStyle = ToolStripItemDisplayStyle.Text };
        _searchBox = new ToolStripTextBox { Width = 110 };
        ((System.Windows.Forms.TextBox)_searchBox.Control).PlaceholderText = "Search...";
        _counterLabel = new ToolStripLabel("0 entities") { Alignment = ToolStripItemAlignment.Right, ForeColor = System.Drawing.SystemColors.GrayText };

        _toolbar = new ToolStrip { Dock = DockStyle.Top, Height = 25, GripStyle = ToolStripGripStyle.Hidden };
        _toolbar.Items.Add(_addBtn);
        _toolbar.Items.Add(_deleteBtn);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(_searchBox);
        _toolbar.Items.Add(_counterLabel);

        // ── Menú contextual ───────────────────────────────────────────────
        _createEmptyItem  = new ToolStripMenuItem("Create Empty");
        _createChildItem  = new ToolStripMenuItem("Create Child");
        _duplicateItem    = new ToolStripMenuItem("Duplicate") { ShortcutKeys = Keys.Control | Keys.D, ShowShortcutKeys = true };
        _renameItem       = new ToolStripMenuItem("Rename") { ShortcutKeys = Keys.F2, ShowShortcutKeys = true };
        _deleteItem       = new ToolStripMenuItem("Delete") { ShortcutKeys = Keys.Delete, ShowShortcutKeys = true };
        _setActiveItem    = new ToolStripMenuItem("Toggle Active");
        _saveAsPrefabItem = new ToolStripMenuItem("Save as Prefab...");
        _applyPrefabItem  = new ToolStripMenuItem("Apply Prefab");
        _revertPrefabItem = new ToolStripMenuItem("Revert from Prefab");

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.AddRange(new ToolStripItem[]
        {
            _createEmptyItem,
            _createChildItem,
            new ToolStripSeparator(),
            _duplicateItem,
            new ToolStripSeparator(),
            _renameItem,
            _deleteItem,
            new ToolStripSeparator(),
            _setActiveItem,
            new ToolStripSeparator(),
            _saveAsPrefabItem,
            _applyPrefabItem,
            _revertPrefabItem,
        });

        // ── TreeView ─────────────────────────────────────────────────────
        _tree = new TreeView
        {
            Dock          = DockStyle.Fill,
            CheckBoxes    = true,
            AllowDrop     = true,
            LabelEdit     = true,
            HideSelection = false,
            FullRowSelect = true,
            BorderStyle   = BorderStyle.None,
            ImageList     = _entityIcons,
            ContextMenuStrip = _contextMenu,
        };

        // ── Etiqueta de estado ───────────────────────────────────────────
        _statusLabel = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 18,
            Font      = new System.Drawing.Font("Segoe UI", 7.5f),
            ForeColor = System.Drawing.SystemColors.GrayText,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
            Text      = "0 objects in scene",
        };

        Controls.Add(_tree);
        Controls.Add(_toolbar);
        Controls.Add(_statusLabel);

        WireTreeEvents();
        WireMenuEvents();

        _addBtn.Click    += OnCreateEmpty;
        _deleteBtn.Click += OnDelete;
        _searchBox.TextChanged += (_, _) => RefreshTreeSafe();
    }

    #endregion

    #region Initialization

    /// <summary>Conecta este panel con el contexto del editor. Debe llamarse antes de que se cargue cualquier escena.</summary>
    public void Initialize(EditorContext context, PrefabManager? prefabManager = null)
    {
        _context      = context;
        _prefabManager = prefabManager;

        _onUndo = _ => RefreshTreeSafe();
        _onRedo = _ => RefreshTreeSafe();

        _context.EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
        _context.EventBus.Subscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
        _context.EventBus.Subscribe<UndoPerformedEvent>(_onUndo);
        _context.EventBus.Subscribe<RedoPerformedEvent>(_onRedo);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _context is not null)
        {
            _context.EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            _context.EventBus.Unsubscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
            if (_onUndo is not null) _context.EventBus.Unsubscribe<UndoPerformedEvent>(_onUndo);
            if (_onRedo is not null) _context.EventBus.Unsubscribe<RedoPerformedEvent>(_onRedo);
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Event bus handlers

    private void OnSceneLoaded(SceneLoadedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnSceneLoaded(evt)); return; }
        RebuildTree(evt.Scene);

        // Selecciona automáticamente el primer objeto raíz para que el inspector se rellene de inmediato.
        if (_tree.Nodes.Count > 0 && _tree.Nodes[0].Tag is EditorGameObject first)
        {
            _suppressSelectionEvent = true;
            _tree.SelectedNode = _tree.Nodes[0];
            _suppressSelectionEvent = false;
            _multiSelected.Clear();
            _multiSelected.Add(first);
            _context?.SetSelection(first);
        }
    }

    private void OnGameObjectSelected(GameObjectSelectedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnGameObjectSelected(evt)); return; }
        _suppressSelectionEvent = true;
        try
        {
            _tree.SelectedNode = evt.GameObject is null ? null : FindNode(evt.GameObject);
        }
        finally
        {
            _suppressSelectionEvent = false;
        }
    }

    #endregion

    #region Tree building

    private void RebuildTree(EditorScene? scene)
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        _multiSelected.Clear();

        int totalCount = 0;
        if (scene is not null)
        {
            string filter = _searchBox.Text?.Trim() ?? string.Empty;
            for (int i = 0; i < scene.RootGameObjects.Count; i++)
            {
                EditorGameObject obj = scene.RootGameObjects[i];
                totalCount += CountObjects(obj);
                if (string.IsNullOrEmpty(filter) || MatchesFilter(obj, filter))
                    _tree.Nodes.Add(BuildNode(obj));
            }
            _tree.ExpandAll();
        }

        int visibleCount = CountTreeNodes(_tree.Nodes);
        _counterLabel.Text = $"{visibleCount} entities";
        _statusLabel.Text  = $"{totalCount} objects in scene";

        _tree.EndUpdate();
    }

    private static int CountObjects(EditorGameObject obj)
    {
        int count = 1;
        for (int i = 0; i < obj.Children.Count; i++)
            count += CountObjects(obj.Children[i]);
        return count;
    }

    private static int CountTreeNodes(TreeNodeCollection nodes)
    {
        int count = nodes.Count;
        for (int i = 0; i < nodes.Count; i++)
            count += CountTreeNodes(nodes[i].Nodes);
        return count;
    }

    private static bool MatchesFilter(EditorGameObject obj, string filter)
    {
        if (obj.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        for (int i = 0; i < obj.Children.Count; i++)
            if (MatchesFilter(obj.Children[i], filter)) return true;
        return false;
    }

    private void RefreshTreeSafe()
    {
        if (InvokeRequired) { BeginInvoke(RefreshTreeSafe); return; }
        RebuildTree(_context?.ActiveScene);
    }

    private static TreeNode BuildNode(EditorGameObject obj)
    {
        TreeNode node = new TreeNode(obj.Name)
        {
            Tag                = obj,
            Checked            = obj.Active,
            ImageIndex         = 0,
            SelectedImageIndex = 0,
        };
        if (obj.PrefabPath is not null)
            node.ForeColor = System.Drawing.Color.CornflowerBlue;
        for (int i = 0; i < obj.Children.Count; i++)
            node.Nodes.Add(BuildNode(obj.Children[i]));
        return node;
    }

    private TreeNode? FindNode(EditorGameObject obj) =>
        FindNodeIn(_tree.Nodes, obj);

    private static TreeNode? FindNodeIn(TreeNodeCollection nodes, EditorGameObject obj)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Tag == obj) return nodes[i];
            TreeNode? found = FindNodeIn(nodes[i].Nodes, obj);
            if (found is not null) return found;
        }
        return null;
    }

    #endregion

    #region Tree event wiring

    private void WireTreeEvents()
    {
        _tree.AfterSelect    += OnAfterSelect;
        _tree.NodeMouseClick += OnNodeMouseClick;
        _tree.AfterCheck     += OnAfterCheck;
        _tree.AfterLabelEdit += OnAfterLabelEdit;
        _tree.KeyDown        += OnTreeKeyDown;
        _tree.ItemDrag       += OnItemDrag;
        _tree.DragEnter      += OnTreeDragEnter;
        _tree.DragOver       += OnDragOver;
        _tree.DragDrop       += OnDragDrop;
    }

    private void WireMenuEvents()
    {
        _createEmptyItem.Click  += OnCreateEmpty;
        _createChildItem.Click  += OnCreateChild;
        _duplicateItem.Click    += OnDuplicate;
        _renameItem.Click       += (_, _) => _tree.SelectedNode?.BeginEdit();
        _deleteItem.Click       += OnDelete;
        _setActiveItem.Click    += OnToggleActive;
        _saveAsPrefabItem.Click += OnSaveAsPrefab;
        _applyPrefabItem.Click  += OnApplyPrefab;
        _revertPrefabItem.Click += OnRevertFromPrefab;
        _contextMenu.Opening    += OnContextMenuOpening;
    }

    #endregion

    #region Selection

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_suppressSelectionEvent) return;
        if (e.Node?.Tag is not EditorGameObject obj) return;
        if ((ModifierKeys & Keys.Control) == 0)
        {
            _multiSelected.Clear();
            _multiSelected.Add(obj);
            _context?.SetSelection(obj);
        }
    }

    private void OnNodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (_suppressSelectionEvent) return;

        if (e.Button == MouseButtons.Right)
        {
            // WinForms TreeView NO selecciona automáticamente al hacer clic derecho, por lo que lo hacemos manualmente.
            // Establecer SelectedNode dispara AfterSelect, que actualiza la selección de _context.
            _tree.SelectedNode = e.Node;
            return;
        }

        if (e.Button != MouseButtons.Left) return;
        if (e.Node?.Tag is not EditorGameObject obj) return;

        if ((ModifierKeys & Keys.Control) != 0)
        {
            if (_multiSelected.Contains(obj))
                _multiSelected.Remove(obj);
            else
                _multiSelected.Add(obj);
            _context?.SetMultiSelection(_multiSelected);
        }
    }

    #endregion

    #region Checkbox (active toggle)

    private void OnAfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_suppressCheckEvent) return;
        if (e.Node?.Tag is not EditorGameObject obj) return;
        bool newActive = e.Node.Checked;
        if (obj.Active == newActive) return;
        bool oldActive = obj.Active;
        _context!.Commands.Execute(new SetPropertyCommand<bool>(
            "Set Active", oldActive, newActive, v => obj.Active = v));
    }

    #endregion

    #region Label edit (rename)

    private void OnAfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        e.CancelEdit = true;
        if (e.Label is null || e.Node?.Tag is not EditorGameObject obj) return;
        string newName = e.Label.Trim();
        if (string.IsNullOrEmpty(newName) || newName == obj.Name) return;
        TreeNode node = e.Node;
        _context!.Commands.Execute(new RenameEntityCommand(obj, newName));
        node.Text = obj.Name;
    }

    #endregion

    #region Keyboard shortcuts

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Delete:
                OnDelete(sender, e);
                e.Handled = true;
                break;
            case Keys.F2:
                _tree.SelectedNode?.BeginEdit();
                e.Handled = true;
                break;
            case Keys.D when e.Control:
                OnDuplicate(sender, e);
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Context menu operations

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        bool hasScene    = _context?.ActiveScene is not null;
        bool hasSelected = _tree.SelectedNode?.Tag is EditorGameObject;
        bool isPrefab    = _tree.SelectedNode?.Tag is EditorGameObject { PrefabPath: not null };
        bool hasPrefabMgr = _prefabManager is not null;
        _createEmptyItem.Enabled  = hasScene;
        _createChildItem.Enabled  = hasSelected;
        _duplicateItem.Enabled    = hasSelected;
        _renameItem.Enabled       = hasSelected;
        _deleteItem.Enabled       = hasSelected;
        _setActiveItem.Enabled    = hasSelected;
        _saveAsPrefabItem.Enabled = hasSelected && hasPrefabMgr;
        _applyPrefabItem.Enabled  = isPrefab && hasPrefabMgr;
        _revertPrefabItem.Enabled = isPrefab && hasPrefabMgr;
    }

    private void OnCreateEmpty(object? sender, EventArgs e)
    {
        EditorScene? scene = _context?.ActiveScene;
        if (scene is null) return;
        EditorGameObject newObj = new() { Name = "GameObject" };
        _context!.Commands.Execute(new CreateEntityCommand(newObj, scene));
        RefreshTreeSafe();
        SelectAndEditNode(newObj);
    }

    private void OnCreateChild(object? sender, EventArgs e)
    {
        EditorScene? scene = _context?.ActiveScene;
        if (scene is null || _tree.SelectedNode?.Tag is not EditorGameObject parent) return;
        EditorGameObject newObj = new() { Name = "GameObject" };
        _context!.Commands.Execute(new CreateEntityCommand(newObj, scene, parent));
        RefreshTreeSafe();
        SelectAndEditNode(newObj);
    }

    private void OnDuplicate(object? sender, EventArgs e)
    {
        EditorScene? scene = _context?.ActiveScene;
        if (scene is null || _tree.SelectedNode?.Tag is not EditorGameObject source) return;
        EditorGameObject copy = DeepCopy(source);
        _context!.Commands.Execute(new CreateEntityCommand(copy, scene, source.Parent));
        RefreshTreeSafe();
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        EditorScene? scene = _context?.ActiveScene;
        if (scene is null || _tree.SelectedNode?.Tag is not EditorGameObject obj) return;
        _context!.Commands.Execute(new DeleteEntityCommand(obj, scene));
        _context.SetSelection(null);
        RefreshTreeSafe();
    }

    private void OnToggleActive(object? sender, EventArgs e)
    {
        if (_tree.SelectedNode?.Tag is not EditorGameObject obj) return;
        bool newActive = !obj.Active;
        _context!.Commands.Execute(new SetPropertyCommand<bool>(
            "Set Active", obj.Active, newActive, v => obj.Active = v));
        _suppressCheckEvent = true;
        try
        {
            TreeNode? node = FindNode(obj);
            if (node is not null) node.Checked = obj.Active;
        }
        finally { _suppressCheckEvent = false; }
    }

    private void OnSaveAsPrefab(object? sender, EventArgs e)
    {
        EditorScene? scene = _context?.ActiveScene;
        if (scene is null || _prefabManager is null) return;
        if (_tree.SelectedNode?.Tag is not EditorGameObject source) return;

        EditorProject? project = _context!.ActiveProject;
        string prefabsDir = project is not null
            ? project.PrefabsPath
            : Path.Combine(AppContext.BaseDirectory, "Prefabs");

        using SaveFileDialog dlg = new SaveFileDialog
        {
            Title            = "Save as Prefab",
            Filter           = "Prefab files (*.prefab.json)|*.prefab.json",
            DefaultExt       = "prefab.json",
            FileName         = source.Name,
            InitialDirectory = prefabsDir,
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _prefabManager.Save(source, dlg.FileName);
        source.PrefabPath = dlg.FileName;
        RefreshTreeSafe();
    }

    private void OnApplyPrefab(object? sender, EventArgs e)
    {
        if (_prefabManager is null) return;
        if (_tree.SelectedNode?.Tag is not EditorGameObject { PrefabPath: { } prefabPath } obj) return;
        _context!.Commands.Execute(new ApplyPrefabCommand(obj, prefabPath, _prefabManager));
    }

    private void OnRevertFromPrefab(object? sender, EventArgs e)
    {
        if (_prefabManager is null) return;
        if (_tree.SelectedNode?.Tag is not EditorGameObject { PrefabPath: { } prefabPath } obj) return;
        _context!.Commands.Execute(new RevertPrefabCommand(obj, prefabPath, _prefabManager));
        RefreshTreeSafe();
    }

    #endregion

    #region Drag and drop

    private void OnItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode node)
            DoDragDrop(node, DragDropEffects.Move);
    }

    private void OnTreeDragEnter(object? sender, DragEventArgs e)
    {
        // Acepta reasignación de padre de nodo del árbol o archivos de prefab soltados desde el navegador de assets
        if (e.Data?.GetDataPresent(typeof(TreeNode)) == true)
        {
            e.Effect = DragDropEffects.Move;
            return;
        }
        if (e.Data?.GetDataPresent(DataFormats.Text) == true)
        {
            string? path = e.Data.GetData(DataFormats.Text) as string;
            e.Effect = IsPrefabFile(path) ? DragDropEffects.Copy : DragDropEffects.None;
            return;
        }
        e.Effect = DragDropEffects.None;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        TreeNode? target = _tree.GetNodeAt(_tree.PointToClient(new System.Drawing.Point(e.X, e.Y)));
        if (target is not null) _tree.SelectedNode = target;

        if (e.Data?.GetDataPresent(typeof(TreeNode)) == true)
            e.Effect = DragDropEffects.Move;
        else if (e.Data?.GetDataPresent(DataFormats.Text) == true)
        {
            string? path = e.Data.GetData(DataFormats.Text) as string;
            e.Effect = IsPrefabFile(path) ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
            e.Effect = DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        // ── Soltar archivo de prefab ─────────────────────────────────────
        if (e.Data?.GetData(DataFormats.Text) is string filePath && IsPrefabFile(filePath))
        {
            InstantiatePrefabAtDrop(filePath);
            return;
        }

        // ── Reasignación de padre de nodo del árbol ──────────────────────
        if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode) return;
        if (draggedNode.Tag is not EditorGameObject draggedObj) return;

        EditorScene? scene = _context?.ActiveScene;
        if (scene is null) return;

        System.Drawing.Point pt = _tree.PointToClient(new System.Drawing.Point(e.X, e.Y));
        TreeNode? targetNode = _tree.GetNodeAt(pt);

        EditorGameObject? newParent = null;
        if (targetNode is not null)
        {
            if (targetNode == draggedNode) return;
            if (targetNode.Tag is not EditorGameObject targetObj) return;
            if (IsAncestorOf(draggedNode, targetNode)) return;
            newParent = targetObj;
        }

        _context!.Commands.Execute(new ReparentEntityCommand(draggedObj, scene, newParent));
        RefreshTreeSafe();
    }

    private void InstantiatePrefabAtDrop(string prefabPath)
    {
        if (_prefabManager is null || _context?.ActiveScene is null) return;
        EditorGameObject? instance = _prefabManager.Instantiate(prefabPath);
        if (instance is null) return;

        System.Drawing.Point pt = _tree.PointToClient(System.Windows.Forms.Cursor.Position);
        TreeNode? targetNode = _tree.GetNodeAt(pt);
        EditorGameObject? parent = targetNode?.Tag as EditorGameObject;

        _context.Commands.Execute(new CreateEntityCommand(instance, _context.ActiveScene, parent));
        RefreshTreeSafe();
    }

    private static bool IsPrefabFile(string? path)
        => path is not null && path.EndsWith(".prefab.json", StringComparison.OrdinalIgnoreCase);

    private static bool IsAncestorOf(TreeNode ancestor, TreeNode node)
    {
        TreeNode? current = node.Parent;
        while (current is not null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    #endregion

    #region Helpers

    private static System.Drawing.Bitmap MakeColorSquare(System.Drawing.Color color)
    {
        System.Drawing.Bitmap bmp = new(16, 16);
        using System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(color);
        return bmp;
    }

    private void SelectAndEditNode(EditorGameObject obj)
    {
        TreeNode? node = FindNode(obj);
        if (node is null) return;
        _suppressSelectionEvent = true;
        _tree.SelectedNode = node;
        _suppressSelectionEvent = false;
        _multiSelected.Clear();
        _multiSelected.Add(obj);
        _context?.SetSelection(obj);
        node.BeginEdit();
    }

    private static EditorGameObject DeepCopy(EditorGameObject source)
    {
        EditorGameObject copy = new()
        {
            Name     = source.Name + " Copy",
            Active   = source.Active,
            Position = source.Position,
            Rotation = source.Rotation,
            Scale    = source.Scale,
        };
        for (int i = 0; i < source.Behaviours.Count; i++)
        {
            EditorBehaviour b = source.Behaviours[i];
            EditorBehaviour bCopy = new() { TypeName = b.TypeName, Enabled = b.Enabled };
            foreach (System.Collections.Generic.KeyValuePair<string, JsonElement> kv in b.Properties)
                bCopy.Properties[kv.Key] = kv.Value;
            copy.Behaviours.Add(bCopy);
        }
        for (int i = 0; i < source.Children.Count; i++)
        {
            EditorGameObject childCopy = DeepCopy(source.Children[i]);
            childCopy.Parent = copy;
            copy.Children.Add(childCopy);
        }
        return copy;
    }

    #endregion
}
