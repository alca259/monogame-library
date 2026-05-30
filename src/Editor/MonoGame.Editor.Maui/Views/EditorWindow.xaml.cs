namespace MonoGame.Editor.Maui.Views;

/// <summary>
/// Ventana principal del editor. Gestiona el toolbar, menú, status bar y coordina el ciclo Play/Stop.
/// Los paneles se comunican exclusivamente a través de <see cref="IEditorEventBus"/>.
/// </summary>
public sealed partial class EditorWindow : ContentPage
{
    #region Fields

    private static readonly Color ActiveToolBg   = Color.FromArgb("#2f81f7");
    private static readonly Color ActiveToolFg   = Colors.White;
    private static readonly Color InactiveToolBg = Colors.Transparent;
    private static readonly Color InactiveToolFg = Color.FromArgb("#d6d6d8");

    private static readonly Color ActivePillBg   = Color.FromArgb("#2f81f7");
    private static readonly Color InactivePillBg = Color.FromArgb("#2d2d30");
    private static readonly Color ActivePillFg   = Colors.White;
    private static readonly Color InactivePillFg = Color.FromArgb("#a7a7ab");
    private static readonly Color PillBorderActive   = Color.FromArgb("#2f81f7");
    private static readonly Color PillBorderInactive = Color.FromArgb("#3a3a3d");

    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private string _activeTool = "Select";
    private bool _is2D   = true;
    private bool _isSnap = false;
    private bool _isNav  = false;
    private bool _isRes  = false;

    private Action<EditorStateChangedEvent>? _onStateChanged;
    private Action<SceneDirtyChangedEvent>?  _onDirtyChanged;
    private Action<ProjectOpenedEvent>?      _onProjectOpened;
    private Action<SceneLoadedEvent>?        _onSceneLoaded;
    private Action<BuildOutputLineEvent>?    _onBuildOutput;

    #endregion

    public EditorWindow()
    {
        InitializeComponent();
        Subscribe();
    }

    #region EventBus subscriptions

    private void Subscribe()
    {
        _onStateChanged  = e => MainThread.BeginInvokeOnMainThread(() => OnEditorStateChanged(e));
        _onDirtyChanged  = e => MainThread.BeginInvokeOnMainThread(() => OnDirtyChanged(e));
        _onProjectOpened = e => MainThread.BeginInvokeOnMainThread(() => OnProjectOpened(e));
        _onSceneLoaded   = e => MainThread.BeginInvokeOnMainThread(() => OnSceneLoaded(e));
        _onBuildOutput   = e => MainThread.BeginInvokeOnMainThread(() => OnBuildOutputLine(e));

        _bus.Subscribe(_onStateChanged);
        _bus.Subscribe(_onDirtyChanged);
        _bus.Subscribe(_onProjectOpened);
        _bus.Subscribe(_onSceneLoaded);
        _bus.Subscribe(_onBuildOutput);
    }

    private void OnEditorStateChanged(EditorStateChangedEvent e)
    {
        bool playing = e.NewState is EditorState.Playing or EditorState.Paused;
        StopBtn.IsEnabled = playing;

        if (playing)
        {
            StopBtn.BackgroundColor = Color.FromArgb("#f0524f");
            StopBtn.TextColor = Colors.White;
        }
        else
        {
            StopBtn.BackgroundColor = Color.FromArgb("#2d2d30");
            StopBtn.TextColor = Color.FromArgb("#86868b");
        }
    }

    private void OnDirtyChanged(SceneDirtyChangedEvent e)
    {
        var ctx = EditorContext.Instance;
        UpdateWindowTitle(ctx.ActiveProject?.Name, ctx.ActiveScene?.Name, e.IsDirty);
    }

    private void OnProjectOpened(ProjectOpenedEvent e)
    {
        UpdateWindowTitle(e.Project?.Name, null, false);
    }

    private void OnSceneLoaded(SceneLoadedEvent e)
    {
        var ctx = EditorContext.Instance;
        UpdateWindowTitle(ctx.ActiveProject?.Name, e.Scene?.Name, false);

        int count = e.Scene?.RootGameObjects.Count ?? 0;
        ObjectCountLabel.Text = count == 1 ? "1 object in scene" : $"{count} objects in scene";
    }

    private void OnBuildOutputLine(BuildOutputLineEvent e)
    {
        string line = e.Line;

        if (line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            BuildStatusLabel.Text = "Build succeeded";
            BuildStatusLabel.TextColor = Color.FromArgb("#3fb950");
            BuildStatusSegment.BackgroundColor = Color.FromArgb("#2a2a2c");
        }
        else if (line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
              || (e.IsError && line.Contains("error", StringComparison.OrdinalIgnoreCase)))
        {
            BuildStatusLabel.Text = "Build failed";
            BuildStatusLabel.TextColor = Colors.White;
            BuildStatusSegment.BackgroundColor = Color.FromArgb("#f0524f");
        }
    }

    private void UpdateWindowTitle(string? projectName, string? sceneName, bool dirty)
    {
        string title = "MonoGame Editor";
        if (projectName is not null)
            title = $"MonoGame Editor — {projectName}";
        if (sceneName is not null)
            title = $"{title} — {sceneName}";
        if (dirty)
            title = $"● {title}";

        if (Window is not null)
            Window.Title = title;
    }

    #endregion

    #region Menu bar

    private void OnFileMenuClicked(object sender, EventArgs e)
    {
        // TODO Fase 9: abrir menú contextual File
    }

    private void OnEditMenuClicked(object sender, EventArgs e)
    {
        // TODO Fase 9: abrir menú contextual Edit
    }

    private void OnProjectMenuClicked(object sender, EventArgs e)
    {
        // TODO Fase 9: abrir menú contextual Project
    }

    private void OnDebugMenuClicked(object sender, EventArgs e)
    {
        // TODO Fase 9: abrir menú contextual Debug
    }

    #endregion

    #region Toolbar — gizmo tools

    private void OnToolClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        _activeTool = btn.CommandParameter as string ?? "Select";
        UpdateToolButtons();
        // TODO Fase 2: EditorContext.Instance.Gizmos.SetMode(...)
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
        // TODO Fase 2: actualizar modo cámara en viewport
    }

    private void OnToggleSnap(object sender, EventArgs e)
    {
        _isSnap = !_isSnap;
        SetPillStyle(ToggleSnapBtn, _isSnap);
        // TODO Fase 2: actualizar snap en EditModeRenderer
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

    #region Toolbar — transport

    private void OnPlayClicked(object sender, EventArgs e)
    {
        // TODO Fase 10: PlayModeManager.EnterPlay()
    }

    private void OnStopClicked(object sender, EventArgs e)
    {
        // TODO Fase 10: PlayModeManager.ExitPlay()
    }

    #endregion
}
