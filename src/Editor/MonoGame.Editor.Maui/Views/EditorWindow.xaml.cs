namespace MonoGame.Editor.Maui.Views;

/// <summary>
/// Ventana principal del editor. Gestiona el toolbar, menú, status bar y coordina el ciclo Play/Stop.
/// Los paneles se comunican exclusivamente a través de <see cref="IEditorEventBus"/>.
/// </summary>
public sealed partial class EditorWindow : ContentPage
{
    #region Constants / colors

    private static readonly Color ActiveToolBg   = Color.FromArgb("#4A9EFF");
    private static readonly Color ActiveToolFg   = Colors.White;
    private static readonly Color InactiveToolBg = Colors.Transparent;
    private static readonly Color InactiveToolFg = Color.FromArgb("#9A9AA2");

    private static readonly Color ActivePillBg       = Color.FromArgb("#4A9EFF");
    private static readonly Color InactivePillBg     = Color.FromArgb("#252528");
    private static readonly Color ActivePillFg       = Colors.White;
    private static readonly Color InactivePillFg     = Color.FromArgb("#9A9AA2");
    private static readonly Color PillBorderActive   = Color.FromArgb("#4A9EFF");
    private static readonly Color PillBorderInactive = Color.FromArgb("#34343A");

    private static readonly Color BuildSuccessColor = Color.FromArgb("#46C66A");
    private static readonly Color BuildErrorColor   = Colors.White;
    private static readonly Color BuildErrorBg      = Color.FromArgb("#C73E3E");
    private static readonly Color BuildNormalBg     = Color.FromArgb("#252528");

    private static readonly Color DropdownItemFg     = Color.FromArgb("#E6E6E8");
    private static readonly Color DropdownItemBg     = Colors.Transparent;
    private static readonly Color DropdownItemHoverBg = Color.FromArgb("#2E2E34");
    private static readonly Color DropdownSeparatorColor = Color.FromArgb("#34343A");

    #endregion

    #region Fields

    private readonly IEditorEventBus    _bus              = EditorContext.Instance.EventBus;
    private readonly GameObjectRegistry _registry         = new();
    private readonly ViewportRenderer   _viewportRenderer = new();
    private readonly EditorPreferences  _preferences      = new();
    private readonly ExternalPlayLauncher _externalLauncher = new();

    private string _activeTool = "Select";
    private bool _is2D   = true;
    private bool _isSnap = false;
    private bool _isNav  = false;
    private bool _isRes  = false;

    private bool _hierarchyVisible = true;
    private bool _inspectorVisible = true;
    private bool _dockVisible      = true;
    private const double HierarchyWidth = 268;
    private const double InspectorWidth = 362;
    private const double DockHeight     = 266;

    private string? _openMenuTag;

    private PlayModeRunner? _activeRunner;
    private double          _panLastX;
    private double          _panLastY;
    private float           _lastPointerScreenX;
    private float           _lastPointerScreenY;
    private float           _panStartScreenX;
    private float           _panStartScreenY;
    private bool            _gizmoDragging;

    private bool   _pointerOverViewport;
    private double _hierSepPanLast;
    private double _inspSepPanLast;
    private double _dockSepPanLast;

    private Action<EditorStateChangedEvent>?  _onStateChanged;
    private Action<SceneLoadedEvent>?         _onSceneLoaded;
    private Action<BuildOutputLineEvent>?     _onBuildOutput;
    private Action<FpsUpdatedEvent>?          _onFpsUpdated;
    private Action<SceneDirtyChangedEvent>?   _onSceneDirty;
    private Action<ProjectOpenedEvent>?       _onProjectOpened;
    private Action<GameObjectSelectedEvent>?  _onGameObjectSelected;

    #endregion

    public EditorWindow()
    {
        InitializeComponent();
        Viewport.Drawable = _viewportRenderer;
        _preferences.Load();
        Subscribe();
        ApplyPreferences();
        SetPillStyle(Toggle2DBtn, _is2D);
        SetPillStyle(ToggleSnapBtn, _isSnap);
        SetPillStyle(ToggleNavBtn, _isNav);
        SetPillStyle(ToggleResBtn, _isRes);
        UpdateToolButtons();
        PlayBtn.IsEnabled = false;
        _ = TryAutoLoadLastProjectAsync();
    }

