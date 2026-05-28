using System.Diagnostics;

namespace MonoGame.Editor.WinForms;

/// <summary>Main editor window. Logic and event wiring.</summary>
public sealed partial class EditorForm : Form
{
    private enum SceneViewMode
    {
        TwoD,
        TwoPointFiveD,
    }

    private readonly EditorContext _context = null!;
    private readonly EditorPreferences _preferences = null!;
    private readonly GameObjectRegistry _registry = null!;
    private readonly PrefabManager  _prefabManager   = new();
    private readonly GizmoController _gizmoCtrl      = new();
    private GizmoRenderer?           _gizmoRenderer;
    private EditModeRenderer?        _editRenderer;
    private NavGridPreviewRenderer?       _navGridRenderer;
    private ResolutionPreviewRenderer?    _resRenderer;
    private PlayModeRunner?          _playRunner;
    private readonly ContentWatcher  _contentWatcher  = null!;
    private ToolStripMenuItem        _saveSceneMenuItem   = null!;
    private ToolStripMenuItem        _saveSceneAsMenuItem = null!;
    private ToolStripMenuItem        _newSceneMenuItem    = null!;
    private ToolStripMenuItem        _openRecentMenuItem  = null!;
    private ProjectSettings?         _projectSettings;
    private readonly ICodeGenService _codeGenService = new SceneCodeGenerator();
    private SceneViewMode _sceneViewMode = SceneViewMode.TwoD;
    private bool _handToolEnabled;
    private readonly HashSet<Keys> _pressedNavigationKeys = [];
    private bool _tilemapPainting;

    // FPS counter (written/read on render thread; label updated on UI thread via BeginInvoke)
    private double _fpsAccumTime;
    private int    _fpsFrameCount;
    private float  _fpsCurrent;

    // Default clear color for the scene viewport
    private static readonly Microsoft.Xna.Framework.Color DefaultClearColor = new(30, 30, 30);

    /// <summary>Designer-only constructor.</summary>
    public EditorForm() => InitializeComponent();

    public EditorForm(EditorContext context)
    {
        _context   = context;
        _registry  = new GameObjectRegistry();
        _preferences = new EditorPreferences();
        _preferences.Load();
        _contentWatcher = new ContentWatcher(context.EventBus);

        InitializeComponent();
        WireEvents();
    }

    #region Startup / Shutdown

    private void ApplyPreferences()
    {
        _outerSplit.Panel1MinSize = 180;
        _innerSplit.Panel2MinSize = 220;
        _mainSplit.Panel2MinSize  = 80;

        int leftWidth = _preferences.LeftPanelWidth > 0 ? _preferences.LeftPanelWidth : 220;
        _outerSplit.SplitterDistance = ClampSplitter(leftWidth, 180, _outerSplit.Width - 180);

        int rightWidth = _preferences.RightPanelWidth > 0 ? _preferences.RightPanelWidth : 280;
        _innerSplit.SplitterDistance = ClampSplitter(_innerSplit.Width - rightWidth, 320, _innerSplit.Width - 220);

        int bottomH = _preferences.ConsolePanelHeight > 0 ? _preferences.ConsolePanelHeight : 200;
        _mainSplit.SplitterDistance = ClampSplitter(_mainSplit.Height - bottomH, 240, _mainSplit.Height - 80);

        _viewHierarchyMenuItem.Checked    = _preferences.HierarchyVisible;
        _viewInspectorMenuItem.Checked    = _preferences.InspectorVisible;
        _viewAssetBrowserMenuItem.Checked = _preferences.AssetBrowserVisible;
        _viewConsoleMenuItem.Checked      = _preferences.ConsoleVisible;
        _viewSceneManagerMenuItem.Checked = _preferences.SceneManagerVisible;
        _viewLocalizationMenuItem.Checked = _preferences.LocalizationBrowserVisible;
        _viewInputMapEditorMenuItem.Checked = _preferences.InputMapEditorVisible;
        _viewTilemapPaletteMenuItem.Checked = _preferences.TilemapPaletteVisible;
        _viewUndoHistoryMenuItem.Checked = _preferences.UndoHistoryVisible;
        _assetBrowserPanel.SplitterDistance = _preferences.AssetBrowserSplitterDistance;

        UpdatePanelVisibility();
    }

    private static int ClampSplitter(int value, int min, int max)
        => max < min ? min : Math.Clamp(value, min, max);

    private void CenterPlaybackStrip()
    {
        // Center relative to full toolbar width (not just the fill cell)
        int x = (_toolbarTable.Width - _playbackStrip.Width) / 2 - _playbackCell.Left;
        int y = (_playbackCell.Height - _playbackStrip.Height) / 2;
        _playbackStrip.Location = new System.Drawing.Point(Math.Max(0, x), Math.Max(0, y));
    }

    private void WireEvents()
    {
        Shown  += (_, _) => { ApplyPreferences(); CenterPlaybackStrip(); _registry.Scan(); };
        KeyPreview = true;
        KeyDown    += OnFormKeyDown;

        _context.EventBus.Subscribe<EditorStateChangedEvent>(OnEditorStateChanged);
        _context.EventBus.Subscribe<UndoPerformedEvent>(OnUndoPerformed);
        _context.EventBus.Subscribe<RedoPerformedEvent>(OnRedoPerformed);
        _context.EventBus.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
        _context.EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
        _context.EventBus.Subscribe<SceneDirtyChangedEvent>(OnSceneDirtyChanged);

        FormClosing += OnFormClosing;
        _viewport.RenderFrame += OnViewportRenderFrame;
        _gameViewport.RenderFrame += OnGameViewportRenderFrame;
        _gameViewport.ClearColor = new Microsoft.Xna.Framework.Color(15, 15, 25); // dark blue — distinct from scene
        _gameViewport.IsActive   = false; // game tab not selected at startup
        _centerTabControl.SelectedIndexChanged += OnCenterTabChanged;
        _toolbarTable.Resize  += (_, _) => CenterPlaybackStrip();

        // Gizmo mouse interaction
        _viewport.MouseDown += OnViewportMouseDown;
        _viewport.MouseMove += OnViewportMouseMove;
        _viewport.MouseUp   += OnViewportMouseUp;

        // Initialize gizmo renderer (GPU resources allocated lazily on first render)
        _gizmoRenderer = new GizmoRenderer(_gizmoCtrl);

        // Initialize panels
        _consolePanel.Initialize(_context);
        _hierarchyPanel.Initialize(_context, _prefabManager);
        _inspectorPanel.Initialize(_context, _registry, _prefabManager);
        _assetBrowserPanel.Initialize(_context);
        _sceneManagerPanel.Initialize(_context);
        _localizationPanel.Initialize(_context);
        _inputMapEditorPanel.Initialize(_context);
        _tilemapPalettePanel.Initialize(_context, _context.EventBus);
        _undoHistoryPanel.Initialize(_context, _context.EventBus);

        _context.EventBus.Subscribe<BehaviourAddedEvent>(OnBehaviourAdded);
        _context.EventBus.Subscribe<InputMapLoadedEvent>(OnInputMapLoaded);
        _context.EventBus.Subscribe<LocalizationLoadedEvent>(OnLocalizationLoaded);

        // Build menus programmatically (avoids Designer.cs C#-version concerns)
        BuildFileMenuExtras();
        BuildProjectMenu();
    }

