using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Forms;

/// <summary>
/// Ventana principal del editor: shell con menú, toolbar, paneles placeholder y status bar.
/// La lógica de presentación vive en <see cref="EditorWindowViewModel"/>.
/// </summary>
internal sealed partial class MainForm : Form
{
    private readonly EditorWindowViewModel _vm;

    public MainForm()
    {
        InitializeComponent();
        _vm = new EditorWindowViewModel();
        _vm.Attach();
        ApplyTheme();
        BuildMenus();
        BindVm();
        WireDialogHooks();
    }

    #region Tema

    private void ApplyTheme()
    {
        BackColor = EditorColors.ShellBackground;
        ForeColor = EditorColors.TextPrimary;
        Font      = EditorFonts.Primary;

        _splitMain.BackColor        = EditorColors.Border;
        _splitCenterRight.BackColor = EditorColors.Border;
        _splitViewportDock.BackColor = EditorColors.Border;
    }

    #endregion

    #region Ciclo de vida

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        AdjustSplitters();
        await _vm.TryAutoLoadLastProjectAsync().ConfigureAwait(true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AdjustSplitters();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm.Detach();
        base.OnFormClosed(e);
    }

    /// <summary>Recalcula SplitterDistance del SplitContainer central para mantener el
    /// inspector a ancho fijo cuando la ventana cambia de tamaño.</summary>
    private void AdjustSplitters()
    {
        int centerWidth = _splitCenterRight.Width;
        if (centerWidth > EditorStyles.InspectorWidth + _splitCenterRight.Panel1MinSize)
        {
            int dist = centerWidth - EditorStyles.InspectorWidth - _splitCenterRight.SplitterWidth;
            if (dist > _splitCenterRight.Panel1MinSize)
                _splitCenterRight.SplitterDistance = dist;
        }

        int viewportDockHeight = _splitViewportDock.Height;
        if (viewportDockHeight > EditorStyles.DockBarHeight + _splitViewportDock.Panel1MinSize)
        {
            int dist = viewportDockHeight - EditorStyles.DockBarHeight - _splitViewportDock.SplitterWidth;
            if (dist > _splitViewportDock.Panel1MinSize)
                _splitViewportDock.SplitterDistance = dist;
        }
    }

    #endregion

    #region Binding al ViewModel