    #region Lifecycle — keyboard shortcuts

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is not null)
        {
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win?.Content is not null)
            {
                win.Content.AddHandler(
                    Microsoft.UI.Xaml.UIElement.KeyDownEvent,
                    new Microsoft.UI.Xaml.Input.KeyEventHandler(OnNativeKeyDown),
                    handledEventsToo: false);
                win.Content.PointerWheelChanged += OnNativePointerWheelChanged;
                win.Closed += (_, _) => SavePreferences();
            }

            AttachSeparatorCursors();
        }
    }

    private void OnNativeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
            ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        bool textFocused = win?.Content?.XamlRoot is { } root &&
            Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root)
                is Microsoft.UI.Xaml.Controls.TextBox or Microsoft.UI.Xaml.Controls.PasswordBox;

        bool ctrl  = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool shift = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        switch (e.Key)
        {
            // Global shortcuts — always active
            case Windows.System.VirtualKey.Z when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(OnUndoClicked);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Y when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(OnRedoClicked);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = SaveSceneAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && shift:
                MainThread.BeginInvokeOnMainThread(() => _ = SaveSceneAsAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.B when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = BuildSolutionAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.F5 when ctrl:
                MainThread.BeginInvokeOnMainThread(OnPlayClicked);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.G when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = GenerateCodeAsync());
                e.Handled = true;
                return;
        }

        // Viewport shortcuts — skip when a text input is focused
        if (textFocused) return;

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Q:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Select"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.W:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Move"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.E:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Rotate"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.R:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Scale"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.T:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Rect"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.H:
                MainThread.BeginInvokeOnMainThread(() => ActivateTool("Pan"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.G:
                MainThread.BeginInvokeOnMainThread(OnToggleSnap);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Delete:
                MainThread.BeginInvokeOnMainThread(OnDeleteSelected);
                e.Handled = true;
                break;
        }
    }

    #endregion

    #region Lifecycle — mouse wheel zoom

    private void OnNativePointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_pointerOverViewport) return;
        if (e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse) return;

        int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
        if (delta == 0) return;

        float factor = delta > 0 ? 1.1f : 1f / 1.1f;
        SizeF vs     = new((float)Viewport.Width, (float)Viewport.Height);
        Microsoft.Maui.Graphics.PointF focus = new(_lastPointerScreenX, _lastPointerScreenY);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _viewportRenderer.Camera.ZoomAt(factor, focus, vs);
            Viewport.Invalidate();
        });

        e.Handled = true;
    }

    private void OnViewportPointerEntered(object sender, PointerEventArgs e) => _pointerOverViewport = true;
    private void OnViewportPointerExited(object sender, PointerEventArgs e)  => _pointerOverViewport = false;

    #endregion

    #region EventBus subscriptions

    private void Log(string message, LogLevel level = LogLevel.Info)
        => _bus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));

    private void Subscribe()
    {
        _onStateChanged       = e => MainThread.BeginInvokeOnMainThread(() => OnEditorStateChanged(e));
        _onSceneLoaded        = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onBuildOutput        = e => MainThread.BeginInvokeOnMainThread(() => OnBuildOutputLine(e));
        _onFpsUpdated         = e => MainThread.BeginInvokeOnMainThread(() => FpsLabel.Text = $"{e.Fps} FPS");
        _onSceneDirty         = e => MainThread.BeginInvokeOnMainThread(() => OnSceneDirtyChanged(e));
        _onProjectOpened      = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _onGameObjectSelected = _ => MainThread.BeginInvokeOnMainThread(() => Viewport.Invalidate());

        _bus.Subscribe(_onStateChanged);
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onBuildOutput);
        _bus.Subscribe(_onFpsUpdated);
        _bus.Subscribe(_onSceneDirty);
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onGameObjectSelected);
    }

    private void OnEditorStateChanged(EditorStateChangedEvent e)
    {
        bool playing  = e.NewState is EditorState.Playing;
        bool hasScene = EditorContext.Instance.ActiveScene is not null;

        PlayBtn.IsEnabled = !playing && hasScene;
        StopBtn.IsEnabled = playing;

        bool canEdit = !playing;
        SelectBtn.IsEnabled  = canEdit;
        MoveBtn.IsEnabled    = canEdit;
        RotateBtn.IsEnabled  = canEdit;
        ScaleBtn.IsEnabled   = canEdit;
        RectBtn.IsEnabled    = canEdit;
        PanBtn.IsEnabled     = canEdit;
        Toggle2DBtn.IsEnabled   = canEdit;
        ToggleSnapBtn.IsEnabled = canEdit;
        ToggleNavBtn.IsEnabled  = canEdit;
        ToggleResBtn.IsEnabled  = canEdit;

        if (playing)
        {
            StopBtn.BackgroundColor = Color.FromArgb("#E5484D");
            StopBtn.TextColor       = Colors.White;
        }
        else
        {
            StopBtn.BackgroundColor = Color.FromArgb("#252528");
            StopBtn.TextColor       = Color.FromArgb("#6A6A72");
        }
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        int count = e.Scene?.RootGameObjects.Count ?? 0;
        ObjectCountLabel.Text = count == 1 ? "1 object in scene" : $"{count} objects in scene";
        UpdateTitleBar();
        Viewport.Invalidate();

        bool hasScene  = e.Scene is not null;
        bool isPlaying = EditorContext.Instance.State is EditorState.Playing;
        PlayBtn.IsEnabled = hasScene && !isPlaying;

        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is not null && e.Scene is not null && !string.IsNullOrEmpty(e.Scene.ScenePath))
            _ = Task.Run(() => ProjectManager.SaveLastOpenedScene(project, e.Scene.ScenePath));
    }

    private void OnBuildOutputLine(BuildOutputLineEvent e)
    {
        string line = e.Line;

        if (line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            BuildStatusLabel.Text                  = "Build succeeded";
            BuildStatusLabel.TextColor             = BuildSuccessColor;
            BuildStatusSegment.BackgroundColor     = BuildNormalBg;
        }
        else if (line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
              || (e.IsError && line.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            BuildStatusLabel.Text                  = "Build failed";
            BuildStatusLabel.TextColor             = BuildErrorColor;
            BuildStatusSegment.BackgroundColor     = BuildErrorBg;
        }
    }

    private void OnSceneDirtyChanged(SceneDirtyChangedEvent e)
    {
        UpdateTitleBar();
        Viewport.Invalidate();
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        UpdateTitleBar();
        _registry.Scan();
    }

    private void UpdateTitleBar()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        bool           dirty   = EditorContext.Instance.IsSceneDirty;

        string projectPart = project?.Name ?? "No Project";
        string scenePart   = scene?.Name   ?? "No Scene";
        string dirtyMark   = dirty ? " ●" : string.Empty;

        Title = $"MonoGame Editor — {projectPart} — {scenePart}{dirtyMark}";
    }

    // ── Preferences ───────────────────────────────────────────────────────────

    private void ApplyPreferences()
    {
        _hierarchyVisible = _preferences.HierarchyVisible;
        _inspectorVisible = _preferences.InspectorVisible;
        _dockVisible      = _preferences.AssetBrowserVisible;

        BodyGrid.ColumnDefinitions[0].Width = new GridLength(_hierarchyVisible ? _preferences.LeftPanelWidth  : 0);
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(_inspectorVisible ? _preferences.RightPanelWidth : 0);
        MainGrid.RowDefinitions[3].Height   = new GridLength(_dockVisible      ? _preferences.ConsolePanelHeight : 0);
        HierarchySep.IsVisible = _hierarchyVisible;
        InspectorSep.IsVisible = _inspectorVisible;
        DockRow.IsVisible      = _dockVisible;

        _viewportRenderer.GridCellSize                    = _preferences.GridCellSize;
        EditorContext.Instance.Gizmos.GridCellSize        = _preferences.GridCellSize;
        EditorContext.Instance.Gizmos.SnapRotationDegrees = _preferences.SnapRotationDegrees;
        EditorContext.Instance.Gizmos.SnapScaleStep       = _preferences.SnapScaleStep;
    }

    private void SavePreferences()
    {
        _preferences.HierarchyVisible    = _hierarchyVisible;
        _preferences.InspectorVisible    = _inspectorVisible;
        _preferences.AssetBrowserVisible = _dockVisible;
        _preferences.LeftPanelWidth      = (int)(BodyGrid.ColumnDefinitions[0].Width.Value);
        _preferences.RightPanelWidth     = (int)(BodyGrid.ColumnDefinitions[4].Width.Value);
        _preferences.ConsolePanelHeight  = (int)(MainGrid.RowDefinitions[3].Height.Value);
        _preferences.GridCellSize        = _viewportRenderer.GridCellSize;
        _preferences.SnapRotationDegrees = EditorContext.Instance.Gizmos.SnapRotationDegrees;
        _preferences.SnapScaleStep       = EditorContext.Instance.Gizmos.SnapScaleStep;
        _preferences.Save();
    }

    private async Task TryAutoLoadLastProjectAsync()
    {
        if (string.IsNullOrEmpty(_preferences.LastProjectPath)) return;
        if (!Directory.Exists(_preferences.LastProjectPath)) return;

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(_preferences.LastProjectPath))
                                               .ConfigureAwait(true);
            if (project is null) return;
            EditorContext.Instance.SetActiveProject(project);
            Log($"[Editor] Auto-loaded project '{project.Name}'.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Auto-load failed: {ex.Message}", LogLevel.Warning);
        }
    }

    private async Task TryLoadLastSceneForProjectAsync(EditorProject project)
    {
        string lastScene = await Task.Run(() => ProjectManager.GetLastOpenedScene(project.RootPath))
                                     .ConfigureAwait(true);
        if (string.IsNullOrEmpty(lastScene) || !File.Exists(lastScene)) return;

        try
        {
            EditorScene? scene = await SceneSerializer.LoadAsync(lastScene).ConfigureAwait(true);
            if (scene is null) return;
            EditorContext.Instance.SetActiveScene(scene);
            Log($"[Editor] Auto-loaded last scene '{scene.Name}'.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Failed to auto-load last scene: {ex.Message}", LogLevel.Warning);
        }
    }

    #endregion

    #region Menu bar — dropdown management

    private void OnFileMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "File") { HideDropdown(); return; }
        ShowDropdown("File", 4, BuildFileMenuItems());
    }

    private void OnEditMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Edit") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + 4);
        ShowDropdown("Edit", offsetX, BuildEditMenuItems());
    }

    private void OnProjectMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Project") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + 4);
        ShowDropdown("Project", offsetX, BuildProjectMenuItems());
    }

    private void OnDebugMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "Debug") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + ProjectMenuBtn.Width + 4);
        ShowDropdown("Debug", offsetX, BuildDebugMenuItems());
    }

    private void OnViewMenuClicked(object sender, EventArgs e)
    {
        if (_openMenuTag == "View") { HideDropdown(); return; }
        int offsetX = (int)(FileMenuBtn.Width + EditMenuBtn.Width + ProjectMenuBtn.Width + DebugMenuBtn.Width + 4);
        ShowDropdown("View", offsetX, BuildViewMenuItems());
    }

    private void OnMenuOverlayTapped(object? sender, TappedEventArgs e) => HideDropdown();

    private void ShowDropdown(string tag, int offsetX,
                              IEnumerable<(string Label, bool IsSeparator, Action? Action)> items)
    {
        _openMenuTag = tag;
        DropdownStack.Children.Clear();

        foreach (var (label, isSep, action) in items)
        {
            if (isSep)
            {
                DropdownStack.Children.Add(new BoxView
                {
                    HeightRequest   = 1,
                    Color           = DropdownSeparatorColor,
                    Margin          = new Thickness(8, 2),
                });
                continue;
            }

            bool isDisabled = action is null;

            var row = new Grid
            {
                BackgroundColor = DropdownItemBg,
                Padding         = new Thickness(16, 6),
                MinimumHeightRequest = 32,
            };

            row.Add(new Label
            {
                Text                    = label,
                TextColor               = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize                = 13,
                VerticalTextAlignment   = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions         = LayoutOptions.Fill,
            });

            if (!isDisabled)
            {
                var captured = action!;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => { HideDropdown(); captured(); };
                row.GestureRecognizers.Add(tap);

                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) => row.BackgroundColor = DropdownItemHoverBg;
                pointer.PointerExited  += (_, _) => row.BackgroundColor = DropdownItemBg;
                row.GestureRecognizers.Add(pointer);
            }

            DropdownStack.Children.Add(row);
        }

        DropdownPanel.Margin = new Thickness(offsetX, 28, 0, 0);
        MenuOverlay.IsVisible = true;
    }

    private void HideDropdown()
    {
        MenuOverlay.IsVisible = false;
        _openMenuTag = null;
    }

    #endregion

    #region Menu item builders

    private IEnumerable<(string, bool, Action?)> BuildFileMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return ("New Project…",   false, () => _ = NewProjectAsync());
        yield return ("Open Project…",  false, () => _ = OpenProjectAsync());

        if (_preferences.RecentProjects.Count > 0)
        {
            yield return ("---", true, null);
            yield return ("Recent Projects", false, null);
            foreach (string path in _preferences.RecentProjects)
            {
                string captured = path;
                string label    = $"  {Path.GetFileName(captured)}";
                yield return (label, false, () => _ = OpenProjectByPathAsync(captured));
            }
        }

        yield return ("---",            true,  null);
        yield return ("New Scene",      false, hasProject ? () => _ = NewSceneAsync()    : null);
        yield return ("Save Scene",     false, hasScene   ? () => _ = SaveSceneAsync()   : null);
        yield return ("Save Scene As…", false, hasScene   ? () => _ = SaveSceneAsAsync() : null);
        yield return ("---",            true,  null);
        yield return ("Exit",           false, OnExitClicked);
    }

    private IEnumerable<(string, bool, Action?)> BuildEditMenuItems()
    {
        bool hasScene     = EditorContext.Instance.ActiveScene is not null;
        bool hasSelection = EditorContext.Instance.SelectedObject is not null;
        bool hasClipboard = EditorContext.Instance.ClipboardEntity is not null;

        yield return ("Undo",       false, hasScene                    ? OnUndoClicked       : null);
        yield return ("Redo",       false, hasScene                    ? OnRedoClicked       : null);
        yield return ("---",        true,  null);
        yield return ("Cut",        false, hasSelection                ? OnCut               : null);
        yield return ("Copy",       false, hasSelection                ? OnCopy              : null);
        yield return ("Paste",      false, hasScene && hasClipboard    ? OnPaste             : null);
        yield return ("Duplicate",  false, hasSelection                ? OnDuplicateSelected : null);
        yield return ("Delete",     false, hasSelection                ? OnDeleteSelected    : null);
        yield return ("---",        true,  null);
        yield return ("Select All", false, hasScene                    ? OnSelectAll         : null);
    }

    private IEnumerable<(string, bool, Action?)> BuildProjectMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return ("Project Settings…", false, hasProject             ? () => _ = OpenProjectSettingsAsync() : null);
        yield return ("---",               true,  null);
        yield return ("Build Content",     false, hasProject             ? () => _ = BuildContentAsync()        : null);
        yield return ("Build Solution",    false, hasProject             ? () => _ = BuildSolutionAsync()       : null);
        yield return ("Generate Code",     false, hasProject && hasScene ? () => _ = GenerateCodeAsync()        : null);
        yield return ("---",               true,  null);
        yield return ("Run",               false, hasProject             ? OnRunGame                            : null);
    }

    private IEnumerable<(string, bool, Action?)> BuildDebugMenuItems()
    {
        bool hasScene  = EditorContext.Instance.ActiveScene is not null;
        bool isPlaying = EditorContext.Instance.State is EditorState.Playing;

        yield return ("Play", false, hasScene && !isPlaying ? OnPlayClicked : null);
        yield return ("Stop", false, isPlaying              ? OnStopClicked : null);
    }

    private IEnumerable<(string, bool, Action?)> BuildViewMenuItems()
    {
        string hPfx = _hierarchyVisible ? "✓ " : "  ";
        string iPfx = _inspectorVisible ? "✓ " : "  ";
        string dPfx = _dockVisible      ? "✓ " : "  ";

        yield return ($"{hPfx}Hierarchy",   false, ToggleHierarchy);
        yield return ($"{iPfx}Inspector",   false, ToggleInspector);
        yield return ($"{dPfx}Bottom Dock", false, ToggleDock);
        yield return ("---",                true,  null);
        yield return ("Reset Layout",       false, ResetLayout);
    }

    private void ToggleHierarchy()
    {
        _hierarchyVisible = !_hierarchyVisible;
        double w = _hierarchyVisible ? Math.Max(_preferences.LeftPanelWidth, 120) : 0;
        BodyGrid.ColumnDefinitions[0].Width = new GridLength(w);
        HierarchySep.IsVisible = _hierarchyVisible;
        SavePreferences();
    }

    private void ToggleInspector()
    {
        _inspectorVisible = !_inspectorVisible;
        double w = _inspectorVisible ? Math.Max(_preferences.RightPanelWidth, 120) : 0;
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(w);
        InspectorSep.IsVisible = _inspectorVisible;
        SavePreferences();
    }

    private void ToggleDock()
    {
        _dockVisible = !_dockVisible;
        double h = _dockVisible ? Math.Max(_preferences.ConsolePanelHeight, 80) : 0;
        MainGrid.RowDefinitions[3].Height = new GridLength(h);
        DockRow.IsVisible = _dockVisible;
        SavePreferences();
    }

    private void ResetLayout()
    {
        _hierarchyVisible = true;
        _inspectorVisible = true;
        _dockVisible      = true;
        BodyGrid.ColumnDefinitions[0].Width = new GridLength(HierarchyWidth);
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(InspectorWidth);
        MainGrid.RowDefinitions[3].Height   = new GridLength(DockHeight);
        HierarchySep.IsVisible = true;
        InspectorSep.IsVisible = true;
        DockRow.IsVisible      = true;
        _preferences.LeftPanelWidth     = (int)HierarchyWidth;
        _preferences.RightPanelWidth    = (int)InspectorWidth;
        _preferences.ConsolePanelHeight = (int)DockHeight;
        SavePreferences();
    }

    #endregion

    #region Menu actions — File

    private async Task NewProjectAsync()
    {
        var result = await Views.Dialogs.NewProjectDialog.ShowAsync(Navigation);
        if (result is null) return;

        try
        {
            EditorProject project = await Task.Run(() =>
                ProjectManager.Create(result.ProjectName, result.ParentPath, result.GameCsprojPath))
                .ConfigureAwait(true);

            EditorContext.Instance.SetActiveProject(project);
            _preferences.LastProjectPath = project.RootPath;
            _preferences.AddRecentProject(project.RootPath);
            Log($"[Editor] Project '{project.Name}' created.");
        }
        catch (Exception ex)
        {
            Log($"[Editor] Failed to create project: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task OpenProjectAsync()
    {
        try
        {
            string? path = await PickFolderAsync();
            if (path is null) return;
            await OpenProjectByPathAsync(path).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task OpenProjectByPathAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            bool remove = await this.DisplayAlertAsync(
                "Project not found",
                $"The project '{Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}' could not be found at:\n{path}\n\nDo you want to remove it from recent projects?",
                "Remove",
                "Keep").ConfigureAwait(true);
            if (remove)
                _preferences.RemoveRecentProject(path);
            return;
        }

        try
        {
            EditorProject? project = await Task.Run(() => ProjectManager.Load(path)).ConfigureAwait(true);
            if (project is null)
            {
                Log($"[Editor] No valid project found at: {path}", LogLevel.Warning);
                return;
            }

            EditorContext.Instance.SetActiveProject(project);
            _preferences.LastProjectPath = path;
            _preferences.AddRecentProject(path);
            Log($"[Editor] Project '{project.Name}' opened.");
            await TryLoadLastSceneForProjectAsync(project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Editor] Open project error: {ex.Message}", LogLevel.Error);
        }
    }

    private static async Task<string?> PickFolderAsync()
    {
        try
        {
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win is null) return null;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            var picker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch
        {
            return null;
        }
    }

    private async Task NewSceneAsync()
    {
        var result = await Views.Dialogs.NewSceneDialog.ShowAsync(Navigation);
        if (result is null) return;

        EditorProject? project = EditorContext.Instance.ActiveProject;

        EditorScene scene = new()
        {
            Name      = result.SceneName,
            WorldSize = new EditorVector2(result.WorldWidth, result.WorldHeight),
        };

        if (project is not null && !string.IsNullOrEmpty(project.ScenesPath))
        {
            Directory.CreateDirectory(project.ScenesPath);
            string safeName = string.Concat(scene.Name.Split(Path.GetInvalidFileNameChars()));
            string scenePath = Path.Combine(project.ScenesPath, safeName + ".scene.json");
            scene.ScenePath  = scenePath;
            try
            {
                await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log($"[Editor] Failed to save scene: {ex.Message}", LogLevel.Error);
            }
        }

        EditorContext.Instance.SetActiveScene(scene);
        _bus.Publish(new SceneCreatedEvent(scene));
        Log($"[Editor] New scene '{scene.Name}' created.");
    }

    private async Task SaveSceneAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;

        if (scene is null) return;

        string scenePath = scene.ScenePath;

        if (string.IsNullOrEmpty(scenePath))
        {
            await SaveSceneAsAsync().ConfigureAwait(true);
            return;
        }

        try
        {
            await SceneSerializer.SaveAsync(scene, scenePath).ConfigureAwait(true);
            EditorContext.Instance.MarkSceneClean();
            BuildStatusLabel.Text      = "Saved";
            BuildStatusLabel.TextColor = BuildSuccessColor;
            Log($"[Save] Scene saved to {scenePath}");

            if (project is not null)
                await TryGenerateCodeOnSaveAsync(scene, project).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task SaveSceneAsAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;

        if (scene is null) return;

        try
        {
            string initialDir = project is not null && Directory.Exists(project.ScenesPath)
                ? project.ScenesPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string suggestedName = string.IsNullOrEmpty(scene.Name)
                ? "NewScene.scene.json"
                : $"{scene.Name}.scene.json";

            FileResult? picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Save Scene As",
            }).ConfigureAwait(true);

            if (picked is null) return;
            string path = picked.FullPath;
            if (!path.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase))
                path += ".scene.json";

            scene.ScenePath = path;
            scene.Name      = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(path));

            await SceneSerializer.SaveAsync(scene, path).ConfigureAwait(true);
            EditorContext.Instance.MarkSceneClean();
            Log($"[Save] Scene saved to {path}");
        }
        catch (Exception ex)
        {
            Log($"[Save] Error: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task TryGenerateCodeOnSaveAsync(EditorScene scene, EditorProject project)
    {
        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (!settings.GenerateOnSave) return;
        if (string.IsNullOrEmpty(project.GameCsprojPath)) return;
        if (string.IsNullOrWhiteSpace(settings.RootNamespace)) return;

        _bus.Publish(new CodeGenStartedEvent(scene.Name));

        ICodeGenService codeGen = new SceneCodeGenerator();
        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings)
                                            .ConfigureAwait(true);

        _bus.Publish(new CodeGenCompletedEvent(result));
        Log(
            result.Success ? $"[CodeGen] {result.OutputPath}" : $"[CodeGen] Error: {result.ErrorMessage}",
            result.Success ? LogLevel.Info : LogLevel.Error);
    }

    private void OnExitClicked()
        => Application.Current?.CloseWindow(Application.Current.Windows.First());

    #endregion

    #region Menu actions — Edit

    private void OnUndoClicked()
        => EditorContext.Instance.Commands.Undo();

    private void OnRedoClicked()
        => EditorContext.Instance.Commands.Redo();

    private void OnDeleteSelected()
    {
        EditorGameObject? obj   = EditorContext.Instance.SelectedObject;
        EditorScene?      scene = EditorContext.Instance.ActiveScene;
        if (obj is null || scene is null) return;
        EditorContext.Instance.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    private void OnDuplicateSelected()
    {
        EditorGameObject? obj   = EditorContext.Instance.SelectedObject;
        EditorScene?      scene = EditorContext.Instance.ActiveScene;
        if (obj is null || scene is null) return;
        EditorContext.Instance.Commands.Execute(new DuplicateEntityCommand(obj, scene));
    }

    private void OnSelectAll()
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;
        List<EditorGameObject> all = [];
        CollectAll(scene.RootGameObjects, all);
        EditorContext.Instance.SetMultiSelection(all);
    }

    private void OnCopy()
    {
        EditorGameObject? obj = EditorContext.Instance.SelectedObject;
        if (obj is null) return;
        EditorContext.Instance.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
    }

    private void OnCut()
    {
        EditorGameObject? obj   = EditorContext.Instance.SelectedObject;
        EditorScene?      scene = EditorContext.Instance.ActiveScene;
        if (obj is null || scene is null) return;
        EditorContext.Instance.SetClipboard(DuplicateEntityCommand.DeepClone(obj, null));
        EditorContext.Instance.Commands.Execute(new DeleteEntityCommand(obj, scene));
    }

    private void OnPaste()
    {
        EditorGameObject? clipboard = EditorContext.Instance.ClipboardEntity;
        EditorScene?      scene     = EditorContext.Instance.ActiveScene;
        if (clipboard is null || scene is null) return;
        EditorGameObject? parent = EditorContext.Instance.SelectedObject;
        EditorGameObject  clone  = DuplicateEntityCommand.DeepClone(clipboard, parent);
        EditorContext.Instance.Commands.Execute(new CreateEntityCommand(clone, scene, parent));
    }

    private static void CollectAll(List<EditorGameObject> objects, List<EditorGameObject> result)
    {
        foreach (EditorGameObject obj in objects)
        {
            result.Add(obj);
            CollectAll(obj.Children, result);
        }
    }

    #endregion

    #region Menu actions — Project

    private async Task OpenProjectSettingsAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        await Views.Dialogs.ProjectSettingsDialog.ShowAsync(Navigation, project, settings)
                                                 .ConfigureAwait(true);
    }

    private async Task BuildContentAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        string mgcbFile = Path.Combine(project.ContentPath, "Content.mgcb");
        if (!File.Exists(mgcbFile))
        {
            Log($"[Build] Content.mgcb not found at: {mgcbFile}", LogLevel.Warning);
            BuildStatusLabel.Text      = "Content.mgcb not found";
            BuildStatusLabel.TextColor = Color.FromArgb("#E8A050");
            return;
        }

        BuildStatusLabel.Text      = "Building content…";
        BuildStatusLabel.TextColor = Color.FromArgb("#9A9AA2");
        BuildStatusSegment.BackgroundColor = BuildNormalBg;

        int exit = await MgcbRunner.RunAsync(mgcbFile, line =>
            _bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line))))
            .ConfigureAwait(true);

        if (exit == 0)
        {
            _bus.Publish(new BuildOutputLineEvent("Build succeeded", false));
        }
        else
        {
            _bus.Publish(new BuildOutputLineEvent($"Build FAILED (exit {exit})", true));
        }

        _bus.Publish(new BuildFinishedEvent(exit, "Content"));
    }

    private async Task BuildSolutionAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) return;

        string csproj = project.GameCsprojPath;
        if (string.IsNullOrEmpty(csproj) || !File.Exists(csproj))
        {
            Log("[Build] Game .csproj path not configured.", LogLevel.Warning);
            return;
        }

        BuildStatusLabel.Text      = "Building solution…";
        BuildStatusLabel.TextColor = Color.FromArgb("#9A9AA2");

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        int exit = await MgcbRunner.RunDotnetBuildAsync(csproj, settings.BuildConfiguration, line =>
            _bus.Publish(new BuildOutputLineEvent(line, IsErrorLine(line))))
            .ConfigureAwait(true);

        _bus.Publish(new BuildOutputLineEvent(
            exit == 0 ? "Build succeeded" : $"Build FAILED (exit {exit})",
            exit != 0));

        _bus.Publish(new BuildFinishedEvent(exit, "Solution"));
    }

    private async Task GenerateCodeAsync()
    {
        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (scene is null || project is null) return;

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(settings.RootNamespace))
        {
            Log("[CodeGen] RootNamespace not set in Project Settings.", LogLevel.Warning);
            return;
        }

        var progressDlg = new Views.Dialogs.CodeGenProgressDialog();
        _ = Navigation.PushModalAsync(progressDlg);

        _bus.Publish(new CodeGenStartedEvent(scene.Name));
        ICodeGenService codeGen = new SceneCodeGenerator();

        CodeGenResult result = await codeGen.GenerateSceneAsync(scene, project, settings)
                                            .ConfigureAwait(true);

        progressDlg.AddFileResult(result.OutputPath ?? string.Empty, result.Success);
        progressDlg.MarkComplete(result.Success ? 1 : 0, result.Success ? 0 : 1);
        _bus.Publish(new CodeGenCompletedEvent(result));
        _bus.Publish(new BuildFinishedEvent(result.Success ? 0 : 1, "CodeGen"));
    }

    private void OnRunGame()
        => _ = RunGameAsync();

    private async Task RunGameAsync()
    {
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null || string.IsNullOrEmpty(project.GameCsprojPath)) return;

        string dir = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
        string exeName = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";

        ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
        string[] searchDirs =
        [
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0-windows"),
            Path.Combine(dir, "bin", settings.BuildConfiguration, "net10.0-windows10.0.19041.0"),
        ];

        foreach (string candidate in searchDirs)
        {
            string exePath = Path.Combine(candidate, exeName);
            if (!File.Exists(exePath)) continue;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName         = exePath,
                WorkingDirectory = dir,
                UseShellExecute  = true,
            });
            return;
        }

        bool buildNow = await this.DisplayAlertAsync(
            "Executable not found",
            $"'{exeName}' was not found in the output directories. Build the solution first?",
            "Build Now",
            "Cancel").ConfigureAwait(true);

        if (buildNow)
            await BuildSolutionAsync().ConfigureAwait(true);
    }

    private static bool IsErrorLine(string line)
        => line.Contains(": error ", StringComparison.OrdinalIgnoreCase)
        || line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Toolbar — gizmo tools

    private void OnToolClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string tool = btn.CommandParameter as string ?? "Select";
        ActivateTool(tool);
    }

    private void ActivateTool(string tool)
    {
        _activeTool = tool;
        UpdateToolButtons();
        EditorContext.Instance.Gizmos.Mode = _activeTool switch
        {
            "Move"   => GizmoMode.Move,
            "Rotate" => GizmoMode.Rotate,
            "Scale"  => GizmoMode.Scale,
            "Rect"   => GizmoMode.Rect,
            _        => GizmoMode.Select,
        };
        Viewport.Invalidate();
    }

    private void UpdateToolButtons()
    {
        SetToolStyle(SelectBtn, _activeTool == "Select");
        SetToolStyle(MoveBtn,   _activeTool == "Move");
        SetToolStyle(RotateBtn, _activeTool == "Rotate");
        SetToolStyle(ScaleBtn,  _activeTool == "Scale");
        SetToolStyle(RectBtn,   _activeTool == "Rect");
        SetToolStyle(PanBtn,    _activeTool == "Pan");
    }

    private static void SetToolStyle(Button btn, bool active)
    {
        btn.BackgroundColor = active ? ActiveToolBg  : InactiveToolBg;
        btn.TextColor       = active ? ActiveToolFg  : InactiveToolFg;
    }

    #endregion

    #region Toolbar — mode toggles

    private void OnToggle2D(object sender, EventArgs e)
    {
        _is2D = !_is2D;
        SetPillStyle(Toggle2DBtn, _is2D);
        EditorContext.Instance.Gizmos.IsDepthMode = !_is2D;
    }

    private void OnToggleSnap(object sender, EventArgs e) => OnToggleSnap();
    private void OnToggleSnap()
    {
        _isSnap = !_isSnap;
        SetPillStyle(ToggleSnapBtn, _isSnap);
        EditorContext.Instance.Gizmos.SnapEnabled = _isSnap;
    }

    private void OnToggleNav(object sender, EventArgs e)
    {
        _isNav = !_isNav;
        SetPillStyle(ToggleNavBtn, _isNav);
    }

    private void OnToggleRes(object sender, EventArgs e)
    {
        _isRes = !_isRes;
        SetPillStyle(ToggleResBtn, _isRes);
    }

    private static void SetPillStyle(Button btn, bool active)
    {
        btn.BackgroundColor = active ? ActivePillBg  : InactivePillBg;
        btn.TextColor       = active ? ActivePillFg  : InactivePillFg;
        btn.BorderColor     = active ? PillBorderActive : PillBorderInactive;
    }

    #endregion

    #region Toolbar — transport (Phase 10)

    private void OnPlayClicked(object sender, EventArgs e) => OnPlayClicked();
    private void OnPlayClicked()
    {
        if (EditorContext.Instance.State is EditorState.Playing) return;

        EditorScene?   scene   = EditorContext.Instance.ActiveScene;
        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (scene is null) return;

        _registry.Scan();
        EditorContext.Instance.TakePlaySnapshot();
        EditorContext.Instance.SetState(EditorState.Playing);

        if (project is not null && !string.IsNullOrEmpty(scene.ScenePath))
        {
            string dir      = Path.GetDirectoryName(project.GameCsprojPath) ?? string.Empty;
            string exeName  = Path.GetFileNameWithoutExtension(project.GameCsprojPath) + ".exe";
            string[] search =
            [
                Path.Combine(dir, "bin", "Debug",   "net10.0-windows"),
                Path.Combine(dir, "bin", "Debug",   "net10.0-windows10.0.19041.0"),
                Path.Combine(dir, "bin", "Release",  "net10.0-windows"),
                Path.Combine(dir, "bin", "Release",  "net10.0-windows10.0.19041.0"),
            ];

            string? exePath = null;
            foreach (string candidate in search)
            {
                string p = Path.Combine(candidate, exeName);
                if (File.Exists(p)) { exePath = p; break; }
            }

            if (exePath is not null)
            {
                _externalLauncher.Launch(exePath, scene.ScenePath,
                    line => _bus.Publish(new BuildOutputLineEvent(line, false)));
                Log("[Play] External game process started.");
                return;
            }
        }

        // Fallback: in-editor runner
        _activeRunner = new PlayModeRunner(scene, _registry);
        Log("[Play] Play mode started (in-editor).");
    }

    private void OnStopClicked(object sender, EventArgs e) => OnStopClicked();
    private void OnStopClicked()
    {
        EditorState state = EditorContext.Instance.State;
        if (state is EditorState.Editing) return;

        _externalLauncher.Stop();

        _activeRunner?.Dispose();
        _activeRunner = null;

        EditorScene? restored = EditorContext.Instance.RestoreFromSnapshot();
        EditorContext.Instance.ClearPlaySnapshot();

        if (restored is not null)
            EditorContext.Instance.SetActiveScene(restored);

        EditorContext.Instance.SetState(EditorState.Editing);
        Log("[Play] Play mode stopped.");
    }

    #endregion

    #region Viewport — input

    private void OnViewportTapped(object sender, TappedEventArgs e)
    {
        if (EditorContext.Instance.Gizmos.Mode != GizmoMode.Select) return;

        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        Point? tapPos = e.GetPosition(Viewport);
        if (tapPos is null) return;

        Microsoft.Maui.Graphics.SizeF viewSize = new((float)Viewport.Width, (float)Viewport.Height);
        Microsoft.Maui.Graphics.PointF worldPos = _viewportRenderer.Camera.ScreenToWorld(
            new Microsoft.Maui.Graphics.PointF((float)tapPos.Value.X, (float)tapPos.Value.Y),
            viewSize);

        EditorGameObject? hit = HitTest(scene.RootGameObjects, worldPos);
        EditorContext.Instance.SetSelection(hit);
    }

    private void OnViewportPointerMoved(object sender, PointerEventArgs e)
    {
        Point? pos = e.GetPosition(Viewport);
        if (pos is null) return;
        _lastPointerScreenX = (float)pos.Value.X;
        _lastPointerScreenY = (float)pos.Value.Y;
    }

    private void OnViewportPanned(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
            {
                _panLastX        = 0;
                _panLastY        = 0;
                _panStartScreenX = _lastPointerScreenX;
                _panStartScreenY = _lastPointerScreenY;
                _gizmoDragging   = false;

                EditorGameObject? sel = EditorContext.Instance.SelectedObject;
                if (sel is not null && _activeTool is not "Select" and not "Pan")
                {
                    SizeF vs = new((float)Viewport.Width, (float)Viewport.Height);
                    Microsoft.Maui.Graphics.PointF clickWorld = _viewportRenderer.Camera.ScreenToWorld(
                        new Microsoft.Maui.Graphics.PointF(_panStartScreenX, _panStartScreenY), vs);
                    Microsoft.Maui.Graphics.PointF objScreen = _viewportRenderer.Camera.WorldToScreen(
                        new Microsoft.Maui.Graphics.PointF(sel.Position.X, sel.Position.Y), vs);

                    _gizmoDragging = EditorContext.Instance.Gizmos.BeginDrag(
                        _panStartScreenX, _panStartScreenY,
                        objScreen.X,     objScreen.Y,
                        clickWorld.X,    clickWorld.Y,
                        sel);
                }
                break;
            }

            case GestureStatus.Running:
            {
                double dx = e.TotalX - _panLastX;
                double dy = e.TotalY - _panLastY;
                _panLastX = e.TotalX;
                _panLastY = e.TotalY;

                if (_gizmoDragging)
                {
                    EditorGameObject? sel = EditorContext.Instance.SelectedObject;
                    if (sel is not null)
                    {
                        SizeF vs      = new((float)Viewport.Width, (float)Viewport.Height);
                        float screenX = _panStartScreenX + (float)e.TotalX;
                        float screenY = _panStartScreenY + (float)e.TotalY;
                        Microsoft.Maui.Graphics.PointF world  = _viewportRenderer.Camera.ScreenToWorld(
                            new Microsoft.Maui.Graphics.PointF(screenX, screenY), vs);
                        Microsoft.Maui.Graphics.PointF objSc = _viewportRenderer.Camera.WorldToScreen(
                            new Microsoft.Maui.Graphics.PointF(sel.Position.X, sel.Position.Y), vs);
                        EditorContext.Instance.Gizmos.UpdateDrag(
                            world.X, world.Y, screenX, screenY, objSc.X, objSc.Y, sel);
                        Viewport.Invalidate();
                    }
                }
                else if (_activeTool == "Pan")
                {
                    float zoom = _viewportRenderer.Camera.Zoom;
                    _viewportRenderer.Camera.Pan(
                        new Microsoft.Maui.Graphics.PointF((float)(-dx / zoom), (float)(-dy / zoom)));
                    Viewport.Invalidate();
                }
                break;
            }

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
            {
                if (_gizmoDragging)
                {
                    IEditorCommand? cmd = EditorContext.Instance.Gizmos.EndDrag(
                        EditorContext.Instance.SelectedObject, ctrlHeld: false);
                    if (cmd is not null)
                    {
                        EditorContext.Instance.Commands.Execute(cmd);
                        EditorGameObject? dragSel = EditorContext.Instance.SelectedObject;
                        if (dragSel is not null)
                            _bus.Publish(new GameObjectSelectedEvent(dragSel));
                    }
                    Viewport.Invalidate();
                }
                _gizmoDragging = false;
                _panLastX      = 0;
                _panLastY      = 0;
                break;
            }
        }
    }

    private void OnViewportPinched(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status != GestureStatus.Running) return;
        SizeF vs = new((float)Viewport.Width, (float)Viewport.Height);
        Microsoft.Maui.Graphics.PointF focus = new(_lastPointerScreenX, _lastPointerScreenY);
        _viewportRenderer.Camera.ZoomAt((float)e.Scale, focus, vs);
        Viewport.Invalidate();
    }

    private static EditorGameObject? HitTest(List<EditorGameObject> objects, Microsoft.Maui.Graphics.PointF worldPos)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            EditorGameObject obj = objects[i];
            if (!obj.Active) continue;

            if (obj.Children.Count > 0)
            {
                EditorGameObject? child = HitTest(obj.Children, worldPos);
                if (child is not null) return child;
            }

            const float defaultHalfSize = 16f;
            float halfW = defaultHalfSize * obj.Scale.X;
            float halfH = defaultHalfSize * obj.Scale.Y;

            if (worldPos.X >= obj.Position.X - halfW && worldPos.X <= obj.Position.X + halfW &&
                worldPos.Y >= obj.Position.Y - halfH && worldPos.Y <= obj.Position.Y + halfH)
                return obj;
        }

        return null;
    }

    #endregion

    #region Panel resizing

    // Attaches hover-highlight feedback and native resize-cursor to every separator strip.
    private void AttachSeparatorCursors()
    {
        AddSeparatorDrag(
            HierarchySep,
            isVertical: true,
            onDrag: (dx, _) =>
            {
                double newW = Math.Clamp(BodyGrid.ColumnDefinitions[0].Width.Value + dx, 120, 600);
                BodyGrid.ColumnDefinitions[0].Width = new GridLength(newW);
            },
            onDragEnd: () =>
            {
                _preferences.LeftPanelWidth = (int)BodyGrid.ColumnDefinitions[0].Width.Value;
                SavePreferences();
            });

        AddSeparatorDrag(
            InspectorSep,
            isVertical: true,
            onDrag: (dx, _) =>
            {
                double newW = Math.Clamp(BodyGrid.ColumnDefinitions[4].Width.Value - dx, 120, 600);
                BodyGrid.ColumnDefinitions[4].Width = new GridLength(newW);
            },
            onDragEnd: () =>
            {
                _preferences.RightPanelWidth = (int)BodyGrid.ColumnDefinitions[4].Width.Value;
                SavePreferences();
            });

        AddSeparatorDrag(
            DockSep,
            isVertical: false,
            onDrag: (_, dy) =>
            {
                double newH = Math.Clamp(MainGrid.RowDefinitions[3].Height.Value - dy, 80, 500);
                MainGrid.RowDefinitions[3].Height = new GridLength(newH);
            },
            onDragEnd: () =>
            {
                _preferences.ConsolePanelHeight = (int)MainGrid.RowDefinitions[3].Height.Value;
                SavePreferences();
            });
    }

    private void AddSeparatorDrag(BoxView sep, bool isVertical,
                                   Action<double, double> onDrag, Action onDragEnd)
    {
        Color idle  = Color.FromArgb("#34343A");
        Color hover = Color.FromArgb("#4A9EFF");

        PointerGestureRecognizer ptr = new();
        ptr.PointerEntered += (_, _) => sep.Color = hover;
        ptr.PointerExited  += (_, _) => sep.Color = idle;
        sep.GestureRecognizers.Add(ptr);

#if WINDOWS
        sep.HandlerChanged += (_, _) =>
        {
            if (sep.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement uiEl) return;

            var shape = isVertical
                ? Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast
                : Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;
            var cursor = Microsoft.UI.Input.InputSystemCursor.Create(shape);

            // ProtectedCursor is protected; access via reflection so we don't need to subclass.
            System.Reflection.PropertyInfo? prop = typeof(Microsoft.UI.Xaml.UIElement)
                .GetProperty("ProtectedCursor",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(uiEl, cursor);

            // PanGestureRecognizer does not fire for left-click drag on Windows —
            // wire native WinUI pointer events instead.
            bool dragging = false;
            double lastX  = 0;
            double lastY  = 0;

            uiEl.PointerPressed += (_, e) =>
            {
                var pt = e.GetCurrentPoint(null);
                if (pt.Properties.PointerUpdateKind != Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed)
                    return;
                lastX = pt.Position.X;
                lastY = pt.Position.Y;
                uiEl.CapturePointer(e.Pointer);
                dragging = true;
            };

            uiEl.PointerMoved += (_, e) =>
            {
                if (!dragging) return;
                var pt = e.GetCurrentPoint(null);
                double dx = pt.Position.X - lastX;
                double dy = pt.Position.Y - lastY;
                lastX = pt.Position.X;
                lastY = pt.Position.Y;
                if (Math.Abs(isVertical ? dx : dy) < 0.5) return;
                onDrag(dx, dy);
            };

            uiEl.PointerReleased += (_, e) =>
            {
                if (!dragging) return;
                dragging = false;
                uiEl.ReleasePointerCapture(e.Pointer);
                onDragEnd();
            };

            uiEl.PointerCaptureLost += (_, _) => dragging = false;
        };
#endif
    }

    private void OnHierarchySepPanned(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _hierSepPanLast = 0;
                break;
            case GestureStatus.Running:
            {
                double delta = e.TotalX - _hierSepPanLast;
                _hierSepPanLast = e.TotalX;
                double newW = Math.Clamp(BodyGrid.ColumnDefinitions[0].Width.Value + delta, 120, 600);
                BodyGrid.ColumnDefinitions[0].Width = new GridLength(newW);
                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _preferences.LeftPanelWidth = (int)BodyGrid.ColumnDefinitions[0].Width.Value;
                SavePreferences();
                break;
        }
    }

    private void OnInspectorSepPanned(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _inspSepPanLast = 0;
                break;
            case GestureStatus.Running:
            {
                double delta = e.TotalX - _inspSepPanLast;
                _inspSepPanLast = e.TotalX;
                double newW = Math.Clamp(BodyGrid.ColumnDefinitions[4].Width.Value - delta, 120, 600);
                BodyGrid.ColumnDefinitions[4].Width = new GridLength(newW);
                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _preferences.RightPanelWidth = (int)BodyGrid.ColumnDefinitions[4].Width.Value;
                SavePreferences();
                break;
        }
    }

    private void OnDockSepPanned(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _dockSepPanLast = 0;
                break;
            case GestureStatus.Running:
            {
                double delta = e.TotalY - _dockSepPanLast;
                _dockSepPanLast = e.TotalY;
                double newH = Math.Clamp(MainGrid.RowDefinitions[3].Height.Value - delta, 80, 500);
                MainGrid.RowDefinitions[3].Height = new GridLength(newH);
                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _preferences.ConsolePanelHeight = (int)MainGrid.RowDefinitions[3].Height.Value;
                SavePreferences();
                break;
        }
    }

    #endregion
}