    private void SavePreferences()
    {
        _preferences.LeftPanelWidth               = _outerSplit.SplitterDistance;
        _preferences.RightPanelWidth              = _innerSplit.Width - _innerSplit.SplitterDistance;
        _preferences.ConsolePanelHeight           = _mainSplit.Height - _mainSplit.SplitterDistance;
        _preferences.AssetBrowserSplitterDistance = _assetBrowserPanel.SplitterDistance;
        _preferences.HierarchyVisible    = _viewHierarchyMenuItem.Checked;
        _preferences.InspectorVisible    = _viewInspectorMenuItem.Checked;
        _preferences.AssetBrowserVisible = _viewAssetBrowserMenuItem.Checked;
        _preferences.ConsoleVisible      = _viewConsoleMenuItem.Checked;
        _preferences.SceneManagerVisible = _viewSceneManagerMenuItem.Checked;
        _preferences.LocalizationBrowserVisible = _viewLocalizationMenuItem.Checked;
        _preferences.InputMapEditorVisible = _viewInputMapEditorMenuItem.Checked;
        _preferences.TilemapPaletteVisible = _viewTilemapPaletteMenuItem.Checked;
        _preferences.UndoHistoryVisible = _viewUndoHistoryMenuItem.Checked;
        _preferences.Save();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _context.EventBus.Unsubscribe<EditorStateChangedEvent>(OnEditorStateChanged);
        _context.EventBus.Unsubscribe<UndoPerformedEvent>(OnUndoPerformed);
        _context.EventBus.Unsubscribe<RedoPerformedEvent>(OnRedoPerformed);
        _context.EventBus.Unsubscribe<ProjectOpenedEvent>(OnProjectOpened);
        _context.EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
        _context.EventBus.Unsubscribe<SceneDirtyChangedEvent>(OnSceneDirtyChanged);
        _context.EventBus.Unsubscribe<BehaviourAddedEvent>(OnBehaviourAdded);
        _context.EventBus.Unsubscribe<InputMapLoadedEvent>(OnInputMapLoaded);
        _context.EventBus.Unsubscribe<LocalizationLoadedEvent>(OnLocalizationLoaded);
        _contentWatcher.Dispose();
        _gizmoRenderer?.Dispose();
        _editRenderer?.Dispose();
        _navGridRenderer?.Dispose();
        _resRenderer?.Dispose();
        base.OnFormClosed(e);
    }

    #endregion