    private void BindVm()
    {
        _vm.PropertyChanged             += OnVmPropertyChanged;
        _vm.ViewportInvalidateRequested += () => _viewport.Invalidate();

        // Toolbar → comandos
        _tsiToolSelect.Click  += (_, _) => _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Select);
        _tsiToolMove.Click    += (_, _) => _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Move);
        _tsiToolRotate.Click  += (_, _) => _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Rotate);
        _tsiToolScale.Click   += (_, _) => _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Scale);
        _tsiToolRect.Click    += (_, _) => _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Rect);
        _tsiSnap.Click        += (_, _) => _vm.ToggleSnapCommand.Execute(null);
        _tsiEnableMove.Click  += (_, _) => _vm.ToggleToolMoveCommand.Execute(null);
        _tsiEnableRotate.Click += (_, _) => _vm.ToggleToolRotateCommand.Execute(null);
        _tsiEnableScale.Click += (_, _) => _vm.ToggleToolScaleCommand.Execute(null);
        _tsiAxisX.Click       += (_, _) => _vm.ToggleAxisXCommand.Execute(null);
        _tsiAxisY.Click       += (_, _) => _vm.ToggleAxisYCommand.Execute(null);
        _tsiNav.Click         += (_, _) => _vm.ToggleNavCommand.Execute(null);
        _tsiRes.Click         += (_, _) => _vm.ToggleResCommand.Execute(null);
        _tsiBtnPlay.Click     += (_, _) => _vm.PlayCommand.Execute(null);
        _tsiBtnStop.Click     += (_, _) => _vm.StopCommand.Execute(null);

        // Estado inicial
        Text = _vm.Title;
        RefreshToolbarState();
        RefreshStatusBar();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(() => OnVmPropertyChanged(sender, e)); return; }

        switch (e.PropertyName)
        {
            case nameof(EditorWindowViewModel.Title):
                Text = _vm.Title;
                break;

            case nameof(EditorWindowViewModel.ActiveTool):
            case nameof(EditorWindowViewModel.IsSnap):
            case nameof(EditorWindowViewModel.ToolMoveEnabled):
            case nameof(EditorWindowViewModel.ToolRotateEnabled):
            case nameof(EditorWindowViewModel.ToolScaleEnabled):
            case nameof(EditorWindowViewModel.AxisXEnabled):
            case nameof(EditorWindowViewModel.AxisYEnabled):
            case nameof(EditorWindowViewModel.IsNav):
            case nameof(EditorWindowViewModel.IsRes):
            case nameof(EditorWindowViewModel.CanPlay):
            case nameof(EditorWindowViewModel.CanStop):
            case nameof(EditorWindowViewModel.StopActive):
                RefreshToolbarState();
                break;

            case nameof(EditorWindowViewModel.BuildStatusText):
            case nameof(EditorWindowViewModel.BuildStatusColor):
            case nameof(EditorWindowViewModel.ObjectCountText):
            case nameof(EditorWindowViewModel.FpsText):
                RefreshStatusBar();
                break;
        }
    }

    private void RefreshToolbarState()
    {
        _tsiToolSelect.IsToggled  = _vm.ActiveTool == EditorWindowViewModel.SceneTools.Select;
        _tsiToolMove.IsToggled    = _vm.ActiveTool == EditorWindowViewModel.SceneTools.Move;
        _tsiToolRotate.IsToggled  = _vm.ActiveTool == EditorWindowViewModel.SceneTools.Rotate;
        _tsiToolScale.IsToggled   = _vm.ActiveTool == EditorWindowViewModel.SceneTools.Scale;
        _tsiToolRect.IsToggled    = _vm.ActiveTool == EditorWindowViewModel.SceneTools.Rect;

        _tsiSnap.IsToggled         = _vm.IsSnap;
        _tsiEnableMove.IsToggled   = _vm.ToolMoveEnabled;
        _tsiEnableRotate.IsToggled = _vm.ToolRotateEnabled;
        _tsiEnableScale.IsToggled  = _vm.ToolScaleEnabled;
        _tsiAxisX.IsToggled        = _vm.AxisXEnabled;
        _tsiAxisY.IsToggled        = _vm.AxisYEnabled;
        _tsiNav.IsToggled          = _vm.IsNav;
        _tsiRes.IsToggled          = _vm.IsRes;

        _tsiBtnPlay.Enabled   = _vm.CanPlay;
        _tsiBtnStop.Enabled   = _vm.CanStop;
        _tsiBtnStop.BackColor = _vm.StopActive ? EditorColors.StopRed : EditorColors.BgChrome;
        _tsiBtnStop.ForeColor = _vm.StopActive ? Color.White : EditorColors.TextMuted;
    }

    private void RefreshStatusBar()
    {
        _sslStatus.Text       = _vm.BuildStatusText;
        _sslStatus.ForeColor  = _vm.BuildStatusColor;
        _sslObjectCount.Text  = _vm.ObjectCountText;
        _sslFps.Text          = _vm.FpsText;
        _sslFps.ForeColor     = _vm.CanStop ? EditorColors.PlayGreen : EditorColors.TextMuted;
    }

    #endregion

    #region Hooks de diálogos

    private void WireDialogHooks()
    {
        _vm.RequestNewProjectDialog = () =>
            Task.FromResult(NewProjectForm.Show(this));

        _vm.RequestNewSceneDialog = () =>
            Task.FromResult(NewSceneForm.Show(this));

        _vm.RequestProjectSettingsDialog = async () =>
        {
            EditorProject? project = EditorContext.Instance.ActiveProject;
            if (project is not null)
                await ProjectSettingsForm.ShowAsync(this, project).ConfigureAwait(true);
        };

        _vm.OpenCodeGenProgressDialog = () =>
        {
            CodeGenProgressForm form = new();
            form.Show(this);
            return new CodeGenProgressCallbacks(
                (path, success) =>
                {
                    if (!form.IsDisposed) form.AddFileResult(path, success);
                },
                (ok, fail) =>
                {
                    if (!form.IsDisposed) form.MarkComplete(ok, fail);
                });
        };
    }

    #endregion

    #region Panel visibility (usado desde Menus.cs)

    private bool _hierarchyVisible = true;
    private bool _inspectorVisible = true;
    private bool _dockVisible      = true;

    internal void ToggleHierarchy()
    {
        _hierarchyVisible = !_hierarchyVisible;
        _splitMain.Panel1Collapsed = !_hierarchyVisible;
    }

    internal void ToggleInspector()
    {
        _inspectorVisible = !_inspectorVisible;
        _splitCenterRight.Panel2Collapsed = !_inspectorVisible;
    }

    internal void ToggleDock()
    {
        _dockVisible = !_dockVisible;
        _splitViewportDock.Panel2Collapsed = !_dockVisible;
    }

    #endregion
}
