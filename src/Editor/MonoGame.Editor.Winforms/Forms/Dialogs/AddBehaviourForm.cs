using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Forms.Dialogs;

/// <summary>
/// Diálogo para seleccionar un tipo de Behaviour del registro del proyecto y añadirlo al objeto activo.
/// Devuelve el nombre de tipo completo seleccionado, o <c>null</c> si el usuario cancela.
/// </summary>
internal sealed class AddBehaviourForm : Form
{
    private readonly GameObjectRegistry _registry;

    private readonly TextBox  _searchBox;
    private readonly TreeView _tree;
    private readonly Button   _btnOk;
    private readonly Button   _btnRescan;

    private string? _selectedTypeName;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Abre el diálogo de forma modal y devuelve el nombre de tipo elegido, o <c>null</c>.
    /// </summary>
    public static string? Show(IWin32Window? owner, GameObjectRegistry registry)
    {
        using AddBehaviourForm dlg = new(registry);
        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg._selectedTypeName : null;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    private AddBehaviourForm(GameObjectRegistry registry)
    {
        _registry = registry;

        Text            = "Add Behaviour";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(460, 520);
        MinimumSize     = new Size(340, 380);
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;
        Font            = EditorFonts.Primary;
        ShowIcon        = false;
        ShowInTaskbar   = false;

        // ── Barra de búsqueda ─────────────────────────────────────────────────
        Panel searchPanel = new()
        {
            Dock      = DockStyle.Top,
            Height    = 34,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(6, 5, 6, 5),
        };

        _searchBox = new TextBox
        {
            Dock        = DockStyle.Fill,
            PlaceholderText = "Search behaviour…",
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = EditorFonts.Primary,
        };
        searchPanel.Controls.Add(_searchBox);

        // ── TreeView ──────────────────────────────────────────────────────────
        _tree = new TreeView
        {
            Dock            = DockStyle.Fill,
            BackColor       = EditorColors.PanelBackground,
            ForeColor       = EditorColors.TextPrimary,
            Font            = EditorFonts.Primary,
            BorderStyle     = BorderStyle.None,
            HideSelection   = false,
            ShowLines       = true,
            ShowRootLines   = true,
            FullRowSelect   = false,
        };

        // ── Botonera inferior ─────────────────────────────────────────────────
        Panel btnPanel = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 42,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(8, 7, 8, 7),
        };

        _btnRescan = new Button
        {
            Text      = "Rescan",
            Dock      = DockStyle.Left,
            Width     = 72,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Primary,
        };
        _btnRescan.FlatAppearance.BorderColor = EditorColors.Border;

        Button btnCancel = new()
        {
            Text         = "Cancel",
            Dock         = DockStyle.Right,
            Width        = 72,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.PanelBackground,
            ForeColor    = EditorColors.TextSecondary,
            Font         = EditorFonts.Primary,
            DialogResult = DialogResult.Cancel,
        };
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;

        _btnOk = new Button
        {
            Text         = "Add",
            Dock         = DockStyle.Right,
            Width        = 72,
            FlatStyle    = FlatStyle.Flat,
            BackColor    = EditorColors.AccentBlue,
            ForeColor    = EditorColors.TextPrimary,
            Font         = EditorFonts.PrimaryBold,
            Enabled      = false,
            DialogResult = DialogResult.OK,
        };
        _btnOk.FlatAppearance.BorderSize = 0;

        btnPanel.Controls.Add(_btnRescan);
        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(_btnOk);

        AcceptButton = _btnOk;
        CancelButton = btnCancel;

        Controls.Add(_tree);
        Controls.Add(searchPanel);
        Controls.Add(btnPanel);

        // ── Eventos ───────────────────────────────────────────────────────────
        _searchBox.TextChanged    += (_, _) => RebuildTree(_searchBox.Text);
        _tree.AfterSelect         += OnAfterSelect;
        _tree.NodeMouseDoubleClick += OnNodeDoubleClick;
        _btnRescan.Click          += (_, _) => Rescan();

        RebuildTree(string.Empty);
    }

    // ── Construcción del árbol ────────────────────────────────────────────────

    private void RebuildTree(string filter)
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        bool filtering = !string.IsNullOrWhiteSpace(filter);

        // Tipos compilados: organizar por namespace
        Dictionary<string, TreeNode> nsNodes = [];

        foreach (KeyValuePair<string, Type> kv in _registry.RegisteredTypes)
        {
            string fullName  = kv.Key;
            string shortName = GetShortName(fullName);
            string ns        = GetNamespace(fullName);

            if (filtering && !fullName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                          && !shortName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!nsNodes.TryGetValue(ns, out TreeNode? nsNode))
            {
                nsNode = new TreeNode(ns)
                {
                    ForeColor = EditorColors.TextSecondary,
                    NodeFont  = EditorFonts.Small,
                    Tag       = null,
                };
                nsNodes[ns] = nsNode;
                _tree.Nodes.Add(nsNode);
            }

            TreeNode leaf = new(shortName)
            {
                ForeColor = EditorColors.TextPrimary,
                Tag       = fullName,
                ToolTipText = fullName,
            };
            nsNode.Nodes.Add(leaf);
        }

        // Tipos pendientes (sin compilar)
        if (_registry.PendingTypeNames.Count > 0)
        {
            TreeNode pendingNs = new("(pending — not compiled)")
            {
                ForeColor = EditorColors.TextMuted,
                NodeFont  = EditorFonts.Small,
                Tag       = null,
            };

            foreach (string pendingName in _registry.PendingTypeNames)
            {
                if (filtering && !pendingName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                TreeNode leaf = new(pendingName)
                {
                    ForeColor   = EditorColors.TextMuted,
                    NodeFont    = new Font(EditorFonts.Primary, FontStyle.Italic),
                    Tag         = null,
                    ToolTipText = "Not yet compiled — build the project first.",
                };
                pendingNs.Nodes.Add(leaf);
            }

            if (pendingNs.Nodes.Count > 0)
                _tree.Nodes.Add(pendingNs);
        }

        // Expandir todo si hay filtro o pocas raíces
        if (filtering || _tree.Nodes.Count <= 4)
        {
            foreach (TreeNode n in _tree.Nodes)
                n.Expand();
        }

        _tree.EndUpdate();
        UpdateOkState();
    }

    // ── Selección ─────────────────────────────────────────────────────────────

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        _selectedTypeName = e.Node?.Tag as string;
        UpdateOkState();
    }

    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is string typeName)
        {
            _selectedTypeName = typeName;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void UpdateOkState() =>
        _btnOk.Enabled = _selectedTypeName is not null;

    // ── Rescan ────────────────────────────────────────────────────────────────

    private void Rescan()
    {
        _btnRescan.Enabled = false;
        _registry.Scan();
        RebuildTree(_searchBox.Text);
        _btnRescan.Enabled = true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetShortName(string fullName)
    {
        int dot = fullName.LastIndexOf('.');
        return dot >= 0 ? fullName[(dot + 1)..] : fullName;
    }

    private static string GetNamespace(string fullName)
    {
        int dot = fullName.LastIndexOf('.');
        return dot >= 0 ? fullName[..dot] : "(global)";
    }
}
