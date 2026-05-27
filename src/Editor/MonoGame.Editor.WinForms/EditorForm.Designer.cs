#nullable enable
namespace MonoGame.Editor.WinForms;

partial class EditorForm
{
    private System.ComponentModel.IContainer? components = null;

    // ── Menu ──────────────────────────────────────────────────────────────
    private MenuStrip _mainMenuStrip = null!;
    private ToolStripMenuItem _fileMenu = null!;
    private ToolStripMenuItem _newProjectItem = null!;
    private ToolStripMenuItem _openProjectItem = null!;
    private ToolStripSeparator _fileSeparator = null!;
    private ToolStripMenuItem _exitItem = null!;
    private ToolStripMenuItem _editMenu = null!;
    private ToolStripMenuItem _undoMenuItem = null!;
    private ToolStripMenuItem _redoMenuItem = null!;
    private ToolStripSeparator _editSeparator1 = null!;
    private ToolStripMenuItem _viewMenu = null!;
    private ToolStripMenuItem _viewHierarchyMenuItem = null!;
    private ToolStripMenuItem _viewInspectorMenuItem = null!;
    private ToolStripMenuItem _viewAssetBrowserMenuItem = null!;
    private ToolStripMenuItem _viewConsoleMenuItem = null!;
    private ToolStripSeparator _viewMenuSeparator1 = null!;
    private ToolStripMenuItem _resetLayoutMenuItem = null!;
    private ToolStripMenuItem _projectMenu = null!;
    private ToolStripMenuItem _debugMenu = null!;

    // ── Horizontal toolbar ────────────────────────────────────────────────
    // Col 0 (AutoSize): gizmo buttons  |  Col 1 (Fill): centered play buttons
    private TableLayoutPanel _toolbarTable = null!;
    private ToolStrip _gizmoStrip = null!;
    private ToolStripButton _selectModeButton = null!;
    private ToolStripButton _moveModeButton = null!;
    private ToolStripButton _rotateModeButton = null!;
    private ToolStripButton _scaleModeButton = null!;
    private ToolStripButton _rectModeButton = null!;
    private ToolStripButton _handModeButton = null!;
    private ToolStripSeparator _gizmoSeparator = null!;
    private ToolStripButton _sceneViewModeButton = null!;
    private Panel _playbackCell = null!;
    private ToolStrip _playbackStrip = null!;
    private ToolStripButton _playButton = null!;
    private ToolStripButton _pauseButton = null!;
    private ToolStripButton _stopButton = null!;

    // ── Status ────────────────────────────────────────────────────────────
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    // ── Split containers ──────────────────────────────────────────────────
    private SplitContainer _mainSplit = null!;
    private SplitContainer _outerSplit = null!;
    private SplitContainer _innerSplit = null!;

    // ── Panels ────────────────────────────────────────────────────────────
    private SceneHierarchyPanel _hierarchyPanel = null!;
    private TabControl _centerTabControl = null!;
    private TabPage _sceneTab = null!;
    private TabPage _gameTab = null!;
    private MonoGameControl _viewport = null!;
    private MonoGameControl _gameViewport = null!;
    private InspectorPanel _inspectorPanel = null!;

    // ── Bottom tabs ───────────────────────────────────────────────────────
    private TabControl _bottomTabControl = null!;
    private TabPage _assetsTab = null!;
    private TabPage _consoleTab = null!;
    private TabPage _sceneManagerTab = null!;
    private AssetBrowserPanel _assetBrowserPanel = null!;
    private ConsolePanel _consolePanel = null!;
    private SceneManagerPanel _sceneManagerPanel = null!;
    private TabPage _localizationTab = null!;
    private LocalizationBrowserPanel _localizationPanel = null!;
    private TabPage _inputMapEditorTab = null!;
    private InputMapEditorPanel _inputMapEditorPanel = null!;