    #region Keyboard shortcuts

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            _context.Commands.Undo();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            _context.Commands.Redo();
            e.Handled = true;
        }
        else if (!e.Control && !e.Alt)
        {
            switch (e.KeyCode)
            {
                case Keys.Q: SetGizmoMode(GizmoMode.Select); e.Handled = true; break;
                case Keys.W: SetGizmoMode(GizmoMode.Move);   e.Handled = true; break;
                case Keys.E: SetGizmoMode(GizmoMode.Rotate); e.Handled = true; break;
                case Keys.R: SetGizmoMode(GizmoMode.Scale);  e.Handled = true; break;
                case Keys.T: SetGizmoMode(GizmoMode.Rect);   e.Handled = true; break;
                case Keys.H: ToggleHandTool();               e.Handled = true; break;
                case Keys.G: _gizmoCtrl.ShowGrid = !_gizmoCtrl.ShowGrid; e.Handled = true; break;
            }
        }

        if (IsNavigationKey(e.KeyCode))
            _pressedNavigationKeys.Add(e.KeyCode);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _pressedNavigationKeys.Remove(e.KeyCode);
    }

    private static bool IsNavigationKey(Keys key)
        => key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.ShiftKey;

    #endregion

    #region Toolbar — Play / Pause / Stop

    private async void OnPlayClick(object? sender, EventArgs e)
    {
        if (_context.ActiveScene is null)
        {
            MessageBox.Show(this, "No scene is open.", "Play", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        EditorState next = _context.State switch
        {
            EditorState.Editing => EditorState.Playing,
            EditorState.Paused  => EditorState.Playing,
            _                   => EditorState.Playing,
        };

        if (next == EditorState.Playing && _context.State == EditorState.Editing)
            await BuildContentIfNeededAsync().ConfigureAwait(true);

        _context.SetState(next);
    }

    private async Task BuildContentIfNeededAsync()
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null) return;

        string mgcbFile = Path.Combine(project.ContentPath, "Content.mgcb");
        if (!File.Exists(mgcbFile)) return;

        _consolePanel.AppendLine("[Play] Building content before play…");
        _statusLabel.Text = "Building content…";

        try
        {
            int exitCode = await MgcbRunner.RunAsync(mgcbFile,
                line => BeginInvoke(() => _consolePanel.AppendLine(line))).ConfigureAwait(true);

            if (exitCode != 0)
            {
                _consolePanel.AppendLine($"[Play] Content build failed (exit {exitCode}). Aborting play.");
                _statusLabel.Text = "Build failed.";
                throw new InvalidOperationException($"Content build failed with exit code {exitCode}.");
            }

            _statusLabel.Text = "Build succeeded.";
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Play] Build error: {ex.Message}");
            _statusLabel.Text = "Build error.";
        }
    }

    private void OnPauseClick(object? sender, EventArgs e)
    {
        if (_context.State == EditorState.Playing)
            _context.SetState(EditorState.Paused);
    }

    private void OnStopClick(object? sender, EventArgs e)
    {
        if (_context.State != EditorState.Editing)
            _context.SetState(EditorState.Editing);
    }

    private void OnEditorStateChanged(EditorStateChangedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnEditorStateChanged(evt)); return; }

        UpdatePlaybackButtons(evt.NewState);
        _statusLabel.Text = evt.NewState switch
        {
            EditorState.Editing => "Editing",
            EditorState.Playing => "Playing",
            EditorState.Paused  => "Paused",
            _                   => string.Empty,
        };

        switch (evt.NewState)
        {
            case EditorState.Playing when evt.OldState == EditorState.Editing:
                StartPlayMode();
                break;
            case EditorState.Editing:
                StopPlayMode();
                break;
        }
    }

    private void StartPlayMode()
    {
        try
        {
            _context.TakePlaySnapshot();
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"[PlayMode] Failed to snapshot scene: {ex.Message}");
            MessageBox.Show(this, $"Could not start play mode: {ex.Message}", "Play Mode Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _playRunner = new PlayModeRunner(_context.ActiveScene!, _registry);
        _context.Logger.Log("[PlayMode] Started.", LogLevel.Info);
        _viewport.IsActive = false;
        _gameViewport.IsActive = true;
        _centerTabControl.SelectedTab = _gameTab;
        _gameViewport.BringToFront();
        _gameViewport.Invalidate();
    }

    private void StopPlayMode()
    {
        _playRunner?.Dispose();
        _playRunner = null;

        EditorScene? restored = null;
        try
        {
            restored = _context.RestoreFromSnapshot();
        }
        catch (Exception ex)
        {
            _context.Logger.LogError($"[PlayMode] Failed to restore snapshot: {ex.Message}");
        }

        _context.ClearPlaySnapshot();

        if (restored is not null)
            _context.SetActiveScene(restored);

        _context.Logger.Log("[PlayMode] Stopped — scene restored.", LogLevel.Info);
        _gameViewport.IsActive = false;
        _viewport.IsActive = true;
        _centerTabControl.SelectedTab = _sceneTab;
        _viewport.BringToFront();
        _viewport.Invalidate();
        _sceneTab.Invalidate();
        _centerTabControl.Invalidate();
    }

    private void UpdatePlaybackButtons(EditorState state)
    {
        System.Drawing.Color accent = System.Drawing.Color.FromArgb(0, 122, 204);
        System.Drawing.Color normal = System.Drawing.SystemColors.Control;

        _playButton.BackColor  = state == EditorState.Playing ? accent : normal;
        _pauseButton.BackColor = state == EditorState.Paused  ? accent : normal;
        _stopButton.BackColor  = normal;

        _playButton.Enabled  = state != EditorState.Playing;
        _pauseButton.Enabled = state == EditorState.Playing;
        _stopButton.Enabled  = state != EditorState.Editing;
    }

    #endregion

    #region Toolbar — Gizmo modes

    private void OnGizmoModeClick(object? sender, EventArgs e)
    {
        GizmoMode mode = sender switch
        {
            _ when ReferenceEquals(sender, _moveModeButton)   => GizmoMode.Move,
            _ when ReferenceEquals(sender, _rotateModeButton) => GizmoMode.Rotate,
            _ when ReferenceEquals(sender, _scaleModeButton)  => GizmoMode.Scale,
            _ when ReferenceEquals(sender, _rectModeButton)   => GizmoMode.Rect,
            _                                                  => GizmoMode.Select,
        };
        SetGizmoMode(mode);
    }

    private void SetGizmoMode(GizmoMode mode)
    {
        _gizmoCtrl.Mode = mode;
        _selectModeButton.Checked = mode == GizmoMode.Select;
        _moveModeButton.Checked   = mode == GizmoMode.Move;
        _rotateModeButton.Checked = mode == GizmoMode.Rotate;
        _scaleModeButton.Checked  = mode == GizmoMode.Scale;
        _rectModeButton.Checked   = mode == GizmoMode.Rect;

        if (_handToolEnabled)
            ToggleHandTool(false);
    }

    private void OnHandModeClick(object? sender, EventArgs e) => ToggleHandTool();

    private void ToggleHandTool(bool? force = null)
    {
        bool next = force ?? !_handToolEnabled;
        _handToolEnabled = next;
        _handModeButton.Checked = next;
        _viewport.HandToolEnabled = next;
    }

    private void OnSceneViewModeClick(object? sender, EventArgs e)
    {
        _sceneViewMode = _sceneViewMode == SceneViewMode.TwoD
            ? SceneViewMode.TwoPointFiveD
            : SceneViewMode.TwoD;

        _sceneViewModeButton.Text = _sceneViewMode == SceneViewMode.TwoD
            ? "View: 2D"
            : "View: 2.5D";

        _gizmoCtrl.IsDepthMode = _sceneViewMode == SceneViewMode.TwoPointFiveD;
        _viewport.Invalidate();
    }

    private void OnSnapButtonClick(object? sender, EventArgs e)
    {
        _gizmoCtrl.SnapEnabled = _snapButton.Checked;
    }

    private void OnNavButtonClick(object? sender, EventArgs e) { /* state is read from _navButton.Checked in DrawEditorGizmos */ }

    private void OnResButtonClick(object? sender, EventArgs e) { /* state is read from _resButton.Checked in DrawEditorGizmos */ }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_context.IsSceneDirty)
        {
            DialogResult answer = MessageBox.Show(this,
                "The current scene has unsaved changes.\n\nSave before closing?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (answer == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (answer == DialogResult.Yes)
            {
                EditorScene? scene = _context.ActiveScene;
                if (scene is not null && !string.IsNullOrEmpty(scene.ScenePath))
                {
                    try
                    {
                        SceneSerializer.SaveAsync(scene, scene.ScenePath).GetAwaiter().GetResult();
                        _context.MarkSceneClean();
                    }
                    catch { /* best-effort on close */ }
                }
            }
        }

        SavePreferences();
    }

    #endregion

    #region View menu

    private void UpdatePanelVisibility()
    {
        _outerSplit.Panel1Collapsed = !_viewHierarchyMenuItem.Checked;
        _innerSplit.Panel2Collapsed = !_viewInspectorMenuItem.Checked;

        bool assetsVisible        = _viewAssetBrowserMenuItem.Checked;
        bool consoleVisible       = _viewConsoleMenuItem.Checked;
        bool sceneManagerVisible  = _viewSceneManagerMenuItem.Checked;
        bool localizationVisible  = _viewLocalizationMenuItem.Checked;
        bool inputMapVisible      = _viewInputMapEditorMenuItem.Checked;
        bool tilemapPaletteVisible = _viewTilemapPaletteMenuItem.Checked;
        bool undoHistoryVisible    = _viewUndoHistoryMenuItem.Checked;

        _bottomTabControl.TabPages.Clear();
        if (assetsVisible)         _bottomTabControl.TabPages.Add(_assetsTab);
        if (consoleVisible)        _bottomTabControl.TabPages.Add(_consoleTab);
        if (sceneManagerVisible)   _bottomTabControl.TabPages.Add(_sceneManagerTab);
        if (localizationVisible)   _bottomTabControl.TabPages.Add(_localizationTab);
        if (inputMapVisible)       _bottomTabControl.TabPages.Add(_inputMapEditorTab);
        if (tilemapPaletteVisible) _bottomTabControl.TabPages.Add(_tilemapPaletteTab);
        if (undoHistoryVisible)    _bottomTabControl.TabPages.Add(_undoHistoryTab);

        _mainSplit.Panel2Collapsed = !assetsVisible && !consoleVisible && !sceneManagerVisible && !localizationVisible && !inputMapVisible && !tilemapPaletteVisible && !undoHistoryVisible;
    }

    private void OnViewMenuItemClick(object? sender, EventArgs e) => UpdatePanelVisibility();

    private void OnResetLayoutClick(object? sender, EventArgs e) => ResetLayout();

    private void ResetLayout()
    {
        EditorPreferences defaults = new();

        _viewHierarchyMenuItem.Checked    = defaults.HierarchyVisible;
        _viewInspectorMenuItem.Checked    = defaults.InspectorVisible;
        _viewAssetBrowserMenuItem.Checked = defaults.AssetBrowserVisible;
        _viewConsoleMenuItem.Checked      = defaults.ConsoleVisible;
        _viewSceneManagerMenuItem.Checked = defaults.SceneManagerVisible;
        _viewLocalizationMenuItem.Checked = defaults.LocalizationBrowserVisible;
        _viewInputMapEditorMenuItem.Checked = defaults.InputMapEditorVisible;
        _viewTilemapPaletteMenuItem.Checked = defaults.TilemapPaletteVisible;
        _viewUndoHistoryMenuItem.Checked = defaults.UndoHistoryVisible;
        UpdatePanelVisibility();

        _outerSplit.Panel1MinSize = 180;
        _innerSplit.Panel2MinSize = 220;
        _mainSplit.Panel2MinSize  = 80;

        _outerSplit.SplitterDistance = ClampSplitter(defaults.LeftPanelWidth, 180, _outerSplit.Width - 180);
        _innerSplit.SplitterDistance = ClampSplitter(_innerSplit.Width - defaults.RightPanelWidth, 320, _innerSplit.Width - 220);
        _mainSplit.SplitterDistance  = ClampSplitter(_mainSplit.Height - defaults.ConsolePanelHeight, 240, _mainSplit.Height - 80);
        _assetBrowserPanel.SplitterDistance = defaults.AssetBrowserSplitterDistance;
    }

    #endregion

    #region Menu — Edit (Undo / Redo)

    private void OnUndoClick(object? sender, EventArgs e)
    {
        _context.Commands.Undo();
        _undoHistoryPanel.Refresh(_context.Commands);
    }

    private void OnRedoClick(object? sender, EventArgs e)
    {
        _context.Commands.Redo();
        _undoHistoryPanel.Refresh(_context.Commands);
    }

    private void OnUndoPerformed(UndoPerformedEvent _)
    {
        if (InvokeRequired) { BeginInvoke(UpdateEditMenu); return; }
        UpdateEditMenu();
    }

    private void OnRedoPerformed(RedoPerformedEvent _)
    {
        if (InvokeRequired) { BeginInvoke(UpdateEditMenu); return; }
        UpdateEditMenu();
    }

    private void UpdateEditMenu()
    {
        string? undoDesc = _context.Commands.UndoDescription;
        string? redoDesc = _context.Commands.RedoDescription;
        _undoMenuItem.Text    = undoDesc is null ? "Undo" : $"Undo {undoDesc}";
        _redoMenuItem.Text    = redoDesc is null ? "Redo" : $"Redo {redoDesc}";
        _undoMenuItem.Enabled = undoDesc is not null;
        _redoMenuItem.Enabled = redoDesc is not null;
    }

    #endregion

    #region Menu — Project

    private void BuildProjectMenu()
    {
        ToolStripMenuItem buildContentItem = new()
        {
            Text             = "Build Content",
            ShortcutKeys     = Keys.F7,
            ShowShortcutKeys = true,
        };
        buildContentItem.Click += OnBuildContentClick;
        _projectMenu.DropDownItems.Add(buildContentItem);

        ToolStripMenuItem buildGameItem = new()
        {
            Text             = "Build Game",
            ShortcutKeys     = Keys.Control | Keys.B,
            ShowShortcutKeys = true,
        };
        buildGameItem.Click += OnBuildGameClick;
        _projectMenu.DropDownItems.Add(buildGameItem);

        ToolStripMenuItem runGameItem = new()
        {
            Text             = "Run Game",
            ShortcutKeys     = Keys.Control | Keys.F5,
            ShowShortcutKeys = true,
        };
        runGameItem.Click += OnRunGameClick;
        _projectMenu.DropDownItems.Add(runGameItem);

        _projectMenu.DropDownItems.Add(new ToolStripSeparator());

        ToolStripMenuItem settingsItem = new() { Text = "Project Settings..." };
        settingsItem.Click += OnProjectSettingsClick;
        _projectMenu.DropDownItems.Add(settingsItem);

        _projectMenu.DropDownItems.Add(new ToolStripSeparator());

        ToolStripMenuItem generateSceneItem = new()
        {
            Text             = "Generate Scene Code",
            ShortcutKeys     = Keys.Control | Keys.G,
            ShowShortcutKeys = true,
        };
        generateSceneItem.Click += OnGenerateSceneCodeClick;
        _projectMenu.DropDownItems.Add(generateSceneItem);

        ToolStripMenuItem generateAllItem = new() { Text = "Generate All Scenes" };
        generateAllItem.Click += OnGenerateAllScenesClick;
        _projectMenu.DropDownItems.Add(generateAllItem);

        _projectMenu.DropDownItems.Add(new ToolStripSeparator());

        ToolStripMenuItem rescanItem = new() { Text = "Rescan Behaviours" };
        rescanItem.Click += OnRescanBehavioursClick;
        _projectMenu.DropDownItems.Add(rescanItem);

        ToolStripMenuItem newBehaviourItem = new() { Text = "New Behaviour..." };
        newBehaviourItem.Click += OnNewBehaviourClick;
        _projectMenu.DropDownItems.Add(newBehaviourItem);

        _projectMenu.DropDownItems.Add(new ToolStripSeparator());

        ToolStripMenuItem worldConfigItem = new() { Text = "Configure World Subsystems..." };
        worldConfigItem.Click += OnConfigureWorldSubsystemsClick;
        _projectMenu.DropDownItems.Add(worldConfigItem);
    }

    private async void OnBuildContentClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null)
        {
            _consolePanel.AppendLine("[Build] No project is open.");
            return;
        }

        string mgcbFile = Path.Combine(project.ContentPath, "Content.mgcb");
        if (!File.Exists(mgcbFile))
        {
            _consolePanel.AppendLine($"[Build] Content.mgcb not found at: {mgcbFile}");
            return;
        }

        _consolePanel.AppendLine("[Build] Starting content build…");
        _statusLabel.Text = "Building content…";

        try
        {
            int exitCode = await MgcbRunner.RunAsync(mgcbFile,
                line => BeginInvoke(() => _consolePanel.AppendLine(line))).ConfigureAwait(true);

            string result = exitCode == 0 ? "Build succeeded." : $"Build failed (exit {exitCode}).";
            _consolePanel.AppendLine($"[Build] {result}");
            _statusLabel.Text = result;
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Build] Error: {ex.Message}");
            _statusLabel.Text = "Build error.";
        }
    }

    private async void OnBuildGameClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null)
        {
            _consolePanel.AppendLine("[Build] No project is open.");
            return;
        }

        if (string.IsNullOrEmpty(project.GameCsprojPath))
        {
            _consolePanel.AppendLine("[Build] No game .csproj configured. Use Project > Project Settings to set it.");
            return;
        }

        if (!File.Exists(project.GameCsprojPath))
        {
            _consolePanel.AppendLine($"[Build] Game .csproj not found: {project.GameCsprojPath}");
            return;
        }

        string config = _projectSettings?.BuildConfiguration ?? "Debug";
        _consolePanel.AppendLine($"[Build] Building {project.Name} ({config})…");
        _statusLabel.Text = "Building game…";

        try
        {
            int exitCode = await MgcbRunner.RunDotnetBuildAsync(
                project.GameCsprojPath,
                config,
                line => BeginInvoke(() => _consolePanel.AppendBuildLine(line))).ConfigureAwait(true);

            string result = exitCode == 0 ? "Build succeeded." : $"Build failed (exit {exitCode}).";
            _consolePanel.AppendLine($"[Build] {result}");
            _statusLabel.Text = result;
            _context.EventBus.Publish(new BuildOutputLineEvent(result, exitCode != 0));

            if (exitCode == 0)
                await RescanBehavioursFromBuildAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Build] Error: {ex.Message}");
            _statusLabel.Text = "Build error.";
        }
    }

    private void OnRunGameClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null)
        {
            _consolePanel.AppendLine("[Run] No project is open.");
            return;
        }

        if (string.IsNullOrEmpty(project.GameCsprojPath))
        {
            _consolePanel.AppendLine("[Run] No game .csproj configured. Use Project > Project Settings to set it.");
            return;
        }

        try
        {
            string config = _projectSettings?.BuildConfiguration ?? "Debug";
            ProcessStartInfo psi = new("dotnet",
                $"run --project \"{project.GameCsprojPath}\" --configuration {config}")
            {
                UseShellExecute = false,
                CreateNoWindow  = false,
            };
            Process.Start(psi);
            _consolePanel.AppendLine($"[Run] Launching {project.Name} ({config})…");
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Run] Error: {ex.Message}");
        }
    }

    private async void OnProjectSettingsClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null)
        {
            MessageBox.Show(this, "No project is open.", "Project Settings",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _projectSettings ??= new ProjectSettings();

        using ProjectSettingsDialog dlg = new(project, _projectSettings);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await _projectSettings.SaveAsync(project).ConfigureAwait(true);
            _consolePanel.AppendLine("[Settings] Project settings saved.");
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Settings] Failed to save settings: {ex.Message}");
            MessageBox.Show(this, $"Failed to save settings:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Menu — Project (CodeGen)

    private async void OnGenerateSceneCodeClick(object? sender, EventArgs e)
    {
        EditorScene?   scene   = _context.ActiveScene;
        EditorProject? project = _context.ActiveProject;

        if (scene is null || project is null)
        {
            _consolePanel.AppendLine("[CodeGen] No scene or project is open.");
            return;
        }

        if (string.IsNullOrEmpty(project.GameSourcePath))
        {
            _consolePanel.AppendLine("[CodeGen] GameSourcePath is not configured. Open Project Settings.");
            return;
        }

        ProjectSettings settings = _projectSettings ?? new ProjectSettings();
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            _consolePanel.AppendLine("[CodeGen] RootNamespace is not set. Configure it in Project Settings → Code Generation.");
            return;
        }

        _context.EventBus.Publish(new CodeGenStartedEvent(scene.Name));
        _statusLabel.Text = "Generating code...";

        try
        {
            CodeGenResult result = await _codeGenService
                .GenerateSceneAsync(scene, project, settings)
                .ConfigureAwait(true);

            if (result.Success)
                _consolePanel.AppendLine($"[CodeGen] Generated: {result.OutputPath}", LogLevel.Info);
            else
                _consolePanel.AppendLine($"[CodeGen] Error: {result.ErrorMessage}", LogLevel.Error);

            _context.EventBus.Publish(new CodeGenCompletedEvent(result));
            _statusLabel.Text = result.Success ? "Code generated." : "Code gen failed.";
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[CodeGen] Exception: {ex.Message}", LogLevel.Error);
            _statusLabel.Text = "Code gen failed.";
        }
    }

    private async void OnGenerateAllScenesClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        if (project is null)
        {
            _consolePanel.AppendLine("[CodeGen] No project is open.");
            return;
        }

        if (string.IsNullOrEmpty(project.GameSourcePath) || string.IsNullOrEmpty(project.ScenesPath))
        {
            _consolePanel.AppendLine("[CodeGen] GameSourcePath or ScenesPath is not configured.");
            return;
        }

        ProjectSettings settings = _projectSettings ?? new ProjectSettings();
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            _consolePanel.AppendLine("[CodeGen] RootNamespace is not set. Configure it in Project Settings.");
            return;
        }

        if (!Directory.Exists(project.ScenesPath))
        {
            _consolePanel.AppendLine($"[CodeGen] Scenes folder not found: {project.ScenesPath}");
            return;
        }

        string[] sceneFiles = Directory.GetFiles(project.ScenesPath, "*.scene.json");
        _consolePanel.AppendLine($"[CodeGen] Generating code for {sceneFiles.Length} scene(s)...");
        _statusLabel.Text = "Generating all scenes...";

        CodeGenProgressDialog progressDlg = new();
        progressDlg.Show(this);

        int success = 0;
        int failed  = 0;

        for (int i = 0; i < sceneFiles.Length; i++)
        {
            try
            {
                EditorScene? scene = await SceneSerializer.LoadAsync(sceneFiles[i])
                    .ConfigureAwait(true);

                if (scene is null) { failed++; continue; }

                _context.EventBus.Publish(new CodeGenStartedEvent(scene.Name));
                CodeGenResult result = await _codeGenService
                    .GenerateSceneAsync(scene, project, settings)
                    .ConfigureAwait(true);

                progressDlg.AddFileResult(result.OutputPath.Length > 0 ? result.OutputPath : sceneFiles[i], result.Success);

                if (result.Success)
                {
                    _consolePanel.AppendLine($"[CodeGen] ✓ {scene.Name}", LogLevel.Info);
                    success++;
                }
                else
                {
                    _consolePanel.AppendLine($"[CodeGen] ✗ {scene.Name}: {result.ErrorMessage}", LogLevel.Error);
                    failed++;
                }

                _context.EventBus.Publish(new CodeGenCompletedEvent(result));
            }
            catch (Exception ex)
            {
                _consolePanel.AppendLine($"[CodeGen] Error processing {Path.GetFileName(sceneFiles[i])}: {ex.Message}", LogLevel.Error);
                progressDlg.AddFileResult(sceneFiles[i], false);
                failed++;
            }
        }

        progressDlg.MarkComplete(success, failed);
        _statusLabel.Text = $"CodeGen: {success} OK, {failed} failed.";
    }

    private async void OnRescanBehavioursClick(object? sender, EventArgs e)
    {
        _registry.Scan();

        EditorProject? project = _context.ActiveProject;
        if (project is not null)
            await RescanBehavioursFromBuildAsync(project).ConfigureAwait(true);

        _consolePanel.AppendLine($"[CodeGen] Behaviours rescanned. Found {_registry.RegisteredTypes.Count} type(s), {_registry.PendingTypeNames.Count} pending.");
    }

    private async Task RescanBehavioursFromBuildAsync(EditorProject project)
    {
        if (string.IsNullOrEmpty(project.GameCsprojPath)) return;

        string config = _projectSettings?.BuildConfiguration ?? "Debug";
        string gameDir = project.GameSourcePath;
        if (string.IsNullOrEmpty(gameDir)) return;

        string projectName = Path.GetFileNameWithoutExtension(project.GameCsprojPath);

        // Try common output paths for a SDK-style project
        string[] candidates =
        [
            Path.Combine(gameDir, "bin", config, "net10.0", $"{projectName}.dll"),
            Path.Combine(gameDir, "bin", config, $"{projectName}.dll"),
        ];

        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
            {
                await _registry.ScanFromAssemblyAsync(candidates[i]).ConfigureAwait(true);
                _consolePanel.AppendLine($"[CodeGen] Assembly scanned: {Path.GetFileName(candidates[i])} — {_registry.RegisteredTypes.Count} type(s).", LogLevel.Info);
                return;
            }
        }

        // Fallback: scan source files
        if (Directory.Exists(project.GameSourcePath))
            await _registry.ScanSourceAsync(project.GameSourcePath).ConfigureAwait(true);
    }

    private async void OnNewBehaviourClick(object? sender, EventArgs e)
    {
        EditorProject? project = _context.ActiveProject;
        string gameSourcePath  = project?.GameSourcePath ?? string.Empty;
        string projectRootPath = project?.RootPath       ?? string.Empty;
        string defaultNs       = string.IsNullOrEmpty(project?.GameCsprojPath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(project.GameCsprojPath);

        using NewBehaviourDialog dlg = new(gameSourcePath, projectRootPath, defaultNs);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        if (string.IsNullOrEmpty(dlg.ClassName)) return;

        _statusLabel.Text = $"Creating {dlg.ClassName}...";

        try
        {
            CodeGenResult result = await _codeGenService.GenerateBehaviourSkeletonAsync(
                dlg.ClassName,
                dlg.NamespaceName,
                dlg.RelativeFolder,
                dlg.SelectedMethods,
                project ?? new EditorProject("(none)", string.Empty)).ConfigureAwait(true);

            if (result.Success)
            {
                _consolePanel.AppendLine($"[CodeGen] Created: {result.OutputPath}", LogLevel.Info);
                _registry.Scan();
            }
            else
            {
                _consolePanel.AppendLine($"[CodeGen] Failed: {result.ErrorMessage}", LogLevel.Error);
            }

            _statusLabel.Text = result.Success ? "Behaviour created." : "Failed.";
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[CodeGen] Error: {ex.Message}", LogLevel.Error);
            _statusLabel.Text = "Error.";
        }
    }

    private void OnConfigureWorldSubsystemsClick(object? sender, EventArgs e)
    {
        EditorScene? scene = _context.ActiveScene;
        if (scene is null)
        {
            MessageBox.Show(this, "No scene is currently open.", "Configure World Subsystems", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using WorldConfigDialog dlg = new();
        dlg.LoadFrom(scene.WorldConfig);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        scene.WorldConfig = dlg.BuildConfig();
        _context.MarkSceneDirty();
        _consolePanel.AppendLine("[Scene] World subsystems configuration updated.", LogLevel.Info);
    }

    #endregion

    #region Menu — File

    private void BuildFileMenuExtras()
    {
        _newSceneMenuItem = new ToolStripMenuItem("New Scene...")
        {
            ShortcutKeys     = Keys.Control | Keys.Shift | Keys.N,
            ShowShortcutKeys = true,
        };
        _newSceneMenuItem.Click += OnFileNewSceneClick;

        _openRecentMenuItem = new ToolStripMenuItem("Open Recent");

        _saveSceneMenuItem = new ToolStripMenuItem("Save Scene")
        {
            ShortcutKeys     = Keys.Control | Keys.S,
            ShowShortcutKeys = true,
        };
        _saveSceneMenuItem.Click += OnFileSaveSceneClick;

        _saveSceneAsMenuItem = new ToolStripMenuItem("Save Scene As...")
        {
            ShortcutKeys     = Keys.Control | Keys.Shift | Keys.S,
            ShowShortcutKeys = true,
        };
        _saveSceneAsMenuItem.Click += OnFileSaveSceneAsClick;

        // Target layout: [New Project | New Scene | --- | Open Project | Open Recent | --- | Save Scene | Save Scene As | _fileSeparator | Exit]
        int newProjIdx = _fileMenu.DropDownItems.IndexOf(_newProjectItem);
        _fileMenu.DropDownItems.Insert(newProjIdx + 1, _newSceneMenuItem);
        _fileMenu.DropDownItems.Insert(newProjIdx + 2, new ToolStripSeparator());

        int sepIdx = _fileMenu.DropDownItems.IndexOf(_fileSeparator);
        _fileMenu.DropDownItems.Insert(sepIdx, _openRecentMenuItem);

        sepIdx = _fileMenu.DropDownItems.IndexOf(_fileSeparator);
        _fileMenu.DropDownItems.Insert(sepIdx, new ToolStripSeparator());

        sepIdx = _fileMenu.DropDownItems.IndexOf(_fileSeparator);
        _fileMenu.DropDownItems.Insert(sepIdx, _saveSceneMenuItem);

        sepIdx = _fileMenu.DropDownItems.IndexOf(_fileSeparator);
        _fileMenu.DropDownItems.Insert(sepIdx, _saveSceneAsMenuItem);

        RebuildOpenRecentsMenu();
    }

    private void RebuildOpenRecentsMenu()
    {
        _openRecentMenuItem.DropDownItems.Clear();
        _openRecentMenuItem.Enabled = _preferences.RecentProjects.Count > 0;

        for (int i = 0; i < _preferences.RecentProjects.Count; i++)
        {
            string path = _preferences.RecentProjects[i];
            string label = $"{i + 1}. {path}";
            ToolStripMenuItem item = new(label) { Tag = path };
            item.Click += OnOpenRecentProjectClick;
            _openRecentMenuItem.DropDownItems.Add(item);
        }
    }

    private async void OnOpenRecentProjectClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not string path) return;

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(path)).ConfigureAwait(true);
            if (project is null)
            {
                MessageBox.Show(this,
                    $"Could not load project from:\n{path}\n\nThe project file may have been moved or deleted.",
                    "Project Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _preferences.RecentProjects.Remove(path);
                _preferences.Save();
                RebuildOpenRecentsMenu();
                return;
            }

            _context.SetActiveProject(project);
            _preferences.AddRecentProject(project.RootPath);
            RebuildOpenRecentsMenu();
            _consolePanel.AppendLine($"[Editor] Project '{project.Name}' opened from {project.RootPath}");
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Editor] Failed to open recent project: {ex.Message}");
        }
    }

    private async void OnFileSaveSceneClick(object? sender, EventArgs e)
    {
        EditorScene? scene = _context.ActiveScene;
        EditorProject? project = _context.ActiveProject;

        if (scene is null && project is null)
        {
            _consolePanel.AppendLine("[Save] Nothing to save — no project or scene is open.");
            return;
        }

        try
        {
            if (scene is not null)
            {
                string scenePath = scene.ScenePath;

                if (string.IsNullOrEmpty(scenePath))
                {
                    string initialDir = project is not null && Directory.Exists(project.ScenesPath)
                        ? project.ScenesPath
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    using SaveFileDialog dlg = new()
                    {
                        Title = "Save Scene",
                        Filter = "Scene files (*.scene.json)|*.scene.json|All files (*.*)|*.*",
                        InitialDirectory = initialDir,
                        FileName = string.IsNullOrEmpty(scene.Name) ? "NewScene.scene.json" : $"{scene.Name}.scene.json",
                    };

                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    scenePath = dlg.FileName;
                    scene.ScenePath = scenePath;
                    scene.Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(scenePath));
                }

                await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
                _context.MarkSceneClean();
                _consolePanel.AppendLine($"[Save] Scene saved to {scenePath}");
                _statusLabel.Text = "Saved.";

                // Auto-generate code on save when configured
                await TryGenerateCodeOnSaveAsync(scene, project).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Save] Error: {ex.Message}");
            _statusLabel.Text = "Save failed.";
        }
    }

    private async Task TryGenerateCodeOnSaveAsync(EditorScene scene, EditorProject? project)
    {
        if (project is null) return;
        ProjectSettings settings = _projectSettings ?? new ProjectSettings();
        if (!settings.GenerateOnSave) return;
        if (string.IsNullOrEmpty(project.GameCsprojPath)) return;
        if (string.IsNullOrWhiteSpace(settings.RootNamespace)) return;

        _context.EventBus.Publish(new CodeGenStartedEvent(scene.Name));
        _statusLabel.Text = "Generating code...";

        try
        {
            CodeGenResult result = await _codeGenService
                .GenerateSceneAsync(scene, project, settings)
                .ConfigureAwait(true);

            if (result.Success)
                _consolePanel.AppendLine($"[CodeGen] Generated: {result.OutputPath}", LogLevel.Info);
            else
                _consolePanel.AppendLine($"[CodeGen] Error: {result.ErrorMessage}", LogLevel.Error);

            _context.EventBus.Publish(new CodeGenCompletedEvent(result));
            _statusLabel.Text = result.Success ? "Saved + code generated." : "Saved (code gen failed).";
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[CodeGen] Exception on save: {ex.Message}", LogLevel.Error);
            _statusLabel.Text = "Saved (code gen error).";
        }
    }

    private async void OnFileSaveSceneAsClick(object? sender, EventArgs e)    {
        EditorScene? scene = _context.ActiveScene;
        EditorProject? project = _context.ActiveProject;

        if (scene is null)
        {
            _consolePanel.AppendLine("[Save As] No scene is open.");
            return;
        }

        string initialDir = project is not null && Directory.Exists(project.ScenesPath)
            ? project.ScenesPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        using SaveFileDialog dlg = new()
        {
            Title = "Save Scene As",
            Filter = "Scene files (*.scene.json)|*.scene.json|All files (*.*)|*.*",
            InitialDirectory = initialDir,
            FileName = string.IsNullOrEmpty(scene.Name) ? "NewScene.scene.json" : $"{scene.Name}.scene.json",
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            scene.ScenePath = dlg.FileName;
            scene.Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(dlg.FileName));
            await SceneSerializer.SaveAsync(scene, dlg.FileName).ConfigureAwait(true);
            _context.MarkSceneClean();
            _consolePanel.AppendLine($"[Save As] Scene saved to {dlg.FileName}");
            _statusLabel.Text = "Saved.";
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Save As] Error: {ex.Message}");
            _statusLabel.Text = "Save failed.";
        }
    }

    private async void OnFileNewSceneClick(object? sender, EventArgs e)
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
            catch (Exception ex)
            {
                _consolePanel.AppendLine($"[Editor] Failed to save scene: {ex.Message}", LogLevel.Error);
            }
        }

        _context.SetActiveScene(scene);
        _context.EventBus.Publish(new SceneCreatedEvent(scene));
        _consolePanel.AppendLine($"[Editor] New scene '{scene.Name}' created.");
    }

    private async void OnFileNewProjectClick(object? sender, EventArgs e)
    {
        using NewProjectDialog dlg = new();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string rootPath = Path.Combine(dlg.ParentPath, dlg.ProjectName);
            string contentRel = string.IsNullOrWhiteSpace(dlg.ContentPath)
                ? "Content"
                : Path.GetRelativePath(
                    string.IsNullOrWhiteSpace(dlg.GameCsprojPath)
                        ? rootPath
                        : Path.GetDirectoryName(dlg.GameCsprojPath)!,
                    dlg.ContentPath);
            string locRel = string.IsNullOrWhiteSpace(dlg.LocalizationPath)
                ? "Localization"
                : Path.GetRelativePath(
                    string.IsNullOrWhiteSpace(dlg.GameCsprojPath)
                        ? rootPath
                        : Path.GetDirectoryName(dlg.GameCsprojPath)!,
                    dlg.LocalizationPath);

            EditorProject project = await Task.Run(() =>
                ProjectManager.Create(dlg.ProjectName, dlg.ParentPath, dlg.GameCsprojPath, contentRel, locRel));
            _context.SetActiveProject(project);
            _preferences.LastProjectPath = project.RootPath;
            _preferences.AddRecentProject(project.RootPath);
            _preferences.Save();
            _consolePanel.AppendLine($"[Editor] Project '{project.Name}' created at {project.RootPath}");
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Editor] Failed to create project: {ex.Message}");
            MessageBox.Show(this, $"Failed to create project:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnFileOpenProjectClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            Description = "Select a MonoGame Editor project folder",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_preferences.LastProjectPath)
                ? _preferences.LastProjectPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(dlg.SelectedPath));

            if (project is null)
            {
                string? slnName = await Task.Run(() => ProjectManager.FindSolutionName(dlg.SelectedPath));

                if (slnName is null)
                {
                    MessageBox.Show(this,
                        $"The selected folder does not contain a valid MonoGame Editor project.\n\nExpected a '{ProjectManager.ProjectFileName}' file.",
                        "Invalid Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult answer = MessageBox.Show(this,
                    $"The folder contains an existing MonoGame solution '{slnName}' but has not been initialized as an editor project yet.\n\nInitialize it now? This will create '{ProjectManager.ProjectFileName}' and any missing standard folders.",
                    "Initialize Project", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes) return;

                project = await Task.Run(() => ProjectManager.Initialize(dlg.SelectedPath));
            }

            _context.SetActiveProject(project);
            _preferences.LastProjectPath = project.RootPath;
            _preferences.AddRecentProject(project.RootPath);
            _preferences.Save();
            _consolePanel.AppendLine($"[Editor] Project '{project.Name}' opened from {project.RootPath}");
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Editor] Failed to open project: {ex.Message}");
            MessageBox.Show(this, $"Failed to open project:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFileExitClick(object? sender, EventArgs e) => Close();

    private void OnBehaviourAdded(BehaviourAddedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnBehaviourAdded(evt)); return; }
        _consolePanel.AppendLine($"[Editor] Added '{evt.Behaviour.TypeName}' to '{evt.GameObject.Name}'.");
    }

    private void OnInputMapLoaded(InputMapLoadedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnInputMapLoaded(evt)); return; }
        _consolePanel.AppendLine($"[Input] Input map loaded — {evt.Model.Actions.Count} action(s).");
    }

    private void OnLocalizationLoaded(LocalizationLoadedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnLocalizationLoaded(evt)); return; }
        _consolePanel.AppendLine($"[Localization] Loaded — {evt.Model.Keys.Count} key(s).");
    }

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnProjectOpened(evt)); return; }

        UpdateFormTitle();

        string contentPath = evt.Project?.ContentPath ?? string.Empty;
        _contentWatcher.Watch(contentPath);

        RebuildOpenRecentsMenu();

        if (evt.Project is not null)
        {
            RefreshBehaviourRegistryAsync(evt.Project);
            _ = LoadProjectSettingsAsync(evt.Project);
        }
    }

    private void RefreshBehaviourRegistryAsync(EditorProject project)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _registry.Scan();

                if (!string.IsNullOrWhiteSpace(project.GameCsprojPath))
                {
                    string dllPath = Path.Combine(
                        Path.GetDirectoryName(project.GameCsprojPath) ?? project.RootPath,
                        "bin",
                        "Debug",
                        "net10.0",
                        $"{Path.GetFileNameWithoutExtension(project.GameCsprojPath)}.dll");

                    if (File.Exists(dllPath))
                        await _registry.ScanFromAssemblyAsync(dllPath).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(project.GameSourcePath) && Directory.Exists(project.GameSourcePath))
                    await _registry.ScanSourceAsync(project.GameSourcePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                BeginInvoke(() => _consolePanel.AppendLine($"[Registry] Refresh failed: {ex.Message}"));
            }
        });
    }

    private async Task LoadProjectSettingsAsync(EditorProject project)
    {
        try
        {
            _projectSettings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _consolePanel.AppendLine($"[Settings] Failed to load project settings: {ex.Message}");
        }
    }

    private void OnSceneLoaded(SceneLoadedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnSceneLoaded(evt)); return; }
        UpdateFormTitle();
        UpdateViewportClearColor(evt.Scene);
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnSceneDirtyChanged(evt)); return; }
        UpdateFormTitle();
    }

    private void UpdateFormTitle()
    {
        EditorProject? project = _context.ActiveProject;
        EditorScene? scene     = _context.ActiveScene;
        bool dirty             = _context.IsSceneDirty;

        if (project is null)
        {
            Text = "MonoGame Editor";
            return;
        }

        if (scene is null)
        {
            Text = $"MonoGame Editor — {project.Name}";
            return;
        }

        string dirtyMarker = dirty ? " *" : string.Empty;
        Text = $"MonoGame Editor — {project.Name} — {scene.Name}{dirtyMarker}";
    }

    private void UpdateViewportClearColor(EditorScene? scene)
    {
        if (scene is { WorldConfig.UseLighting: true })
        {
            int[] rgba = scene.WorldConfig.AmbientColorRgba;
            Microsoft.Xna.Framework.Color ambient = new(rgba[0], rgba[1], rgba[2]);
            _viewport.ClearColor = Microsoft.Xna.Framework.Color.Lerp(DefaultClearColor, ambient, 0.4f);
        }
        else
        {
            _viewport.ClearColor = DefaultClearColor;
        }
    }

    #endregion

    #region Viewport rendering

    private void OnCenterTabChanged(object? sender, EventArgs e)
    {
        bool sceneVisible = _centerTabControl.SelectedTab == _sceneTab;
        _viewport.IsActive     = sceneVisible;
        _gameViewport.IsActive = !sceneVisible;
    }

    private void OnViewportRenderFrame(object? sender, RenderEventArgs e)
    {
        ApplyKeyboardNavigation(e.Elapsed);

        // Accumulate FPS counter; update label every 0.5s
        _fpsAccumTime  += e.Elapsed.TotalSeconds;
        _fpsFrameCount++;
        if (_fpsAccumTime >= 0.5)
        {
            _fpsCurrent    = (float)(_fpsFrameCount / _fpsAccumTime);
            _fpsAccumTime  = 0;
            _fpsFrameCount = 0;
            BeginInvoke(() => _fpsStatusLabel.Text = $"{_fpsCurrent:F0} fps");
        }

        // Scene tab always renders the edit-mode overlay (grid, gizmos, sprite previews)
        // regardless of play state — mirrors the Unity Scene view behaviour.
        DrawEditorGizmos(e);
    }

    private void ApplyKeyboardNavigation(TimeSpan elapsed)
    {
        if (_pressedNavigationKeys.Count == 0)
            return;

        float dt = (float)elapsed.TotalSeconds;
        if (dt <= 0f)
            return;

        float speed = _pressedNavigationKeys.Contains(Keys.ShiftKey) ? 1200f : 450f;
        Vector2 delta = Vector2.Zero;

        if (_pressedNavigationKeys.Contains(Keys.W)) delta.Y -= 1f;
        if (_pressedNavigationKeys.Contains(Keys.S)) delta.Y += 1f;
        if (_pressedNavigationKeys.Contains(Keys.A)) delta.X -= 1f;
        if (_pressedNavigationKeys.Contains(Keys.D)) delta.X += 1f;

        if (delta == Vector2.Zero)
            return;

        delta.Normalize();
        _viewport.Camera.Pan(delta * speed * dt / _viewport.Camera.Zoom);
    }

    private void OnGameViewportRenderFrame(object? sender, RenderEventArgs e)
    {
        EditorState state = _context.State;
        if (state != EditorState.Playing && state != EditorState.Paused) return;

        if (_playRunner is null) return;
        _playRunner.EnsureInitialized(e.GraphicsDevice);

        if (state == EditorState.Playing)
            _playRunner.Update(e.Elapsed);

        _playRunner.Draw(e.Elapsed);
    }

    private void DrawEditorGizmos(RenderEventArgs e)
    {
        if (_gizmoRenderer is null) return;

        if (!_gizmoRenderer.IsInitialized)
            _gizmoRenderer.Initialize(e.GraphicsDevice);

        // e.Width/Height come directly from _swapChain.Width/Height in the render thread —
        // no WinForms cross-thread access, no reliance on GraphicsDevice.Viewport being set.
        int w = e.Width;
        int h = e.Height;
        if (w <= 0 || h <= 0) return;

        Viewport vp = new(0, 0, w, h);
        Matrix cameraTransform = _viewport.Camera.GetTransformMatrix(vp);

        // Both 2D and 2.5D use the orthographic edit-mode renderer.
        _editRenderer ??= new EditModeRenderer(_context);
        if (!_editRenderer.IsInitialized)
            _editRenderer.Initialize(e.GraphicsDevice);
        if (_context.ActiveScene is not null)
            _editRenderer.DrawScene(_context.ActiveScene, cameraTransform);

        // NavGrid overlay (only when NAV button is checked)
        if (_navButton.Checked)
        {
            _navGridRenderer ??= new NavGridPreviewRenderer();
            if (!_navGridRenderer.IsInitialized)
                _navGridRenderer.Initialize(e.GraphicsDevice);
            _navGridRenderer.Draw(_context.ActiveScene?.WorldConfig, cameraTransform);
        }

        _gizmoRenderer.Draw(_context.SelectedObject, cameraTransform, w, h, _gizmoCtrl.IsDepthMode,
            _context.ActiveScene?.RootGameObjects);

        // Resolution preview overlay (only when RES button is checked)
        if (_resButton.Checked && _projectSettings is not null)
        {
            _resRenderer ??= new ResolutionPreviewRenderer();
            if (!_resRenderer.IsInitialized)
                _resRenderer.Initialize(e.GraphicsDevice);
            _resRenderer.Draw(_projectSettings.VirtualWidth, _projectSettings.VirtualHeight, w, h);
        }
    }

    #endregion

    #region Viewport mouse — gizmo interaction

    private void OnViewportMouseDown(object? sender, MouseEventArgs e)
    {
        if (_handToolEnabled)
            return;

        if (e.Button != MouseButtons.Left) return;

        EditorGameObject? selected = _context.SelectedObject;
        if (selected == null) return;

        int w = _viewport.ClientSize.Width;
        int h = _viewport.ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        Viewport vp = new(0, 0, w, h);
        Matrix camMatrix = _viewport.Camera.GetTransformMatrix(vp);
        Matrix inverse   = Matrix.Invert(camMatrix);

        Vector2 screenPos = new(e.X, e.Y);
        Vector2 worldPos  = Vector2.Transform(screenPos, inverse);
        Vector2 objScreen = Vector2.Transform(
            new Vector2(selected.Position.X, selected.Position.Y), camMatrix);

        bool gizmoHit = _gizmoCtrl.BeginDrag(
            e.X, e.Y,
            objScreen.X, objScreen.Y,
            worldPos.X,  worldPos.Y,
            selected);

        if (!gizmoHit)
        {
            _tilemapPainting = TryPaintTileAtWorldPos(worldPos, selected);
        }
    }

    private void OnViewportMouseMove(object? sender, MouseEventArgs e)
    {
        if (_handToolEnabled)
            return;

        EditorGameObject? selected = _context.SelectedObject;
        if (selected == null) return;

        int w = _viewport.ClientSize.Width;
        int h = _viewport.ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        Viewport vp = new(0, 0, w, h);
        Matrix camMatrix = _viewport.Camera.GetTransformMatrix(vp);
        Matrix inverse   = Matrix.Invert(camMatrix);

        Vector2 worldPos  = Vector2.Transform(new Vector2(e.X, e.Y), inverse);
        Vector2 objScreen = Vector2.Transform(
            new Vector2(selected.Position.X, selected.Position.Y), camMatrix);

        _gizmoCtrl.UpdateDrag(
            worldPos.X, worldPos.Y,
            e.X, e.Y,
            objScreen.X, objScreen.Y,
            selected);

        _context.EventBus.Publish(new GameObjectTransformChangedEvent(selected));

        if (_tilemapPainting && e.Button == MouseButtons.Left)
            TryPaintTileAtWorldPos(worldPos, selected);
    }

    private void OnViewportMouseUp(object? sender, MouseEventArgs e)
    {
        if (_handToolEnabled)
            return;

        if (e.Button != MouseButtons.Left) return;

        _tilemapPainting = false;

        bool ctrlHeld         = ModifierKeys.HasFlag(Keys.Control);
        IEditorCommand? cmd   = _gizmoCtrl.EndDrag(_context.SelectedObject, ctrlHeld);

        if (cmd != null)
        {
            _context.Commands.Execute(cmd);
            _undoHistoryPanel.Refresh(_context.Commands);
        }
    }

    private bool TryPaintTileAtWorldPos(Vector2 worldPos, EditorGameObject selected)
    {
        int gid = _tilemapPalettePanel.SelectedTileGid;
        if (gid < 0) return false;

        EditorTilemapAsset? asset = _tilemapPalettePanel.CurrentTilemap;
        EditorTileLayer? layer = _tilemapPalettePanel.ActiveLayer;
        if (asset is null || layer is null) return false;
        if (asset.TileWidth <= 0 || asset.TileHeight <= 0) return false;

        float offsetX = worldPos.X - selected.Position.X;
        float offsetY = worldPos.Y - selected.Position.Y;

        int col = (int)Math.Floor(offsetX / asset.TileWidth);
        int row = (int)Math.Floor(offsetY / asset.TileHeight);

        if (col < 0 || col >= layer.Width || row < 0 || row >= layer.Height)
            return true;

        int? existing = layer.GetTile(col, row);
        if (existing == gid) return true;

        _context.Commands.Execute(new PaintTileCommand(layer, col, row, gid));
        _undoHistoryPanel.Refresh(_context.Commands);
        return true;
    }

    #endregion
}
