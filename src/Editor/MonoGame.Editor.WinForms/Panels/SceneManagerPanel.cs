namespace MonoGame.Editor.WinForms.Panels;

/// <summary>Panel that lists all scenes in the active project and allows loading/creating/deleting them.</summary>
public sealed class SceneManagerPanel : UserControl
{
    private EditorContext? _context;
    private System.Drawing.Font? _boldFont;

    private readonly ToolStrip _toolStrip;
    private readonly ToolStripButton _newSceneButton;
    private readonly ToolStripButton _openSceneButton;
    private readonly ToolStripButton _deleteSceneButton;
    private readonly ListView _sceneList;
    private readonly ColumnHeader _nameColumn;
    private readonly ColumnHeader _modifiedColumn;
    private readonly Label _statusLabel;

    /// <summary>Initializes the panel layout.</summary>
    public SceneManagerPanel()
    {
        _toolStrip        = new ToolStrip();
        _newSceneButton   = new ToolStripButton();
        _openSceneButton  = new ToolStripButton();
        _deleteSceneButton = new ToolStripButton();
        _sceneList        = new ListView();
        _nameColumn       = new ColumnHeader();
        _modifiedColumn   = new ColumnHeader();
        _statusLabel      = new Label();

        _toolStrip.SuspendLayout();
        SuspendLayout();

        // _newSceneButton
        _newSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _newSceneButton.Name = "_newSceneButton";
        _newSceneButton.Text = "+ New";
        _newSceneButton.ToolTipText = "Create a new scene";
        _newSceneButton.Click += OnNewSceneClick;

        // _openSceneButton
        _openSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _openSceneButton.Name = "_openSceneButton";
        _openSceneButton.Text = "Open...";
        _openSceneButton.ToolTipText = "Open a scene file";
        _openSceneButton.Click += OnOpenSceneClick;

        // _deleteSceneButton
        _deleteSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _deleteSceneButton.Enabled = false;
        _deleteSceneButton.Name = "_deleteSceneButton";
        _deleteSceneButton.Text = "Delete";
        _deleteSceneButton.ToolTipText = "Delete selected scene";
        _deleteSceneButton.Click += OnDeleteSceneClick;

        // _toolStrip
        _toolStrip.Dock = DockStyle.Top;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
        {
            _newSceneButton,
            _openSceneButton,
            new ToolStripSeparator(),
            _deleteSceneButton,
        });
        _toolStrip.Name = "_toolStrip";

        // _nameColumn
        _nameColumn.Text = "Name";
        _nameColumn.Width = 180;

        // _modifiedColumn
        _modifiedColumn.Text = "Modified";
        _modifiedColumn.Width = 130;