    // ── View menu ─────────────────────────────────────────────────────────
    // (declared alongside _viewSceneManagerMenuItem below in field declarations)
    private ToolStripMenuItem _viewSceneManagerMenuItem = null!;
    private ToolStripMenuItem _viewLocalizationMenuItem = null!;
    private ToolStripMenuItem _viewInputMapEditorMenuItem = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _mainMenuStrip            = new MenuStrip();
        _fileMenu                 = new ToolStripMenuItem();
        _newProjectItem           = new ToolStripMenuItem();
        _openProjectItem          = new ToolStripMenuItem();
        _fileSeparator            = new ToolStripSeparator();
        _exitItem                 = new ToolStripMenuItem();
        _editMenu                 = new ToolStripMenuItem();
        _undoMenuItem             = new ToolStripMenuItem();
        _redoMenuItem             = new ToolStripMenuItem();
        _editSeparator1           = new ToolStripSeparator();
        _viewMenu                 = new ToolStripMenuItem();
        _viewHierarchyMenuItem    = new ToolStripMenuItem();
        _viewInspectorMenuItem    = new ToolStripMenuItem();
        _viewAssetBrowserMenuItem = new ToolStripMenuItem();
        _viewConsoleMenuItem      = new ToolStripMenuItem();
        _viewMenuSeparator1       = new ToolStripSeparator();
        _resetLayoutMenuItem      = new ToolStripMenuItem();
        _projectMenu              = new ToolStripMenuItem();
        _debugMenu                = new ToolStripMenuItem();
        _toolbarTable             = new TableLayoutPanel();
        _gizmoStrip               = new ToolStrip();
        _selectModeButton         = new ToolStripButton();
        _moveModeButton           = new ToolStripButton();
        _rotateModeButton         = new ToolStripButton();
        _scaleModeButton          = new ToolStripButton();
        _rectModeButton           = new ToolStripButton();
        _handModeButton           = new ToolStripButton();
        _gizmoSeparator           = new ToolStripSeparator();
        _sceneViewModeButton      = new ToolStripButton();
        _playbackCell             = new Panel();
        _playbackStrip            = new ToolStrip();
        _playButton               = new ToolStripButton();
        _pauseButton              = new ToolStripButton();
        _stopButton               = new ToolStripButton();
        _statusStrip              = new StatusStrip();
        _statusLabel              = new ToolStripStatusLabel();
        _mainSplit                = new SplitContainer();
        _outerSplit               = new SplitContainer();
        _hierarchyPanel           = new SceneHierarchyPanel();
        _innerSplit               = new SplitContainer();
        _centerTabControl         = new TabControl();
        _sceneTab                 = new TabPage();
        _gameTab                  = new TabPage();
        _viewport                 = new MonoGameControl();
        _gameViewport             = new MonoGameControl();
        _inspectorPanel           = new InspectorPanel();
        _bottomTabControl         = new TabControl();
        _assetsTab                = new TabPage();
        _assetBrowserPanel        = new AssetBrowserPanel();
        _consoleTab               = new TabPage();
        _consolePanel             = new ConsolePanel();
        _sceneManagerTab          = new TabPage();
        _sceneManagerPanel        = new SceneManagerPanel();
        _localizationTab          = new TabPage();
        _localizationPanel        = new LocalizationBrowserPanel();
        _inputMapEditorTab        = new TabPage();
        _inputMapEditorPanel      = new InputMapEditorPanel();
        _viewSceneManagerMenuItem = new ToolStripMenuItem();
        _viewLocalizationMenuItem = new ToolStripMenuItem();
        _viewInputMapEditorMenuItem = new ToolStripMenuItem();

