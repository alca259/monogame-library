using Alca.MonoGame.Kernel.Input;
using XnaInput = Microsoft.Xna.Framework.Input;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Panel para crear y editar mapas de acciones de entrada (<c>*.input.json</c>). El lado izquierdo muestra las acciones en un
/// <see cref="TreeView"/>; el lado derecho muestra los enlaces de la acción seleccionada en un <see cref="DataGridView"/>.
/// </summary>
public sealed class InputMapEditorPanel : UserControl
{
    private EditorContext? _context;
    private InputEditorModel? _activeModel;
    private InputActionEntry? _selectedAction;

    // ── Contenedor dividido ─────────────────────────────────────────────
    private readonly SplitContainer _split;

    // ── Panel izquierdo ───────────────────────────────────────────────────
    private readonly ToolStrip _leftToolStrip;
    private readonly ToolStripButton _loadFileButton;
    private readonly ToolStripButton _saveFileButton;
    private readonly ToolStripSeparator _leftSep;
    private readonly ToolStripButton _addActionButton;
    private readonly ToolStripButton _removeActionButton;
    private readonly ComboBox _mapFileSelector;
    private readonly TreeView _actionTree;
    private readonly Label _leftStatusLabel;

    // ── Panel derecho ──────────────────────────────────────────────────────
    private readonly Label _actionNameLabel;
    private readonly ToolStrip _rightToolStrip;
    private readonly ToolStripButton _addBindingButton;
    private readonly ToolStripButton _removeBindingButton;
    private readonly DataGridView _bindingsGrid;
    private readonly DataGridViewComboBoxColumn _deviceColumn;
    private readonly DataGridViewComboBoxColumn _keyButtonColumn;

    private bool _suppressGridEvents;

    private static readonly string[] DeviceItems = ["Keyboard", "Gamepad", "Mouse"];

    /// <summary>Inicializa el diseño del panel.</summary>
    public InputMapEditorPanel()
    {
        _split            = new SplitContainer();
        _leftToolStrip    = new ToolStrip();
        _loadFileButton   = new ToolStripButton();
        _saveFileButton   = new ToolStripButton();
        _leftSep          = new ToolStripSeparator();
        _addActionButton  = new ToolStripButton();
        _removeActionButton = new ToolStripButton();
        _mapFileSelector  = new ComboBox();
        _actionTree       = new TreeView();
        _leftStatusLabel  = new Label();

        _actionNameLabel  = new Label();
        _rightToolStrip   = new ToolStrip();
        _addBindingButton = new ToolStripButton();
        _removeBindingButton = new ToolStripButton();
        _bindingsGrid     = new DataGridView();
        _deviceColumn     = new DataGridViewComboBoxColumn();
        _keyButtonColumn  = new DataGridViewComboBoxColumn();

        ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
        _split.Panel1.SuspendLayout();
        _split.Panel2.SuspendLayout();
        _split.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_bindingsGrid).BeginInit();
        SuspendLayout();

        // _loadFileButton
        _loadFileButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _loadFileButton.Text         = "Load";
        _loadFileButton.ToolTipText  = "Load .input.json file";
        _loadFileButton.Click       += OnLoadFileClick;

        // _saveFileButton
        _saveFileButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _saveFileButton.Text         = "Save";
        _saveFileButton.ToolTipText  = "Save current map to file";
        _saveFileButton.Enabled      = false;
        _saveFileButton.Click       += OnSaveFileClick;

        // _addActionButton
        _addActionButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _addActionButton.Text         = "+ Action";
        _addActionButton.ToolTipText  = "Add a new action";
        _addActionButton.Enabled      = false;
        _addActionButton.Click       += OnAddActionClick;

        // _removeActionButton
        _removeActionButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _removeActionButton.Text         = "- Action";
        _removeActionButton.ToolTipText  = "Remove selected action";
        _removeActionButton.Enabled      = false;
        _removeActionButton.Click       += OnRemoveActionClick;