        // _sceneList
        _sceneList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _nameColumn, _modifiedColumn });
        _sceneList.Dock = DockStyle.Fill;
        _sceneList.FullRowSelect = true;
        _sceneList.HideSelection = false;
        _sceneList.MultiSelect = false;
        _sceneList.Name = "_sceneList";
        _sceneList.View = View.Details;
        _sceneList.DoubleClick += OnSceneListDoubleClick;
        _sceneList.SelectedIndexChanged += OnSceneListSelectedIndexChanged;

        // _statusLabel
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Font = new System.Drawing.Font("Segoe UI", 7.5f);
        _statusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _statusLabel.Height = 18;
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
        _statusLabel.Text = "0 scenes in project";

        Controls.Add(_sceneList);
        Controls.Add(_toolStrip);
        Controls.Add(_statusLabel);
        Font = new System.Drawing.Font("Segoe UI", 9f);
        Name = "SceneManagerPanel";

        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        ResumeLayout(false);
    }

    /// <summary>Wires event-bus subscriptions and injects the editor context.</summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
        context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
        context.EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_context is not null)
        {
            _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
            _context.EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
        }
        _boldFont?.Dispose();
        _boldFont = null;
        base.OnHandleDestroyed(e);
    }

    #region Event handlers

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }
        PopulateSceneList(evt.Project);
    }

    private void OnSceneLoaded(SceneLoadedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnSceneLoaded(evt)); return; }
        UpdateActiveSceneBold();
    }

    private void OnSceneListSelectedIndexChanged(object? sender, EventArgs e)
    {
        _deleteSceneButton.Enabled = _sceneList.SelectedItems.Count > 0;
    }

    private async void OnNewSceneClick(object? sender, EventArgs e)
    {
        if (_context is null) return;

        using NewSceneDialog dlg = new();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        EditorScene scene = new()
        {
            Name      = dlg.SceneName,
            WorldSize = new EditorVector2(dlg.WorldWidth, dlg.WorldHeight),
        };

        EditorProject? project = _context.ActiveProject;
        if (project is not null && !string.IsNullOrEmpty(project.ScenesPath))
        {
            Directory.CreateDirectory(project.ScenesPath);
            string safeName = string.Concat(scene.Name.Split(Path.GetInvalidFileNameChars()));
            string path = Path.Combine(project.ScenesPath, safeName + ".scene.json");
            scene.ScenePath = path;
            try
            {
                await SceneSerializer.SaveAsync(scene, path).ConfigureAwait(true);
            }
            catch { /* non-fatal — scene is open in memory */ }
        }

        _context.SetActiveScene(scene);
        _context.EventBus.Publish(new SceneCreatedEvent(scene));
        PopulateSceneList(_context.ActiveProject);
    }

    private async void OnOpenSceneClick(object? sender, EventArgs e)
    {
        if (_context is null) return;

        EditorProject? project = _context.ActiveProject;
        string initialDir = project is not null && Directory.Exists(project.ScenesPath)
            ? project.ScenesPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        using OpenFileDialog dlg = new()
        {
            Title            = "Open Scene",
            Filter           = "Scene files (*.scene.json)|*.scene.json|All files (*.*)|*.*",
            InitialDirectory = initialDir,
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!await ConfirmDiscardDirtyAsync()) return;

        EditorScene? scene = await SceneSerializer.LoadAsync(dlg.FileName).ConfigureAwait(true);
        if (scene is null) return;

        scene.ScenePath = dlg.FileName;
        if (string.IsNullOrEmpty(scene.Name))
            scene.Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(dlg.FileName));

        _context.SetActiveScene(scene);
        UpdateActiveSceneBold();
    }

    private async void OnDeleteSceneClick(object? sender, EventArgs e)
    {
        if (_context is null || _sceneList.SelectedItems.Count == 0) return;

        ListViewItem item = _sceneList.SelectedItems[0];
        string? path = item.Tag as string;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        DialogResult confirm = MessageBox.Show(this,
            $"Delete scene '{item.Text}'?\n\nThis action cannot be undone.",
            "Delete Scene", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        if (string.Equals(_context.ActiveScene?.ScenePath, path, StringComparison.OrdinalIgnoreCase))
            _context.SetActiveScene(null);

        try
        {
            await Task.Run(() => File.Delete(path)).ConfigureAwait(true);
            _sceneList.Items.Remove(item);
            UpdateStatusLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to delete scene:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnSceneListDoubleClick(object? sender, EventArgs e)
    {
        if (_context is null || _sceneList.SelectedItems.Count == 0) return;

        ListViewItem item = _sceneList.SelectedItems[0];
        string? path = item.Tag as string;
        if (string.IsNullOrEmpty(path)) return;

        if (!await ConfirmDiscardDirtyAsync()) return;

        EditorScene? scene = await SceneSerializer.LoadAsync(path).ConfigureAwait(true);
        if (scene is null) return;

        scene.ScenePath = path;
        if (string.IsNullOrEmpty(scene.Name))
            scene.Name = item.Text;

        _context.SetActiveScene(scene);
        UpdateActiveSceneBold();
    }

    #endregion

    #region Helpers

    private void PopulateSceneList(EditorProject? project)
    {
        _sceneList.Items.Clear();

        if (project is null || !Directory.Exists(project.ScenesPath))
        {
            _statusLabel.Text = "0 scenes in project";
            return;
        }

        string[] files = Directory.GetFiles(project.ScenesPath, "*.scene.json");
        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];
            string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
            string modified = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");

            ListViewItem item = new(name);
            item.SubItems.Add(modified);
            item.Tag = path;
            _sceneList.Items.Add(item);
        }

        UpdateActiveSceneBold();
        UpdateStatusLabel();
    }

    private void UpdateActiveSceneBold()
    {
        string? activePath = _context?.ActiveScene?.ScenePath;
        _boldFont ??= new System.Drawing.Font(_sceneList.Font, System.Drawing.FontStyle.Bold);

        for (int i = 0; i < _sceneList.Items.Count; i++)
        {
            ListViewItem item = _sceneList.Items[i];
            bool isActive = activePath is not null &&
                string.Equals(item.Tag as string, activePath, StringComparison.OrdinalIgnoreCase);
            item.Font = isActive ? _boldFont : _sceneList.Font;
        }
    }

    private void UpdateStatusLabel()
    {
        int count = _sceneList.Items.Count;
        _statusLabel.Text = $"{count} scene{(count == 1 ? "" : "s")} in project";
    }

    private Task<bool> ConfirmDiscardDirtyAsync()
    {
        if (_context is null || !_context.IsSceneDirty)
            return Task.FromResult(true);

        DialogResult answer = MessageBox.Show(this,
            "The current scene has unsaved changes. Do you want to discard them?",
            "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        return Task.FromResult(answer == DialogResult.Yes);
    }

    #endregion
}