        _toolbarTable.SuspendLayout();
        _playbackCell.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_mainSplit).BeginInit();
        _mainSplit.Panel1.SuspendLayout();
        _mainSplit.Panel2.SuspendLayout();
        _mainSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_outerSplit).BeginInit();
        _outerSplit.Panel1.SuspendLayout();
        _outerSplit.Panel2.SuspendLayout();
        _outerSplit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_innerSplit).BeginInit();
        _innerSplit.Panel1.SuspendLayout();
        _innerSplit.Panel2.SuspendLayout();
        _innerSplit.SuspendLayout();
        _sceneTab.SuspendLayout();
        _gameTab.SuspendLayout();
        _assetsTab.SuspendLayout();
        _consoleTab.SuspendLayout();
        _sceneManagerTab.SuspendLayout();
        _localizationTab.SuspendLayout();
        _inputMapEditorTab.SuspendLayout();
        SuspendLayout();

        // _mainMenuStrip
        _mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _fileMenu, _editMenu, _viewMenu, _projectMenu, _debugMenu });
        _mainMenuStrip.Location = new System.Drawing.Point(0, 0);
        _mainMenuStrip.Name = "_mainMenuStrip";
        _mainMenuStrip.Size = new System.Drawing.Size(1280, 24);
        _mainMenuStrip.TabIndex = 0;
        _mainMenuStrip.Text = "_mainMenuStrip";

        // _fileMenu
        _fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { _newProjectItem, _openProjectItem, _fileSeparator, _exitItem });
        _fileMenu.Name = "_fileMenu";
        _fileMenu.Size = new System.Drawing.Size(37, 20);
        _fileMenu.Text = "File";

        // _newProjectItem
        _newProjectItem.Name = "_newProjectItem";
        _newProjectItem.Size = new System.Drawing.Size(163, 22);
        _newProjectItem.Text = "New Project...";
        _newProjectItem.Click += OnFileNewProjectClick;

        // _openProjectItem
        _openProjectItem.Name = "_openProjectItem";
        _openProjectItem.Size = new System.Drawing.Size(163, 22);
        _openProjectItem.Text = "Open Project...";
        _openProjectItem.Click += OnFileOpenProjectClick;

        // _fileSeparator
        _fileSeparator.Name = "_fileSeparator";
        _fileSeparator.Size = new System.Drawing.Size(160, 6);

        // _exitItem
        _exitItem.Name = "_exitItem";
        _exitItem.Size = new System.Drawing.Size(163, 22);
        _exitItem.Text = "Exit";
        _exitItem.Click += OnFileExitClick;

        // _editMenu
        _editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { _undoMenuItem, _redoMenuItem, _editSeparator1 });
        _editMenu.Name = "_editMenu";
        _editMenu.Size = new System.Drawing.Size(39, 20);
        _editMenu.Text = "Edit";

        // _undoMenuItem
        _undoMenuItem.Enabled      = false;
        _undoMenuItem.Name         = "_undoMenuItem";
        _undoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z;
        _undoMenuItem.ShowShortcutKeys = true;
        _undoMenuItem.Size         = new System.Drawing.Size(180, 22);
        _undoMenuItem.Text         = "Undo";
        _undoMenuItem.Click        += OnUndoClick;

        // _redoMenuItem
        _redoMenuItem.Enabled      = false;
        _redoMenuItem.Name         = "_redoMenuItem";
        _redoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y;
        _redoMenuItem.ShowShortcutKeys = true;
        _redoMenuItem.Size         = new System.Drawing.Size(180, 22);
        _redoMenuItem.Text         = "Redo";
        _redoMenuItem.Click        += OnRedoClick;

        // _editSeparator1
        _editSeparator1.Name = "_editSeparator1";
        _editSeparator1.Size = new System.Drawing.Size(177, 6);

        // _viewMenu
        _viewMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { _viewHierarchyMenuItem, _viewInspectorMenuItem, _viewAssetBrowserMenuItem, _viewConsoleMenuItem, _viewSceneManagerMenuItem, _viewLocalizationMenuItem, _viewInputMapEditorMenuItem, _viewMenuSeparator1, _resetLayoutMenuItem });
        _viewMenu.Name = "_viewMenu";
        _viewMenu.Size = new System.Drawing.Size(44, 20);
        _viewMenu.Text = "View";

        // _viewHierarchyMenuItem
        _viewHierarchyMenuItem.CheckOnClick = true;
        _viewHierarchyMenuItem.Checked = true;
        _viewHierarchyMenuItem.CheckState = CheckState.Checked;
        _viewHierarchyMenuItem.Name = "_viewHierarchyMenuItem";
        _viewHierarchyMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewHierarchyMenuItem.Text = "Hierarchy";
        _viewHierarchyMenuItem.Click += OnViewMenuItemClick;

        // _viewInspectorMenuItem
        _viewInspectorMenuItem.CheckOnClick = true;
        _viewInspectorMenuItem.Checked = true;
        _viewInspectorMenuItem.CheckState = CheckState.Checked;
        _viewInspectorMenuItem.Name = "_viewInspectorMenuItem";
        _viewInspectorMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewInspectorMenuItem.Text = "Inspector";
        _viewInspectorMenuItem.Click += OnViewMenuItemClick;

        // _viewAssetBrowserMenuItem
        _viewAssetBrowserMenuItem.CheckOnClick = true;
        _viewAssetBrowserMenuItem.Checked = true;
        _viewAssetBrowserMenuItem.CheckState = CheckState.Checked;
        _viewAssetBrowserMenuItem.Name = "_viewAssetBrowserMenuItem";
        _viewAssetBrowserMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewAssetBrowserMenuItem.Text = "Asset Browser";
        _viewAssetBrowserMenuItem.Click += OnViewMenuItemClick;

        // _viewConsoleMenuItem
        _viewConsoleMenuItem.CheckOnClick = true;
        _viewConsoleMenuItem.Checked = true;
        _viewConsoleMenuItem.CheckState = CheckState.Checked;
        _viewConsoleMenuItem.Name = "_viewConsoleMenuItem";
        _viewConsoleMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewConsoleMenuItem.Text = "Console";
        _viewConsoleMenuItem.Click += OnViewMenuItemClick;

        // _viewSceneManagerMenuItem
        _viewSceneManagerMenuItem.CheckOnClick = true;
        _viewSceneManagerMenuItem.Checked = true;
        _viewSceneManagerMenuItem.CheckState = CheckState.Checked;
        _viewSceneManagerMenuItem.Name = "_viewSceneManagerMenuItem";
        _viewSceneManagerMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewSceneManagerMenuItem.Text = "Scene Manager";
        _viewSceneManagerMenuItem.Click += OnViewMenuItemClick;

        // _viewLocalizationMenuItem
        _viewLocalizationMenuItem.CheckOnClick = true;
        _viewLocalizationMenuItem.Checked = false;
        _viewLocalizationMenuItem.CheckState = CheckState.Unchecked;
        _viewLocalizationMenuItem.Name = "_viewLocalizationMenuItem";
        _viewLocalizationMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewLocalizationMenuItem.Text = "Localization";
        _viewLocalizationMenuItem.Click += OnViewMenuItemClick;

        // _viewInputMapEditorMenuItem
        _viewInputMapEditorMenuItem.CheckOnClick = true;
        _viewInputMapEditorMenuItem.Checked = false;
        _viewInputMapEditorMenuItem.CheckState = CheckState.Unchecked;
        _viewInputMapEditorMenuItem.Name = "_viewInputMapEditorMenuItem";
        _viewInputMapEditorMenuItem.Size = new System.Drawing.Size(163, 22);
        _viewInputMapEditorMenuItem.Text = "Input Map Editor";
        _viewInputMapEditorMenuItem.Click += OnViewMenuItemClick;

        // _viewMenuSeparator1
        _viewMenuSeparator1.Name = "_viewMenuSeparator1";
        _viewMenuSeparator1.Size = new System.Drawing.Size(160, 6);

        // _resetLayoutMenuItem
        _resetLayoutMenuItem.Name = "_resetLayoutMenuItem";
        _resetLayoutMenuItem.Size = new System.Drawing.Size(163, 22);
        _resetLayoutMenuItem.Text = "Reset Layout";
        _resetLayoutMenuItem.Click += OnResetLayoutClick;

        // _projectMenu
        _projectMenu.Name = "_projectMenu";
        _projectMenu.Size = new System.Drawing.Size(56, 20);
        _projectMenu.Text = "Project";

        // _debugMenu
        _debugMenu.Name = "_debugMenu";
        _debugMenu.Size = new System.Drawing.Size(54, 20);
        _debugMenu.Text = "Debug";

        // _gizmoStrip — horizontal, auto-sizes to button content
        _gizmoStrip.AutoSize = true;
        _gizmoStrip.GripStyle = ToolStripGripStyle.Hidden;
        _gizmoStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _selectModeButton, _moveModeButton, _rotateModeButton, _scaleModeButton, _rectModeButton, _handModeButton, _gizmoSeparator, _sceneViewModeButton });
        _gizmoStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
        _gizmoStrip.Location = new System.Drawing.Point(0, 0);
        _gizmoStrip.Name = "_gizmoStrip";
        _gizmoStrip.Size = new System.Drawing.Size(204, 25);
        _gizmoStrip.TabIndex = 0;

        // _selectModeButton
        _selectModeButton.CheckOnClick = true;
        _selectModeButton.Checked = true;
        _selectModeButton.CheckState = CheckState.Checked;
        _selectModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _selectModeButton.Name = "_selectModeButton";
        _selectModeButton.Size = new System.Drawing.Size(46, 22);
        _selectModeButton.Text = "Q Select";
        _selectModeButton.ToolTipText = "Select (Q)";
        _selectModeButton.Click += OnGizmoModeClick;

        // _moveModeButton
        _moveModeButton.CheckOnClick = true;
        _moveModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _moveModeButton.Name = "_moveModeButton";
        _moveModeButton.Size = new System.Drawing.Size(43, 22);
        _moveModeButton.Text = "W Move";
        _moveModeButton.ToolTipText = "Move (W)";
        _moveModeButton.Click += OnGizmoModeClick;

        // _rotateModeButton
        _rotateModeButton.CheckOnClick = true;
        _rotateModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _rotateModeButton.Name = "_rotateModeButton";
        _rotateModeButton.Size = new System.Drawing.Size(52, 22);
        _rotateModeButton.Text = "E Rotate";
        _rotateModeButton.ToolTipText = "Rotate (E)";
        _rotateModeButton.Click += OnGizmoModeClick;

        // _scaleModeButton
        _scaleModeButton.CheckOnClick = true;
        _scaleModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _scaleModeButton.Name = "_scaleModeButton";
        _scaleModeButton.Size = new System.Drawing.Size(43, 22);
        _scaleModeButton.Text = "R Scale";
        _scaleModeButton.ToolTipText = "Scale (R)";
        _scaleModeButton.Click += OnGizmoModeClick;

        // _rectModeButton
        _rectModeButton.CheckOnClick = true;
        _rectModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _rectModeButton.Name = "_rectModeButton";
        _rectModeButton.Size = new System.Drawing.Size(40, 22);
        _rectModeButton.Text = "T Rect";
        _rectModeButton.ToolTipText = "Rect (T)";
        _rectModeButton.Click += OnGizmoModeClick;

        // _handModeButton
        _handModeButton.CheckOnClick = true;
        _handModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _handModeButton.Name = "_handModeButton";
        _handModeButton.Size = new System.Drawing.Size(44, 22);
        _handModeButton.Text = "H Hand";
        _handModeButton.ToolTipText = "Hand tool (H)";
        _handModeButton.Click += OnHandModeClick;

        // _gizmoSeparator
        _gizmoSeparator.Name = "_gizmoSeparator";
        _gizmoSeparator.Size = new System.Drawing.Size(6, 25);

        // _sceneViewModeButton
        _sceneViewModeButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _sceneViewModeButton.Name = "_sceneViewModeButton";
        _sceneViewModeButton.Size = new System.Drawing.Size(58, 22);
        _sceneViewModeButton.Text = "View: 2D";
        _sceneViewModeButton.ToolTipText = "Toggle Scene View 2D/2.5D";
        _sceneViewModeButton.Click += OnSceneViewModeClick;

        // _playbackStrip — positioned at runtime in center of full toolbar width
        _playbackStrip.AutoSize = true;
        _playbackStrip.GripStyle = ToolStripGripStyle.Hidden;
        _playbackStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _playButton, _pauseButton, _stopButton });
        _playbackStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
        _playbackStrip.Location = new System.Drawing.Point(0, 0);
        _playbackStrip.Name = "_playbackStrip";
        _playbackStrip.Size = new System.Drawing.Size(116, 25);
        _playbackStrip.TabIndex = 0;

        // _playButton
        _playButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _playButton.Name = "_playButton";
        _playButton.Size = new System.Drawing.Size(36, 22);
        _playButton.Text = "▶ Play";
        _playButton.ToolTipText = "Play (F5)";
        _playButton.Click += OnPlayClick;

        // _pauseButton
        _pauseButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _pauseButton.Enabled = false;
        _pauseButton.Name = "_pauseButton";
        _pauseButton.Size = new System.Drawing.Size(46, 22);
        _pauseButton.Text = "⏸ Pause";
        _pauseButton.ToolTipText = "Pause";
        _pauseButton.Click += OnPauseClick;

        // _stopButton
        _stopButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _stopButton.Enabled = false;
        _stopButton.Name = "_stopButton";
        _stopButton.Size = new System.Drawing.Size(36, 22);
        _stopButton.Text = "■ Stop";
        _stopButton.ToolTipText = "Stop";
        _stopButton.Click += OnStopClick;

        // _playbackCell — fill cell in toolbar table, hosts centered playbackStrip
        _playbackCell.Controls.Add(_playbackStrip);
        _playbackCell.Dock = DockStyle.Fill;
        _playbackCell.Location = new System.Drawing.Point(207, 0);
        _playbackCell.Name = "_playbackCell";
        _playbackCell.Size = new System.Drawing.Size(1073, 25);
        _playbackCell.TabIndex = 1;

        // _toolbarTable — Col 0: AutoSize (gizmo) | Col 1: Fill (playback center)
        _toolbarTable.AutoSize = true;
        _toolbarTable.ColumnCount = 2;
        _toolbarTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _toolbarTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _toolbarTable.Controls.Add(_gizmoStrip, 0, 0);
        _toolbarTable.Controls.Add(_playbackCell, 1, 0);
        _toolbarTable.Dock = DockStyle.Top;
        _toolbarTable.Location = new System.Drawing.Point(0, 24);
        _toolbarTable.Name = "_toolbarTable";
        _toolbarTable.RowCount = 1;
        _toolbarTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        _toolbarTable.Size = new System.Drawing.Size(1280, 25);
        _toolbarTable.TabIndex = 1;

        // _statusStrip
        _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _statusLabel });
        _statusStrip.Location = new System.Drawing.Point(0, 778);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new System.Drawing.Size(1280, 22);
        _statusStrip.TabIndex = 3;

        // _statusLabel
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new System.Drawing.Size(43, 17);
        _statusLabel.Text = "Editing";

        // _hierarchyPanel
        _hierarchyPanel.Dock = DockStyle.Fill;
        _hierarchyPanel.Location = new System.Drawing.Point(0, 0);
        _hierarchyPanel.Name = "_hierarchyPanel";
        _hierarchyPanel.Size = new System.Drawing.Size(220, 527);
        _hierarchyPanel.TabIndex = 0;

        // _viewport
        _viewport.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        _viewport.Dock = DockStyle.Fill;
        _viewport.Location = new System.Drawing.Point(0, 0);
        _viewport.Name = "_viewport";
        _viewport.Size = new System.Drawing.Size(776, 499);
        _viewport.TabIndex = 0;

        // _gameViewport
        _gameViewport.BackColor = System.Drawing.Color.FromArgb(20, 20, 20);
        _gameViewport.Dock = DockStyle.Fill;
        _gameViewport.Location = new System.Drawing.Point(0, 0);
        _gameViewport.Name = "_gameViewport";
        _gameViewport.Size = new System.Drawing.Size(776, 499);
        _gameViewport.TabIndex = 0;

        // _sceneTab
        _sceneTab.Controls.Add(_viewport);
        _sceneTab.Location = new System.Drawing.Point(4, 24);
        _sceneTab.Name = "_sceneTab";
        _sceneTab.Padding = new System.Windows.Forms.Padding(0);
        _sceneTab.Size = new System.Drawing.Size(776, 499);
        _sceneTab.TabIndex = 0;
        _sceneTab.Text = "Scene";

        // _gameTab
        _gameTab.Controls.Add(_gameViewport);
        _gameTab.Location = new System.Drawing.Point(4, 24);
        _gameTab.Name = "_gameTab";
        _gameTab.Padding = new System.Windows.Forms.Padding(0);
        _gameTab.Size = new System.Drawing.Size(776, 499);
        _gameTab.TabIndex = 1;
        _gameTab.Text = "Game";

        // _centerTabControl
        _centerTabControl.Controls.Add(_sceneTab);
        _centerTabControl.Controls.Add(_gameTab);
        _centerTabControl.Dock = DockStyle.Fill;
        _centerTabControl.Location = new System.Drawing.Point(0, 0);
        _centerTabControl.Name = "_centerTabControl";
        _centerTabControl.SelectedIndex = 0;
        _centerTabControl.Size = new System.Drawing.Size(776, 527);
        _centerTabControl.TabIndex = 0;

        // _inspectorPanel
        _inspectorPanel.Dock = DockStyle.Fill;
        _inspectorPanel.Location = new System.Drawing.Point(0, 0);
        _inspectorPanel.Name = "_inspectorPanel";
        _inspectorPanel.Size = new System.Drawing.Size(276, 527);
        _inspectorPanel.TabIndex = 0;

        // _innerSplit
        _innerSplit.Dock = DockStyle.Fill;
        _innerSplit.Location = new System.Drawing.Point(0, 0);
        _innerSplit.Name = "_innerSplit";
        _innerSplit.Orientation = Orientation.Vertical;
        _innerSplit.Panel1.Controls.Add(_centerTabControl);
        _innerSplit.Panel2.Controls.Add(_inspectorPanel);
        _innerSplit.Size = new System.Drawing.Size(1056, 527);
        _innerSplit.SplitterDistance = 776;
        _innerSplit.TabIndex = 0;

        // _outerSplit
        _outerSplit.Dock = DockStyle.Fill;
        _outerSplit.Location = new System.Drawing.Point(0, 0);
        _outerSplit.Name = "_outerSplit";
        _outerSplit.Orientation = Orientation.Vertical;
        _outerSplit.Panel1.Controls.Add(_hierarchyPanel);
        _outerSplit.Panel2.Controls.Add(_innerSplit);
        _outerSplit.Size = new System.Drawing.Size(1280, 527);
        _outerSplit.SplitterDistance = 220;
        _outerSplit.TabIndex = 0;

        // _assetBrowserPanel
        _assetBrowserPanel.Dock = DockStyle.Fill;
        _assetBrowserPanel.Location = new System.Drawing.Point(0, 0);
        _assetBrowserPanel.Name = "_assetBrowserPanel";
        _assetBrowserPanel.Size = new System.Drawing.Size(1280, 172);
        _assetBrowserPanel.TabIndex = 0;

        // _assetsTab
        _assetsTab.Controls.Add(_assetBrowserPanel);
        _assetsTab.Location = new System.Drawing.Point(4, 24);
        _assetsTab.Name = "_assetsTab";
        _assetsTab.Padding = new System.Windows.Forms.Padding(0);
        _assetsTab.Size = new System.Drawing.Size(1280, 172);
        _assetsTab.TabIndex = 0;
        _assetsTab.Text = "Assets";

        // _consolePanel
        _consolePanel.Dock = DockStyle.Fill;
        _consolePanel.Location = new System.Drawing.Point(0, 0);
        _consolePanel.Name = "_consolePanel";
        _consolePanel.Size = new System.Drawing.Size(1280, 172);
        _consolePanel.TabIndex = 0;

        // _consoleTab
        _consoleTab.Controls.Add(_consolePanel);
        _consoleTab.Location = new System.Drawing.Point(4, 24);
        _consoleTab.Name = "_consoleTab";
        _consoleTab.Padding = new System.Windows.Forms.Padding(0);
        _consoleTab.Size = new System.Drawing.Size(1280, 172);
        _consoleTab.TabIndex = 1;
        _consoleTab.Text = "Console";

        // _sceneManagerPanel
        _sceneManagerPanel.Dock = DockStyle.Fill;
        _sceneManagerPanel.Location = new System.Drawing.Point(0, 0);
        _sceneManagerPanel.Name = "_sceneManagerPanel";
        _sceneManagerPanel.Size = new System.Drawing.Size(1280, 172);
        _sceneManagerPanel.TabIndex = 0;

        // _sceneManagerTab
        _sceneManagerTab.Controls.Add(_sceneManagerPanel);
        _sceneManagerTab.Location = new System.Drawing.Point(4, 24);
        _sceneManagerTab.Name = "_sceneManagerTab";
        _sceneManagerTab.Padding = new System.Windows.Forms.Padding(0);
        _sceneManagerTab.Size = new System.Drawing.Size(1280, 172);
        _sceneManagerTab.TabIndex = 2;
        _sceneManagerTab.Text = "Scenes";

        // _localizationPanel
        _localizationPanel.Dock = DockStyle.Fill;
        _localizationPanel.Location = new System.Drawing.Point(0, 0);
        _localizationPanel.Name = "_localizationPanel";
        _localizationPanel.Size = new System.Drawing.Size(1280, 172);
        _localizationPanel.TabIndex = 0;

        // _localizationTab
        _localizationTab.Controls.Add(_localizationPanel);
        _localizationTab.Location = new System.Drawing.Point(4, 24);
        _localizationTab.Name = "_localizationTab";
        _localizationTab.Padding = new System.Windows.Forms.Padding(0);
        _localizationTab.Size = new System.Drawing.Size(1280, 172);
        _localizationTab.TabIndex = 3;
        _localizationTab.Text = "Localization";

        // _inputMapEditorPanel
        _inputMapEditorPanel.Dock = DockStyle.Fill;
        _inputMapEditorPanel.Location = new System.Drawing.Point(0, 0);
        _inputMapEditorPanel.Name = "_inputMapEditorPanel";
        _inputMapEditorPanel.Size = new System.Drawing.Size(1280, 172);
        _inputMapEditorPanel.TabIndex = 0;

        // _inputMapEditorTab
        _inputMapEditorTab.Controls.Add(_inputMapEditorPanel);
        _inputMapEditorTab.Location = new System.Drawing.Point(4, 24);
        _inputMapEditorTab.Name = "_inputMapEditorTab";
        _inputMapEditorTab.Padding = new System.Windows.Forms.Padding(0);
        _inputMapEditorTab.Size = new System.Drawing.Size(1280, 172);
        _inputMapEditorTab.TabIndex = 4;
        _inputMapEditorTab.Text = "Input Maps";

        // _bottomTabControl
        _bottomTabControl.Controls.Add(_assetsTab);
        _bottomTabControl.Controls.Add(_consoleTab);
        _bottomTabControl.Controls.Add(_sceneManagerTab);
        _bottomTabControl.Controls.Add(_localizationTab);
        _bottomTabControl.Controls.Add(_inputMapEditorTab);
        _bottomTabControl.Dock = DockStyle.Fill;
        _bottomTabControl.Location = new System.Drawing.Point(0, 0);
        _bottomTabControl.Name = "_bottomTabControl";
        _bottomTabControl.SelectedIndex = 0;
        _bottomTabControl.Size = new System.Drawing.Size(1280, 200);
        _bottomTabControl.TabIndex = 0;

        // _mainSplit
        _mainSplit.Dock = DockStyle.Fill;
        _mainSplit.Location = new System.Drawing.Point(0, 49);
        _mainSplit.Name = "_mainSplit";
        _mainSplit.Orientation = Orientation.Horizontal;
        _mainSplit.Panel1.Controls.Add(_outerSplit);
        _mainSplit.Panel2.Controls.Add(_bottomTabControl);
        _mainSplit.Size = new System.Drawing.Size(1280, 729);
        _mainSplit.SplitterDistance = 527;
        _mainSplit.TabIndex = 2;

        // EditorForm
        AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1280, 800);
        Controls.Add(_mainSplit);
        Controls.Add(_statusStrip);
        Controls.Add(_toolbarTable);
        Controls.Add(_mainMenuStrip);
        Font = new System.Drawing.Font("Segoe UI", 9f);
        MainMenuStrip = _mainMenuStrip;
        MinimumSize = new System.Drawing.Size(800, 600);
        Name = "EditorForm";
        Text = "MonoGame Editor";
        WindowState = FormWindowState.Maximized;

        _toolbarTable.ResumeLayout(false);
        _toolbarTable.PerformLayout();
        _playbackCell.ResumeLayout(false);
        _playbackCell.PerformLayout();
        _mainSplit.Panel1.ResumeLayout(false);
        _mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_mainSplit).EndInit();
        _mainSplit.ResumeLayout(false);
        _outerSplit.Panel1.ResumeLayout(false);
        _outerSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_outerSplit).EndInit();
        _outerSplit.ResumeLayout(false);
        _innerSplit.Panel1.ResumeLayout(false);
        _innerSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_innerSplit).EndInit();
        _innerSplit.ResumeLayout(false);
        _sceneTab.ResumeLayout(false);
        _gameTab.ResumeLayout(false);
        _assetsTab.ResumeLayout(false);
        _consoleTab.ResumeLayout(false);
        _sceneManagerTab.ResumeLayout(false);
        _localizationTab.ResumeLayout(false);
        _inputMapEditorTab.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
