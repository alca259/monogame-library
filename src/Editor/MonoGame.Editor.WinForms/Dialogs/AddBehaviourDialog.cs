namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>
/// Diálogo modal que permite al usuario seleccionar una subclase de <c>GameBehaviour</c> del
/// <see cref="GameObjectRegistry"/> para adjuntarla a un objeto del juego.
/// Los tipos se agrupan por espacio de nombres en un <see cref="TreeView"/>. Un botón "Create New..."
/// abre <see cref="NewBehaviourDialog"/> para crear un nuevo behaviour.
/// </summary>
public sealed class AddBehaviourDialog : Form
{
    private readonly TextBox          _searchBox;
    private readonly TreeView         _tree;
    private readonly Button           _okButton;
    private readonly Button           _cancelButton;
    private readonly Button           _createNewButton;
    private readonly Button           _rescanButton;
    private readonly EditorProject?   _project;
    private readonly GameObjectRegistry _registry;

    // Lista plana de (espacio de nombres, nombre corto, nombre completo) para reconstrucción
    private readonly List<(string Ns, string Short, string Full)> _allTypes = [];

    /// <summary>Nombre completo del tipo seleccionado por el usuario, o <c>null</c> si se canceló.</summary>
    public string? SelectedTypeName { get; private set; }

    /// <summary>Crea un nuevo diálogo poblado desde <paramref name="registry"/>.</summary>
    public AddBehaviourDialog(GameObjectRegistry registry, EditorProject? project = null)
    {
        _registry = registry;
        _project  = project;

        Text            = "Add Behaviour";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new System.Drawing.Size(400, 400);
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        Label searchLabel = new()
        {
            Text     = "Search:",
            Location = new System.Drawing.Point(8, 10),
            AutoSize = true,
        };

        _searchBox = new TextBox
        {
            Location = new System.Drawing.Point(8, 28),
            Width    = 384,
        };

        _tree = new TreeView
        {
            Location      = new System.Drawing.Point(8, 56),
            Size          = new System.Drawing.Size(384, 264),
            HideSelection = false,
            ShowLines     = true,
            ShowPlusMinus = true,
            Sorted        = true,
        };

        _createNewButton = new Button
        {
            Text     = "Create New...",
            Location = new System.Drawing.Point(8, 328),
            Width    = 104,
        };
        _createNewButton.Click += OnCreateNewClick;

        _rescanButton = new Button
        {
            Text     = "Rescan",
            Location = new System.Drawing.Point(120, 328),
            Width    = 72,
        };
        _rescanButton.Click += OnRescanClick;

        _okButton = new Button
        {
            Text         = "OK",
            DialogResult = DialogResult.OK,
            Location     = new System.Drawing.Point(208, 328),
            Width        = 76,
        };
        _okButton.Click += OnOkClick;

        _cancelButton = new Button
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new System.Drawing.Point(316, 328),
            Width        = 76,
        };

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.Add(searchLabel);
        Controls.Add(_searchBox);
        Controls.Add(_tree);
        Controls.Add(_createNewButton);
        Controls.Add(_rescanButton);
        Controls.Add(_okButton);
        Controls.Add(_cancelButton);

        // Poblar datos
        foreach (KeyValuePair<string, Type> kv in registry.RegisteredTypes)
        {
            string full  = kv.Key;
            string short_ = kv.Value.Name;
            string ns    = kv.Value.Namespace ?? string.Empty;
            _allTypes.Add((ns, short_, full));
        }
        _allTypes.Sort((a, b) =>
        {
            int nsComp = string.Compare(a.Ns, b.Ns, StringComparison.OrdinalIgnoreCase);
            return nsComp != 0 ? nsComp : string.Compare(a.Short, b.Short, StringComparison.OrdinalIgnoreCase);
        });

        RebuildTree(string.Empty);

