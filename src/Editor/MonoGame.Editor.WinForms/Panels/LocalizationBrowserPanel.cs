namespace MonoGame.Editor.WinForms.Panels;

/// <summary>Panel para editar archivos de localización (<c>*.json</c>) bajo la ruta LocalizationPath del proyecto.</summary>
public sealed class LocalizationBrowserPanel : UserControl
{
    private EditorContext? _context;
    private LocalizationEditorModel? _model;
    private string _localizationRoot        = string.Empty;
    private string _currentLocalizationPath = string.Empty;

    // ── Árbol de carpetas ────────────────────────────────────────────────
    private readonly SplitContainer _mainSplit;
    private readonly TreeView       _folderTree;

    // ── Barra de herramientas superior ───────────────────────────────────
    private readonly ToolStrip _toolStrip;
    private readonly ToolStripButton _addKeyButton;
    private readonly ToolStripButton _removeKeyButton;
    private readonly ToolStripSeparator _toolSep1;
    private readonly ToolStripButton _addLocaleButton;
    private readonly ToolStripSeparator _toolSep2;
    private readonly ToolStripButton _importButton;
    private readonly ToolStripButton _exportCsvButton;
    private readonly ToolStripSeparator _toolSep3;
    private readonly ToolStripButton _saveButton;

    // ── Filtro ───────────────────────────────────────────────────────────
    private readonly TextBox _filterBox;

    // ── Cuadrícula ───────────────────────────────────────────────────────
    private readonly DataGridView _grid;

    // ── Estado ───────────────────────────────────────────────────────────
    private readonly Label _statusLabel;

    private string _activeFilter = string.Empty;

    /// <summary>Inicializa el diseño del panel.</summary>
    public LocalizationBrowserPanel()
    {
        _mainSplit        = new SplitContainer();
        _folderTree       = new TreeView();
        _toolStrip        = new ToolStrip();
        _addKeyButton     = new ToolStripButton();
        _removeKeyButton  = new ToolStripButton();
        _toolSep1         = new ToolStripSeparator();
        _addLocaleButton  = new ToolStripButton();
        _toolSep2         = new ToolStripSeparator();
        _importButton     = new ToolStripButton();
        _exportCsvButton  = new ToolStripButton();
        _toolSep3         = new ToolStripSeparator();
        _saveButton       = new ToolStripButton();
        _filterBox        = new TextBox();
        _grid             = new DataGridView();
        _statusLabel      = new Label();

        _toolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_mainSplit).BeginInit();
        _mainSplit.Panel1.SuspendLayout();
        _mainSplit.Panel2.SuspendLayout();
        _mainSplit.SuspendLayout();
        SuspendLayout();

        // _addKeyButton
        _addKeyButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _addKeyButton.Name         = "_addKeyButton";
        _addKeyButton.Text         = "+ Key";
        _addKeyButton.ToolTipText  = "Add a new translation key";
        _addKeyButton.Click       += OnAddKeyClick;

        // _removeKeyButton
        _removeKeyButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _removeKeyButton.Enabled      = false;
        _removeKeyButton.Name         = "_removeKeyButton";
        _removeKeyButton.Text         = "- Key";
        _removeKeyButton.ToolTipText  = "Remove selected key";
        _removeKeyButton.Click       += OnRemoveKeyClick;

        // _addLocaleButton
        _addLocaleButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _addLocaleButton.Name         = "_addLocaleButton";
        _addLocaleButton.Text         = "+ Locale";
        _addLocaleButton.ToolTipText  = "Add a new locale column";
        _addLocaleButton.Click       += OnAddLocaleClick;

        // _importButton
        _importButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _importButton.Name         = "_importButton";
        _importButton.Text         = "Import...";
        _importButton.ToolTipText  = "Import a .json locale file";
        _importButton.Click       += OnImportClick;

        // _exportCsvButton
        _exportCsvButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _exportCsvButton.Name         = "_exportCsvButton";
        _exportCsvButton.Text         = "Export CSV";
        _exportCsvButton.ToolTipText  = "Export all keys as CSV";
        _exportCsvButton.Click       += OnExportCsvClick;

        // _saveButton
        _saveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _saveButton.Name         = "_saveButton";
        _saveButton.Text         = "Save";
        _saveButton.ToolTipText  = "Save all locale files";
        _saveButton.Click       += OnSaveClick;