        // _leftToolStrip
        _leftToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _leftToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            { _loadFileButton, _saveFileButton, _leftSep, _addActionButton, _removeActionButton });
        _leftToolStrip.Dock = DockStyle.Top;

        // _mapFileSelector
        _mapFileSelector.Dock           = DockStyle.Top;
        _mapFileSelector.DropDownStyle  = ComboBoxStyle.DropDownList;
        _mapFileSelector.SelectedIndexChanged += OnMapFileSelected;

        // _actionTree
        _actionTree.Dock          = DockStyle.Fill;
        _actionTree.FullRowSelect = true;
        _actionTree.HideSelection = false;
        _actionTree.AfterSelect  += OnActionTreeAfterSelect;

        // _leftStatusLabel
        _leftStatusLabel.Dock      = DockStyle.Bottom;
        _leftStatusLabel.Height    = 18;
        _leftStatusLabel.Font      = new System.Drawing.Font("Segoe UI", 7.5f);
        _leftStatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _leftStatusLabel.Text      = "No map loaded";

        // Ensamblaje del panel izquierdo
        _split.Panel1.Controls.Add(_actionTree);
        _split.Panel1.Controls.Add(_mapFileSelector);
        _split.Panel1.Controls.Add(_leftToolStrip);
        _split.Panel1.Controls.Add(_leftStatusLabel);

        // _actionNameLabel
        _actionNameLabel.Dock      = DockStyle.Top;
        _actionNameLabel.Height    = 24;
        _actionNameLabel.Font      = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
        _actionNameLabel.Text      = "Select an action";
        _actionNameLabel.Padding   = new Padding(4, 4, 0, 0);

        // _addBindingButton
        _addBindingButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _addBindingButton.Text         = "+ Binding";
        _addBindingButton.ToolTipText  = "Add a new binding to the selected action";
        _addBindingButton.Enabled      = false;
        _addBindingButton.Click       += OnAddBindingClick;

        // _removeBindingButton
        _removeBindingButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _removeBindingButton.Text         = "- Binding";
        _removeBindingButton.ToolTipText  = "Remove selected binding";
        _removeBindingButton.Enabled      = false;
        _removeBindingButton.Click       += OnRemoveBindingClick;

        // _rightToolStrip
        _rightToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _rightToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            { _addBindingButton, _removeBindingButton });
        _rightToolStrip.Dock = DockStyle.Top;

        // _deviceColumn
        _deviceColumn.HeaderText      = "Device";
        _deviceColumn.Name            = "_deviceColumn";
        _deviceColumn.Width           = 100;
        _deviceColumn.FlatStyle       = FlatStyle.Flat;
        foreach (string item in DeviceItems) _deviceColumn.Items.Add(item);

        // _keyButtonColumn
        _keyButtonColumn.HeaderText = "Key / Button";
        _keyButtonColumn.Name       = "_keyButtonColumn";
        _keyButtonColumn.FlatStyle  = FlatStyle.Flat;

        // _bindingsGrid
        _bindingsGrid.Dock                  = DockStyle.Fill;
        _bindingsGrid.AllowUserToAddRows    = false;
        _bindingsGrid.AllowUserToDeleteRows = false;
        _bindingsGrid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _bindingsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _bindingsGrid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _bindingsGrid.MultiSelect           = false;
        _bindingsGrid.RowHeadersVisible     = false;
        _bindingsGrid.Columns.AddRange(new DataGridViewColumn[] { _deviceColumn, _keyButtonColumn });
        _bindingsGrid.SelectionChanged     += OnBindingsSelectionChanged;
        _bindingsGrid.CellEndEdit          += OnBindingsCellEndEdit;
        _bindingsGrid.EditingControlShowing += OnBindingsEditingControlShowing;
        _bindingsGrid.DataError            += (_, e) => e.ThrowException = false;

        // Ensamblaje del panel derecho
        _split.Panel2.Controls.Add(_bindingsGrid);
        _split.Panel2.Controls.Add(_rightToolStrip);
        _split.Panel2.Controls.Add(_actionNameLabel);

        // _split
        _split.Dock             = DockStyle.Fill;
        _split.Orientation      = Orientation.Vertical;
        _split.SplitterDistance = 200;

        // UserControl
        Controls.Add(_split);
        Dock = DockStyle.Fill;

        ((System.ComponentModel.ISupportInitialize)_bindingsGrid).EndInit();
        _split.Panel1.ResumeLayout(false);
        _split.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_split).EndInit();
        _split.ResumeLayout(false);
        ResumeLayout(false);
    }

    /// <summary>Conecta el panel con el contexto del editor y se suscribe a eventos.</summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        _context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _context?.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
        base.Dispose(disposing);
    }

    // ── Manejadores de eventos ────────────────────────────────────────────

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }

        PopulateMapSelector(evt.Project?.GameSourcePath ?? string.Empty);
    }

    private void PopulateMapSelector(string gameSourcePath)
    {
        _mapFileSelector.SelectedIndexChanged -= OnMapFileSelected;
        _mapFileSelector.Items.Clear();

        if (!string.IsNullOrEmpty(gameSourcePath) && Directory.Exists(gameSourcePath))
        {
            foreach (string file in Directory.GetFiles(gameSourcePath, "*.input.json", SearchOption.AllDirectories))
                _mapFileSelector.Items.Add(file);
        }

        _mapFileSelector.SelectedIndexChanged += OnMapFileSelected;
        _leftStatusLabel.Text = $"{_mapFileSelector.Items.Count} map(s) found";
        _activeModel = null;
        RebuildActionTree();
    }

    private async void OnMapFileSelected(object? sender, EventArgs e)
    {
        if (_mapFileSelector.SelectedItem is not string filePath) return;

        try
        {
            _activeModel = await InputEditorModel.LoadAsync(filePath).ConfigureAwait(true);
            _context?.EventBus.Publish(new InputMapLoadedEvent(_activeModel));
            _saveFileButton.Enabled   = true;
            _addActionButton.Enabled  = true;
            _leftStatusLabel.Text     = $"{_activeModel.Actions.Count} action(s)";
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(this, ex.Message, "Error loading map", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        RebuildActionTree();
    }

    private async void OnLoadFileClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dlg = new()
        {
            Title  = "Select input map file",
            Filter = "Input Map (*.input.json)|*.input.json|JSON files (*.json)|*.json"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string file = dlg.FileName;
        if (!_mapFileSelector.Items.Contains(file))
            _mapFileSelector.Items.Add(file);

        _mapFileSelector.SelectedItem = file;

        // Si el archivo aún no está en el selector, cargarlo manualmente
        if (_mapFileSelector.SelectedItem is null)
        {
            try
            {
                _activeModel = await InputEditorModel.LoadAsync(file).ConfigureAwait(true);
                _context?.EventBus.Publish(new InputMapLoadedEvent(_activeModel));
                _saveFileButton.Enabled  = true;
                _addActionButton.Enabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(this, ex.Message, "Error loading map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RebuildActionTree();
        }
    }

    private async void OnSaveFileClick(object? sender, EventArgs e)
    {
        if (_activeModel is null) return;

        try
        {
            await _activeModel.SaveAsync().ConfigureAwait(true);
            _leftStatusLabel.Text = "Saved.";
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(this, ex.Message, "Error saving map", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAddActionClick(object? sender, EventArgs e)
    {
        if (_activeModel is null || _context is null) return;

        string? name = PromptForName("New Action", "Action name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        _context.Commands.Execute(new AddInputActionCommand(_activeModel, name));
        RebuildActionTree();
        SelectActionByName(name);
        _leftStatusLabel.Text = $"{_activeModel.Actions.Count} action(s)";
    }

    private void OnRemoveActionClick(object? sender, EventArgs e)
    {
        if (_activeModel is null || _context is null || _selectedAction is null) return;

        string name = _selectedAction.Name;
        _context.Commands.Execute(new RemoveInputActionCommand(_activeModel, name));
        _selectedAction = null;
        RebuildActionTree();
        ClearBindings();
        _leftStatusLabel.Text = $"{_activeModel.Actions.Count} action(s)";
    }

    private void OnActionTreeAfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_activeModel is null || e.Node is null) return;

        string actionName = e.Node.Text;
        _selectedAction = _activeModel.GetAction(actionName);
        _removeActionButton.Enabled = _selectedAction is not null;
        _addBindingButton.Enabled   = _selectedAction is not null;
        _actionNameLabel.Text       = _selectedAction?.Name ?? "Select an action";
        RefreshBindingsGrid();
    }

    private void OnAddBindingClick(object? sender, EventArgs e)
    {
        if (_activeModel is null || _context is null || _selectedAction is null) return;

        InputBindingEntry binding = new(DeviceType.Keyboard, (int)XnaInput.Keys.Space);
        _context.Commands.Execute(new AddInputBindingCommand(_activeModel, _selectedAction.Name, binding));
        RefreshBindingsGrid();
    }

    private void OnRemoveBindingClick(object? sender, EventArgs e)
    {
        if (_activeModel is null || _context is null || _selectedAction is null) return;
        if (_bindingsGrid.SelectedRows.Count == 0) return;

        int row = _bindingsGrid.SelectedRows[0].Index;
        if (row < 0 || row >= _selectedAction.Bindings.Count) return;

        InputBindingEntry binding = _selectedAction.Bindings[row];
        _context.Commands.Execute(new RemoveInputBindingCommand(_activeModel, _selectedAction.Name, binding));
        RefreshBindingsGrid();
    }

    private void OnBindingsSelectionChanged(object? sender, EventArgs e)
    {
        _removeBindingButton.Enabled = _bindingsGrid.SelectedRows.Count > 0 && _selectedAction is not null;
    }

    private void OnBindingsCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressGridEvents || _selectedAction is null || _activeModel is null) return;
        if (e.RowIndex < 0 || e.RowIndex >= _selectedAction.Bindings.Count) return;

        InputBindingEntry oldBinding = _selectedAction.Bindings[e.RowIndex];
        InputBindingEntry newBinding = BuildBindingFromRow(e.RowIndex);

        if (newBinding == oldBinding) return;

        // Mutación directa del modelo para ediciones a nivel de celda (sin entrada de deshacer)
        _selectedAction.Bindings[e.RowIndex] = newBinding;
    }

    private void OnBindingsEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_bindingsGrid.CurrentCell?.ColumnIndex != _keyButtonColumn.Index) return;
        if (e.Control is not ComboBox cb) return;

        int row = _bindingsGrid.CurrentCell.RowIndex;
        object? deviceVal = _bindingsGrid.Rows[row].Cells[_deviceColumn.Index].Value;
        DeviceType device = deviceVal is string s && Enum.TryParse(s, out DeviceType dt) ? dt : DeviceType.Keyboard;

        cb.Items.Clear();
        PopulateKeyButtonItems(cb, device);
    }

        // ── Auxiliares de actualización ───────────────────────────────────

    private void RebuildActionTree()
    {
        _actionTree.BeginUpdate();
        _actionTree.Nodes.Clear();

        if (_activeModel is not null)
        {
            foreach (InputActionEntry action in _activeModel.Actions)
                _actionTree.Nodes.Add(new TreeNode(action.Name));
        }

        _actionTree.EndUpdate();
        ClearBindings();
        _selectedAction = null;
        _removeActionButton.Enabled = false;
    }

    private void RefreshBindingsGrid()
    {
        _suppressGridEvents = true;
        _bindingsGrid.Rows.Clear();

        if (_selectedAction is null) { _suppressGridEvents = false; return; }

        foreach (InputBindingEntry b in _selectedAction.Bindings)
        {
            string deviceStr = b.DeviceType.ToString();
            string keyStr    = b.ToDisplayString();
            _bindingsGrid.Rows.Add(deviceStr, keyStr);
        }

        _suppressGridEvents = false;
    }

    private void ClearBindings()
    {
        _bindingsGrid.Rows.Clear();
        _actionNameLabel.Text       = "Select an action";
        _addBindingButton.Enabled   = false;
        _removeBindingButton.Enabled = false;
    }

    private void SelectActionByName(string name)
    {
        foreach (TreeNode node in _actionTree.Nodes)
        {
            if (node.Text == name)
            {
                _actionTree.SelectedNode = node;
                return;
            }
        }
    }

    private InputBindingEntry BuildBindingFromRow(int rowIndex)
    {
        object? deviceVal = _bindingsGrid.Rows[rowIndex].Cells[_deviceColumn.Index].Value;
        object? keyVal    = _bindingsGrid.Rows[rowIndex].Cells[_keyButtonColumn.Index].Value;

        DeviceType device = deviceVal is string ds && Enum.TryParse(ds, out DeviceType dt) ? dt : DeviceType.Keyboard;
        int code = ParseCode(device, keyVal?.ToString() ?? string.Empty);
        return new InputBindingEntry(device, code);
    }

        // ── Utilidades ────────────────────────────────────────────────────

    private static void PopulateKeyButtonItems(ComboBox cb, DeviceType device)
    {
        switch (device)
        {
            case DeviceType.Keyboard:
                foreach (XnaInput.Keys k in Enum.GetValues<XnaInput.Keys>()) cb.Items.Add(k.ToString());
                break;
            case DeviceType.Gamepad:
                foreach (XnaInput.Buttons b in Enum.GetValues<XnaInput.Buttons>()) cb.Items.Add($"{b} (Gamepad)");
                break;
            case DeviceType.Mouse:
                foreach (Alca.MonoGame.Kernel.Input.MouseButton mb in Enum.GetValues<Alca.MonoGame.Kernel.Input.MouseButton>())
                    cb.Items.Add(mb.ToString());
                break;
        }
    }

    private static int ParseCode(DeviceType device, string displayValue)
    {
        return device switch
        {
            DeviceType.Keyboard => Enum.TryParse(displayValue, out XnaInput.Keys k)   ? (int)k  : 0,
            DeviceType.Mouse    => Enum.TryParse(displayValue, out Alca.MonoGame.Kernel.Input.MouseButton m) ? (int)m : 0,
            DeviceType.Gamepad  => Enum.TryParse(displayValue.Replace(" (Gamepad)", ""), out XnaInput.Buttons b) ? (int)b : 0,
            _                   => 0
        };
    }

    private static string? PromptForName(string title, string prompt)
    {
        using Form dlg = new()
        {
            Text              = title,
            ClientSize        = new System.Drawing.Size(300, 90),
            FormBorderStyle   = FormBorderStyle.FixedDialog,
            StartPosition     = FormStartPosition.CenterParent,
            MinimizeBox       = false,
            MaximizeBox       = false
        };

        Label lbl = new() { Text = prompt, Dock = DockStyle.Top, Height = 20, Padding = new Padding(4, 4, 0, 0) };
        TextBox tb = new() { Dock = DockStyle.Top };
        Button ok = new() { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };

        dlg.Controls.Add(ok);
        dlg.Controls.Add(tb);
        dlg.Controls.Add(lbl);
        dlg.AcceptButton = ok;
        dlg.Shown += (_, _) => tb.Focus();

        return dlg.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
    }
}