        _searchBox.TextChanged += (_, _) => RebuildTree(_searchBox.Text);
        _tree.DoubleClick      += (_, _) => { if (GetSelectedTypeName() is not null) _okButton.PerformClick(); };
        _tree.AfterSelect      += (_, _) => _okButton.Enabled = GetSelectedTypeName() is not null;
        _okButton.Enabled = false;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        SelectedTypeName = GetSelectedTypeName();
        if (SelectedTypeName is not null)
            DialogResult = DialogResult.OK;
    }

    private async void OnCreateNewClick(object? sender, EventArgs e)
    {
        string gameSourcePath  = _project?.GameSourcePath ?? string.Empty;
        string projectRootPath = _project?.RootPath       ?? string.Empty;
        string defaultNs       = string.IsNullOrEmpty(_project?.GameCsprojPath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(_project.GameCsprojPath);

        using NewBehaviourDialog dlg = new(gameSourcePath, projectRootPath, defaultNs);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // Reescanear todo el código fuente bajo la raíz para que el nuevo archivo .cs sea detectado como tipo pendiente.
        if (!string.IsNullOrEmpty(projectRootPath))
            await _registry.ScanSourceAsync(projectRootPath).ConfigureAwait(true);

        // Reconstruir la lista de tipos con los tipos compilados y pendientes.
        _allTypes.Clear();
        foreach (KeyValuePair<string, Type> kv in _registry.RegisteredTypes)
        {
            string full   = kv.Key;
            string short_ = kv.Value.Name;
            string ns     = kv.Value.Namespace ?? string.Empty;
            _allTypes.Add((ns, short_, full));
        }
        foreach (string pendingFull in _registry.PendingTypeNames)
        {
            int dotIdx = pendingFull.LastIndexOf('.');
            string short_ = dotIdx >= 0 ? pendingFull[(dotIdx + 1)..] : pendingFull;
            string ns     = dotIdx >= 0 ? pendingFull[..dotIdx] : string.Empty;
            _allTypes.Add((ns, short_, pendingFull));
        }
        _allTypes.Sort((a, b) =>
        {
            int nsComp = string.Compare(a.Ns, b.Ns, StringComparison.OrdinalIgnoreCase);
            return nsComp != 0 ? nsComp : string.Compare(a.Short, b.Short, StringComparison.OrdinalIgnoreCase);
        });
        RebuildTree(_searchBox.Text);
    }

    private async void OnRescanClick(object? sender, EventArgs e)
    {
        _rescanButton.Enabled = false;
        try
        {
            _registry.Scan();
            string rootPath = _project?.RootPath ?? string.Empty;

            // Escanear todo el código fuente bajo la raíz — cubre proyectos de juego, librerías, GameScripts, etc.
            if (!string.IsNullOrEmpty(rootPath))
                await _registry.ScanSourceAsync(rootPath).ConfigureAwait(true);

            _allTypes.Clear();
            foreach (KeyValuePair<string, Type> kv in _registry.RegisteredTypes)
            {
                string full   = kv.Key;
                string short_ = kv.Value.Name;
                string ns     = kv.Value.Namespace ?? string.Empty;
                _allTypes.Add((ns, short_, full));
            }
            foreach (string pendingFull in _registry.PendingTypeNames)
            {
                int dotIdx = pendingFull.LastIndexOf('.');
                string short_ = dotIdx >= 0 ? pendingFull[(dotIdx + 1)..] : pendingFull;
                string ns     = dotIdx >= 0 ? pendingFull[..dotIdx] : string.Empty;
                _allTypes.Add((ns, short_, pendingFull));
            }
            _allTypes.Sort((a, b) =>
            {
                int nsComp = string.Compare(a.Ns, b.Ns, StringComparison.OrdinalIgnoreCase);
                return nsComp != 0 ? nsComp : string.Compare(a.Short, b.Short, StringComparison.OrdinalIgnoreCase);
            });
            RebuildTree(_searchBox.Text);
        }
        finally
        {
            _rescanButton.Enabled = true;
        }
    }

    private string? GetSelectedTypeName()
    {
        TreeNode? node = _tree.SelectedNode;
        if (node is null || node.Level != 1) return null;
        return node.Tag as string;
    }

    private void RebuildTree(string filter)
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        for (int i = 0; i < _allTypes.Count; i++)
        {
            (string ns, string shortName, string fullName) = _allTypes[i];
            if (!string.IsNullOrEmpty(filter) &&
                !shortName.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !fullName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            string nsKey = string.IsNullOrEmpty(ns) ? "(global)" : ns;
            TreeNode? nsNode = _tree.Nodes[nsKey];
            if (nsNode is null)
            {
                nsNode     = new TreeNode(nsKey) { Name = nsKey };
                _tree.Nodes.Add(nsNode);
            }

            TreeNode typeNode = new(shortName)
            {
                Tag         = fullName,
                ToolTipText = fullName,
            };
            nsNode.Nodes.Add(typeNode);
        }

        _tree.ExpandAll();
        _tree.EndUpdate();
        _okButton.Enabled = false;
    }
}