        // _toolStrip
        _toolStrip.Dock      = DockStyle.Top;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
        {
            _addKeyButton, _removeKeyButton, _toolSep1,
            _addLocaleButton, _toolSep2,
            _importButton, _exportCsvButton, _toolSep3,
            _saveButton,
        });
        _toolStrip.Name = "_toolStrip";

        // _filterBox
        _filterBox.Dock            = DockStyle.Top;
        _filterBox.Font            = new System.Drawing.Font("Segoe UI", 9f);
        _filterBox.Name            = "_filterBox";
        _filterBox.PlaceholderText = "Filter keys...";
        _filterBox.Height          = 22;
        _filterBox.TextChanged    += OnFilterChanged;

        // _grid
        _grid.AllowUserToAddRows      = false;
        _grid.AllowUserToDeleteRows   = false;
        _grid.AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor         = System.Drawing.SystemColors.Control;
        _grid.BorderStyle             = BorderStyle.None;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.Dock                    = DockStyle.Fill;
        _grid.Name                    = "_grid";
        _grid.RowHeadersWidth         = 4;
        _grid.SelectionMode           = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect             = false;
        _grid.CellEndEdit            += OnCellEndEdit;
        _grid.SelectionChanged       += OnGridSelectionChanged;

        // _statusLabel
        _statusLabel.Dock       = DockStyle.Bottom;
        _statusLabel.Font       = new System.Drawing.Font("Segoe UI", 7.5f);
        _statusLabel.ForeColor  = System.Drawing.SystemColors.GrayText;
        _statusLabel.Height     = 18;
        _statusLabel.Name       = "_statusLabel";
        _statusLabel.Padding    = new System.Windows.Forms.Padding(4, 0, 0, 0);
        _statusLabel.Text       = "0 keys, 0 locales";

        // _folderTree
        _folderTree.Dock          = DockStyle.Fill;
        _folderTree.HideSelection = false;
        _folderTree.ShowLines     = true;
        _folderTree.ShowPlusMinus = true;
        _folderTree.BorderStyle   = BorderStyle.None;
        _folderTree.Name          = "_folderTree";
        _folderTree.AfterSelect  += OnFolderSelected;

        // _mainSplit — Panel1: árbol de carpetas | Panel2: editor
        _mainSplit.Dock             = DockStyle.Fill;
        _mainSplit.Orientation      = Orientation.Vertical;
        _mainSplit.SplitterDistance = 180;
        _mainSplit.Name             = "_mainSplit";
        _mainSplit.Panel1.Controls.Add(_folderTree);
        _mainSplit.Panel2.Controls.Add(_grid);
        _mainSplit.Panel2.Controls.Add(_filterBox);
        _mainSplit.Panel2.Controls.Add(_toolStrip);
        _mainSplit.Panel2.Controls.Add(_statusLabel);

        Controls.Add(_mainSplit);
        Font = new System.Drawing.Font("Segoe UI", 9f);
        Name = "LocalizationBrowserPanel";

        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        _mainSplit.Panel1.ResumeLayout(false);
        _mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_mainSplit).EndInit();
        _mainSplit.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>Conecta las suscripciones al bus de eventos e inyecta el contexto del editor.</summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    /// <inheritdoc/>
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_context is not null)
            _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);

        base.OnHandleDestroyed(e);
    }

    #region Event bus handlers

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }

        if (evt.Project is null || string.IsNullOrEmpty(evt.Project.LocalizationPath))
        {
            _localizationRoot = string.Empty;
            _folderTree.Nodes.Clear();
            _model = null;
            RebuildGrid();
            return;
        }

        _localizationRoot = evt.Project.LocalizationPath;
        BuildFolderTree(_localizationRoot);
    }

    #endregion

    #region Folder tree

    private void OnFolderSelected(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not string path) return;
        _ = LoadFromFolderAsync(path);
    }

    private void BuildFolderTree(string rootPath)
    {
        _folderTree.BeginUpdate();
        _folderTree.Nodes.Clear();

        if (!Directory.Exists(rootPath))
        {
            _folderTree.EndUpdate();
            return;
        }

        string name = Path.GetFileName(rootPath);
        if (string.IsNullOrEmpty(name)) name = rootPath;

        TreeNode root = new(name) { Tag = rootPath };
        PopulateFolderNodes(root.Nodes, rootPath);
        _folderTree.Nodes.Add(root);
        root.Expand();

        _folderTree.EndUpdate();
        _folderTree.SelectedNode = root;
    }

    private static void PopulateFolderNodes(TreeNodeCollection nodes, string path)
    {
        try
        {
            string[] dirs = Directory.GetDirectories(path);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dirName = Path.GetFileName(dirs[i]);
                if (string.IsNullOrEmpty(dirName)) continue;
                TreeNode node = new(dirName) { Tag = dirs[i] };
                PopulateFolderNodes(node.Nodes, dirs[i]);
                nodes.Add(node);
            }
        }
        catch (UnauthorizedAccessException) { }
    }

    #endregion

    #region Load / Rebuild

    private async Task LoadFromFolderAsync(string folderPath)
    {
        _currentLocalizationPath = folderPath;

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            _model = null;
            RebuildGrid();
            return;
        }

        _model = await LocalizationEditorModel.LoadAsync(folderPath).ConfigureAwait(true);
        _context?.EventBus.Publish(new LocalizationLoadedEvent(_model));
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        _grid.SuspendLayout();
        _grid.Columns.Clear();
        _grid.Rows.Clear();

        if (_model is null || _model.Keys.Count == 0)
        {
            _grid.ResumeLayout();
            UpdateStatusLabel();
            return;
        }

        // Columna de clave (solo lectura)
        DataGridViewTextBoxColumn keyCol = new()
        {
            Name         = "Key",
            HeaderText   = "Key",
            ReadOnly     = true,
            MinimumWidth = 160,
            FillWeight   = 30,
        };
        keyCol.DefaultCellStyle.BackColor = System.Drawing.SystemColors.ControlLight;
        _grid.Columns.Add(keyCol);

        // Una columna editable por idioma
        for (int i = 0; i < _model.Locales.Count; i++)
        {
            string locale = _model.Locales[i];
            DataGridViewTextBoxColumn localeCol = new()
            {
                Name        = locale,
                HeaderText  = locale,
                ReadOnly    = false,
                MinimumWidth = 80,
            };
            _grid.Columns.Add(localeCol);
        }

        // Filas — una por clave
        for (int r = 0; r < _model.Keys.Count; r++)
        {
            string key = _model.Keys[r];
            int rowIdx = _grid.Rows.Add();
            DataGridViewRow row = _grid.Rows[rowIdx];
            row.Tag = key;
            row.Cells["Key"].Value = key;

            for (int c = 0; c < _model.Locales.Count; c++)
            {
                string locale = _model.Locales[c];
                row.Cells[locale].Value = _model.GetValue(locale, key);
            }
        }

        ApplyFilter(_activeFilter);

        _grid.ResumeLayout();
        UpdateStatusLabel();
    }

    private void ApplyFilter(string filter)
    {
        bool hasFilter = !string.IsNullOrEmpty(filter);

        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            DataGridViewRow row = _grid.Rows[i];
            string key = row.Tag as string ?? string.Empty;
            row.Visible = !hasFilter ||
                key.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Toolbar handlers

    private void OnAddKeyClick(object? sender, EventArgs e)
    {
        if (_model is null) return;

        string? key = PromptForInput("New Key", "Enter the translation key:");
        if (key is null || string.IsNullOrWhiteSpace(key)) return;

        _model.AddKey(key.Trim());
        RebuildGrid();
    }

    private void OnRemoveKeyClick(object? sender, EventArgs e)
    {
        if (_model is null || _grid.SelectedRows.Count == 0) return;

        string key = _grid.SelectedRows[0].Tag as string ?? string.Empty;
        if (string.IsNullOrEmpty(key)) return;

        DialogResult confirm = MessageBox.Show(this,
            $"Remove key '{key}' from all locales?",
            "Remove Key", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _model.RemoveKey(key);
        RebuildGrid();
    }

    private void OnAddLocaleClick(object? sender, EventArgs e)
    {
        if (_model is null) return;

        string? locale = PromptForInput("New Locale", "Enter the locale identifier (e.g. fr, de, pt-BR):");
        if (locale is null || string.IsNullOrWhiteSpace(locale)) return;

        _model.AddLocale(locale.Trim());
        RebuildGrid();
    }

    private async void OnImportClick(object? sender, EventArgs e)
    {
        if (_context?.ActiveProject is null) return;

        string initialDir = !string.IsNullOrEmpty(_currentLocalizationPath) && Directory.Exists(_currentLocalizationPath)
            ? _currentLocalizationPath
            : Directory.Exists(_context.ActiveProject.LocalizationPath)
                ? _context.ActiveProject.LocalizationPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        using OpenFileDialog dlg = new()
        {
            Title            = "Import Locale File",
            Filter           = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = initialDir,
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        string locale = Path.GetFileNameWithoutExtension(dlg.FileName);
        try
        {
            string json = await File.ReadAllTextAsync(dlg.FileName).ConfigureAwait(true);
            Dictionary<string, string>? entries =
                JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (entries is null)
            {
                MessageBox.Show(this, "Invalid JSON format.", "Import Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string loadPath = string.IsNullOrEmpty(_currentLocalizationPath)
                ? _context.ActiveProject.LocalizationPath
                : _currentLocalizationPath;

            if (_model is null)
                _model = await LocalizationEditorModel.LoadAsync(loadPath).ConfigureAwait(true);

            _model.AddLocale(locale);
            foreach (System.Collections.Generic.KeyValuePair<string, string> kv in entries)
            {
                _model.AddKey(kv.Key);
                _model.SetValue(locale, kv.Key, kv.Value);
            }

            RebuildGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnExportCsvClick(object? sender, EventArgs e)
    {
        if (_model is null) return;

        using SaveFileDialog dlg = new()
        {
            Title  = "Export as CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            System.Text.StringBuilder sb = new();

            // Encabezado
            sb.Append("Key");
            for (int i = 0; i < _model.Locales.Count; i++)
            {
                sb.Append(',');
                sb.Append(EscapeCsv(_model.Locales[i]));
            }
            sb.AppendLine();

            // Filas
            for (int r = 0; r < _model.Keys.Count; r++)
            {
                string key = _model.Keys[r];
                sb.Append(EscapeCsv(key));
                for (int c = 0; c < _model.Locales.Count; c++)
                {
                    sb.Append(',');
                    sb.Append(EscapeCsv(_model.GetValue(_model.Locales[c], key)));
                }
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(dlg.FileName, sb.ToString()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        if (_model is null) return;

        try
        {
            await _model.SaveAsync().ConfigureAwait(true);
            _context?.Logger.Log("[Localization] Saved.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Grid handlers

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_model is null || _context is null) return;
        if (e.RowIndex < 0 || e.ColumnIndex < 1) return;

        DataGridViewRow row = _grid.Rows[e.RowIndex];
        string key = row.Tag as string ?? string.Empty;
        if (string.IsNullOrEmpty(key)) return;

        string locale   = _grid.Columns[e.ColumnIndex].Name;
        string newValue = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string ?? string.Empty;
        string oldValue = _model.GetValue(locale, key);

        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;

        SetLocalizationValueCommand cmd = new(_model, locale, key, oldValue, newValue);
        _context.Commands.Execute(cmd);
    }

    private void OnGridSelectionChanged(object? sender, EventArgs e)
    {
        _removeKeyButton.Enabled = _model is not null && _grid.SelectedRows.Count > 0;
    }

    #endregion

    #region Filter

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        _activeFilter = _filterBox.Text.Trim();
        ApplyFilter(_activeFilter);
    }

    #endregion

    #region Helpers

    private void UpdateStatusLabel()
    {
        int keys    = _model?.Keys.Count ?? 0;
        int locales = _model?.Locales.Count ?? 0;
        _statusLabel.Text = $"{keys} key{(keys == 1 ? "" : "s")}, {locales} locale{(locales == 1 ? "" : "s")}";
    }

    private string? PromptForInput(string title, string prompt)
    {
        using Form form = new()
        {
            Text          = title,
            ClientSize    = new System.Drawing.Size(340, 90),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox   = false,
            MinimizeBox   = false,
        };

        Label lbl = new() { Text = prompt, Bounds = new System.Drawing.Rectangle(8, 8, 320, 20) };
        TextBox tb = new() { Bounds = new System.Drawing.Rectangle(8, 32, 320, 22) };
        Button ok = new() { Text = "OK", DialogResult = DialogResult.OK,
            Bounds = new System.Drawing.Rectangle(172, 60, 75, 24) };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel,
            Bounds = new System.Drawing.Rectangle(253, 60, 75, 24) };

        form.Controls.AddRange(new System.Windows.Forms.Control[] { lbl, tb, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        return form.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    #endregion
}
