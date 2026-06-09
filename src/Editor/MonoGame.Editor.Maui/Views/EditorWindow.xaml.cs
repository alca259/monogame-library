namespace MonoGame.Editor.Maui.Views;

/// <summary>
/// Ventana principal del editor. La lógica de negocio, el estado del toolbar/status bar y los
/// comandos viven en <see cref="EditorWindowViewModel"/>. El code-behind conserva la interop
/// nativa WinUI (teclado, rueda, arrastre de separadores), la entrada del viewport, la
/// construcción dinámica de menús desplegables y la gestión de layout de paneles.
/// Los paneles se comunican exclusivamente a través de <see cref="IEditorEventBus"/>.
/// </summary>
public sealed partial class EditorWindow : ContentPage
{
    #region Dropdown colors

    private static readonly Color DropdownItemFg        = Color.FromArgb("#E6E6E8");
    private static readonly Color DropdownItemBg        = Colors.Transparent;
    private static readonly Color DropdownItemHoverBg   = Color.FromArgb("#2E2E34");
    private static readonly Color DropdownSeparatorColor = Color.FromArgb("#34343A");

    #endregion

    #region Dropdown item model

    private sealed record DropdownItem(
        string Label,
        bool IsSeparator = false,
        Action? Action = null,
        IReadOnlyList<DropdownItem>? Children = null)
    {
        public bool HasChildren => Children is { Count: > 0 };
    }

    #endregion

    #region Fields

    private readonly EditorWindowViewModel _vm = new();
    private readonly ViewportRenderer _viewportRenderer = new();

    private bool _hierarchyVisible = true;
    private bool _inspectorVisible = true;
    private bool _dockVisible      = true;
    private const double HierarchyWidth = 268;
    private const double InspectorWidth = 362;
    private const double DockHeight     = 266;

    private string? _openMenuTag;

    private double  _panLastX;
    private double  _panLastY;
    private float   _lastPointerScreenX;
    private float   _lastPointerScreenY;
    private float   _panStartScreenX;
    private float   _panStartScreenY;
    private bool    _gizmoDragging;

    private bool   _pointerOverViewport;
    private bool   _nativeViewportPanActive;
    private double _nativeViewportPanLastX;
    private double _nativeViewportPanLastY;
    private double _hierPanelWidth  = HierarchyWidth;
    private double _inspPanelWidth  = InspectorWidth;
    private double _dockPanelHeight = DockHeight;

    private Action<double, double>? _activeSepOnDrag;
    private Action?                 _activeSepOnDragEnd;
    private double                  _sepDragLastX;
    private double                  _sepDragLastY;

    // Populated by AddSeparatorDrag; used for hit-testing at window root level.
    private readonly List<(BoxView Sep, Action<double, double> OnDrag, Action OnDragEnd)> _sepEntries = [];

    private bool _autoLoadStarted;

    #endregion

    public EditorWindow()
    {
        InitializeComponent();
        BindingContext = _vm;
        Viewport.Drawable = _viewportRenderer;
        _vm.ViewportInvalidateRequested += OnViewportInvalidateRequested;
        _vm.Preferences.Load();
        ApplyPreferences();
    }

    private void OnViewportInvalidateRequested()
        => MainThread.BeginInvokeOnMainThread(() => Viewport.Invalidate());

    #region Lifecycle — keyboard shortcuts & native hooks

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is not null)
        {
            _vm.Attach();

            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win?.Content is not null)
            {
                win.Content.AddHandler(
                    Microsoft.UI.Xaml.UIElement.KeyDownEvent,
                    new Microsoft.UI.Xaml.Input.KeyEventHandler(OnNativeKeyDown),
                    handledEventsToo: false);
                win.Content.AddHandler(
                    Microsoft.UI.Xaml.UIElement.PointerPressedEvent,
                    new Microsoft.UI.Xaml.Input.PointerEventHandler(OnNativeSepDragStarted),
                    handledEventsToo: true);
                win.Content.PointerWheelChanged += OnNativePointerWheelChanged;
                win.Content.PointerMoved        += OnNativeSepDragMoved;
                win.Content.PointerReleased     += OnNativeSepDragReleased;
                win.Closed += (_, _) => SavePreferences();
            }

            AttachSeparatorCursors();

            if (!_autoLoadStarted)
            {
                _autoLoadStarted = true;
                _ = _vm.TryAutoLoadLastProjectAsync();
            }
        }
        else
        {
            _vm.Detach();
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
        bool alt = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        EditorFocusContext focus = EditorContext.Instance.ActiveFocus;

        switch (e.Key)
        {
            // Global shortcuts — always active
            case Windows.System.VirtualKey.Z when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.UndoCommand.Execute(null));
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Y when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.RedoCommand.Execute(null));
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.B when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.BuildSolutionAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.F5 when ctrl:
                MainThread.BeginInvokeOnMainThread(_vm.Play);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.G when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.GenerateCodeAsync());
                e.Handled = true;
                return;
        }

        // Menu mnemonics (Alt+letter) — only when no specific panel holds focus.
        if (alt && focus is EditorFocusContext.Global)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.F:
                    MainThread.BeginInvokeOnMainThread(() => OnFileMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.E:
                    MainThread.BeginInvokeOnMainThread(() => OnEditMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.P:
                    MainThread.BeginInvokeOnMainThread(() => OnProjectMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.D:
                    MainThread.BeginInvokeOnMainThread(() => OnDebugMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.V:
                    MainThread.BeginInvokeOnMainThread(() => OnViewMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
            }
        }

        // Viewport shortcuts — only when the viewport holds focus and no text input is focused.
        if (textFocused || focus is not EditorFocusContext.Viewport) return;

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Q:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Select"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.W:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Move"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.E:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Rotate"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.R:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Scale"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.T:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Rect"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.H:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Pan"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.G:
                MainThread.BeginInvokeOnMainThread(_vm.ToggleSnap);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Delete:
                MainThread.BeginInvokeOnMainThread(_vm.DeleteSelected);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F:
                MainThread.BeginInvokeOnMainThread(FocusOnSelected);
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

    #region Preferences & layout

    private void ApplyPreferences()
    {
        EditorPreferences prefs = _vm.Preferences;

        _hierarchyVisible = prefs.HierarchyVisible;
        _inspectorVisible = prefs.InspectorVisible;
        _dockVisible      = prefs.AssetBrowserVisible;

        _hierPanelWidth  = prefs.LeftPanelWidth;
        _inspPanelWidth  = prefs.RightPanelWidth;
        _dockPanelHeight = prefs.ConsolePanelHeight;

        BodyGrid.ColumnDefinitions[0].Width = new GridLength(_hierarchyVisible ? _hierPanelWidth  : 0);
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(_inspectorVisible ? _inspPanelWidth  : 0);
        MainGrid.RowDefinitions[3].Height   = new GridLength(_dockVisible      ? _dockPanelHeight : 0);
        HierarchySep.IsVisible = _hierarchyVisible;
        InspectorSep.IsVisible = _inspectorVisible;
        DockRow.IsVisible      = _dockVisible;

        _viewportRenderer.GridCellSize             = prefs.GridCellSize;
        EditorContext.Instance.Gizmos.GridCellSize        = prefs.GridCellSize;
        EditorContext.Instance.Gizmos.SnapRotationDegrees = prefs.SnapRotationDegrees;
        EditorContext.Instance.Gizmos.SnapScaleStep       = prefs.SnapScaleStep;
    }

    private void SavePreferences()
    {
        EditorPreferences prefs = _vm.Preferences;

        prefs.HierarchyVisible    = _hierarchyVisible;
        prefs.InspectorVisible    = _inspectorVisible;
        prefs.AssetBrowserVisible = _dockVisible;
        prefs.LeftPanelWidth      = (int)_hierPanelWidth;
        prefs.RightPanelWidth     = (int)_inspPanelWidth;
        prefs.ConsolePanelHeight  = (int)_dockPanelHeight;
        prefs.GridCellSize        = _viewportRenderer.GridCellSize;
        prefs.SnapRotationDegrees = EditorContext.Instance.Gizmos.SnapRotationDegrees;
        prefs.SnapScaleStep       = EditorContext.Instance.Gizmos.SnapScaleStep;
        prefs.Save();
    }

    private void ToggleHierarchy()
    {
        _hierarchyVisible = !_hierarchyVisible;
        double w = _hierarchyVisible ? Math.Max(_hierPanelWidth, 120) : 0;
        BodyGrid.ColumnDefinitions[0].Width = new GridLength(w);
        HierarchySep.IsVisible = _hierarchyVisible;
        SavePreferences();
    }

    private void ToggleInspector()
    {
        _inspectorVisible = !_inspectorVisible;
        double w = _inspectorVisible ? Math.Max(_inspPanelWidth, 120) : 0;
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(w);
        InspectorSep.IsVisible = _inspectorVisible;
        SavePreferences();
    }

    private void ToggleDock()
    {
        _dockVisible = !_dockVisible;
        double h = _dockVisible ? Math.Max(_dockPanelHeight, 80) : 0;
        MainGrid.RowDefinitions[3].Height = new GridLength(h);
        DockRow.IsVisible = _dockVisible;
        SavePreferences();
    }

    private void ResetLayout()
    {
        _hierarchyVisible = true;
        _inspectorVisible = true;
        _dockVisible      = true;
        _hierPanelWidth   = HierarchyWidth;
        _inspPanelWidth   = InspectorWidth;
        _dockPanelHeight  = DockHeight;
        BodyGrid.ColumnDefinitions[0].Width = new GridLength(HierarchyWidth);
        BodyGrid.ColumnDefinitions[4].Width = new GridLength(InspectorWidth);
        MainGrid.RowDefinitions[3].Height   = new GridLength(DockHeight);
        HierarchySep.IsVisible = true;
        InspectorSep.IsVisible = true;
        DockRow.IsVisible      = true;
        SavePreferences();
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

    private const double DropdownRowHeight = 32;
    private const double DropdownSepHeight = 5;   // HeightRequest=1 + Margin top/bottom=2+2
    private const double DropdownTopMargin = 28;

    private void ShowDropdown(string tag, int offsetX, IEnumerable<DropdownItem> items)
    {
        EditorContext.Instance.SetFocus(EditorFocusContext.Global);
        _openMenuTag = tag;
        DropdownStack.Children.Clear();
        HideSubDropdown();

        double cumulativeY = DropdownTopMargin;

        foreach (DropdownItem item in items)
        {
            if (item.IsSeparator)
            {
                cumulativeY += DropdownSepHeight;
                DropdownStack.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color         = DropdownSeparatorColor,
                    Margin        = new Thickness(8, 2),
                });
                continue;
            }

            bool isDisabled = item.Action is null && !item.HasChildren;
            double rowY = cumulativeY;
            cumulativeY += DropdownRowHeight;

            var row = new Grid
            {
                BackgroundColor      = DropdownItemBg,
                Padding              = new Thickness(16, 6),
                MinimumHeightRequest = DropdownRowHeight,
            };

            if (item.HasChildren)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(24, GridUnitType.Absolute)));
            }

            row.Add(new Label
            {
                Text                    = item.Label,
                TextColor               = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize                = 13,
                VerticalTextAlignment   = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions         = LayoutOptions.Fill,
            }, 0, 0);

            if (item.HasChildren)
            {
                row.Add(new Label
                {
                    Text                    = "›",
                    TextColor               = Color.FromArgb("#9A9AA2"),
                    FontSize                = 14,
                    VerticalTextAlignment   = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions         = LayoutOptions.Fill,
                }, 1, 0);

                var capturedChildren = item.Children!;
                double capturedY = rowY;
                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) =>
                {
                    row.BackgroundColor = DropdownItemHoverBg;
                    ShowSubDropdown(capturedChildren, offsetX + DropdownPanel.Width, capturedY);
                };
                row.GestureRecognizers.Add(pointer);
            }
            else
            {
                if (!isDisabled)
                {
                    var captured = item.Action!;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += (_, _) => { HideDropdown(); captured(); };
                    row.GestureRecognizers.Add(tap);
                }

                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) => { row.BackgroundColor = DropdownItemHoverBg; HideSubDropdown(); };
                pointer.PointerExited  += (_, _) => row.BackgroundColor = DropdownItemBg;
                row.GestureRecognizers.Add(pointer);
            }

            DropdownStack.Children.Add(row);
        }

        DropdownPanel.Margin = new Thickness(offsetX, DropdownTopMargin, 0, 0);
        MenuOverlay.IsVisible = true;
    }

    private void ShowSubDropdown(IReadOnlyList<DropdownItem> items, double x, double y)
    {
        SubDropdownStack.Children.Clear();

        foreach (DropdownItem item in items)
        {
            bool isDisabled = item.Action is null;

            var row = new Grid
            {
                BackgroundColor      = DropdownItemBg,
                Padding              = new Thickness(16, 6),
                MinimumHeightRequest = 32,
            };

            row.Add(new Label
            {
                Text                    = item.Label,
                TextColor               = isDisabled ? Color.FromArgb("#6A6A72") : DropdownItemFg,
                FontSize                = 13,
                VerticalTextAlignment   = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalOptions         = LayoutOptions.Fill,
            });

            if (!isDisabled)
            {
                var captured = item.Action!;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => { HideDropdown(); captured(); };
                row.GestureRecognizers.Add(tap);

                var pointer = new PointerGestureRecognizer();
                pointer.PointerEntered += (_, _) => row.BackgroundColor = DropdownItemHoverBg;
                pointer.PointerExited  += (_, _) => row.BackgroundColor = DropdownItemBg;
                row.GestureRecognizers.Add(pointer);
            }

            SubDropdownStack.Children.Add(row);
        }

        SubDropdownPanel.Margin = new Thickness(x, y, 0, 0);
        SubDropdownPanel.IsVisible = true;
    }

    private void HideSubDropdown()
    {
        SubDropdownStack.Children.Clear();
        SubDropdownPanel.IsVisible = false;
    }

    private void HideDropdown()
    {
        HideSubDropdown();
        MenuOverlay.IsVisible = false;
        _openMenuTag = null;
    }

    #endregion

    #region Menu item builders

    private IEnumerable<DropdownItem> BuildFileMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return new DropdownItem("New Project…",  Action: () => _vm.NewProjectCommand.Execute(null));
        yield return new DropdownItem("Open Project…", Action: () => _vm.OpenProjectCommand.Execute(null));

        if (_vm.Preferences.RecentProjects.Count > 0)
        {
            List<DropdownItem> recentItems = [];
            foreach (string path in _vm.Preferences.RecentProjects)
            {
                string captured = path;
                string label    = Path.GetFileName(captured.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                recentItems.Add(new DropdownItem(label, Action: () => _ = _vm.OpenProjectByPathAsync(captured)));
            }
            yield return new DropdownItem("Recent Projects", Children: recentItems);
        }

        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("Close Project",  Action: hasProject ? () => _vm.CloseProjectCommand.Execute(null) : null);
        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("New Scene",      Action: hasProject ? () => _vm.NewSceneCommand.Execute(null)    : null);
        yield return new DropdownItem("Save Scene",     Action: hasScene   ? () => _ = _vm.SaveSceneAsync()             : null);
        yield return new DropdownItem("Save Scene As…", Action: hasScene   ? () => _ = _vm.SaveSceneAsAsync()           : null);
        yield return new DropdownItem("---",            IsSeparator: true);
        yield return new DropdownItem("Exit",           Action: () => _vm.ExitCommand.Execute(null));
    }

    private IEnumerable<DropdownItem> BuildEditMenuItems()
    {
        bool hasScene     = EditorContext.Instance.ActiveScene is not null;
        bool hasSelection = EditorContext.Instance.SelectedObject is not null;
        bool hasClipboard = EditorContext.Instance.ClipboardEntity is not null;

        yield return new DropdownItem("Undo",       Action: hasScene                 ? () => _vm.UndoCommand.Execute(null)              : null);
        yield return new DropdownItem("Redo",       Action: hasScene                 ? () => _vm.RedoCommand.Execute(null)              : null);
        yield return new DropdownItem("---",        IsSeparator: true);
        yield return new DropdownItem("Cut",        Action: hasSelection             ? () => _vm.CutCommand.Execute(null)               : null);
        yield return new DropdownItem("Copy",       Action: hasSelection             ? () => _vm.CopyCommand.Execute(null)              : null);
        yield return new DropdownItem("Paste",      Action: hasScene && hasClipboard ? () => _vm.PasteCommand.Execute(null)             : null);
        yield return new DropdownItem("Duplicate",  Action: hasSelection             ? () => _vm.DuplicateSelectedCommand.Execute(null) : null);
        yield return new DropdownItem("Delete",     Action: hasSelection             ? () => _vm.DeleteSelectedCommand.Execute(null)    : null);
        yield return new DropdownItem("---",        IsSeparator: true);
        yield return new DropdownItem("Select All", Action: hasScene                 ? () => _vm.SelectAllCommand.Execute(null)         : null);
    }

    private IEnumerable<DropdownItem> BuildProjectMenuItems()
    {
        bool hasProject = EditorContext.Instance.ActiveProject is not null;
        bool hasScene   = EditorContext.Instance.ActiveScene is not null;

        yield return new DropdownItem("Project Settings…", Action: hasProject             ? () => _vm.OpenProjectSettingsCommand.Execute(null) : null);
        yield return new DropdownItem("---",               IsSeparator: true);
        yield return new DropdownItem("Build Content",     Action: hasProject             ? () => _ = _vm.BuildContentAsync()                  : null);
        yield return new DropdownItem("Build Solution",    Action: hasProject             ? () => _ = _vm.BuildSolutionAsync()                 : null);
        yield return new DropdownItem("Generate Code",     Action: hasProject && hasScene ? () => _ = _vm.GenerateCodeAsync()                  : null);
        yield return new DropdownItem("---",               IsSeparator: true);
        yield return new DropdownItem("Run",               Action: hasProject             ? () => _vm.RunGameCommand.Execute(null)             : null);
    }

    private IEnumerable<DropdownItem> BuildDebugMenuItems()
    {
        bool hasScene  = EditorContext.Instance.ActiveScene is not null;
        bool isPlaying = EditorContext.Instance.State is EditorState.Playing;

        yield return new DropdownItem("Play", Action: hasScene && !isPlaying ? _vm.Play : null);
        yield return new DropdownItem("Stop", Action: isPlaying              ? _vm.Stop : null);
    }

    private IEnumerable<DropdownItem> BuildViewMenuItems()
    {
        string hPfx = _hierarchyVisible ? "✓ " : "  ";
        string iPfx = _inspectorVisible ? "✓ " : "  ";
        string dPfx = _dockVisible      ? "✓ " : "  ";

        yield return new DropdownItem($"{hPfx}Hierarchy",   Action: ToggleHierarchy);
        yield return new DropdownItem($"{iPfx}Inspector",   Action: ToggleInspector);
        yield return new DropdownItem($"{dPfx}Bottom Dock", Action: ToggleDock);
        yield return new DropdownItem("---",                IsSeparator: true);
        yield return new DropdownItem("Reset Layout",       Action: ResetLayout);
    }

    #endregion

    #region Viewport — input

    private void FocusOnSelected()
    {
        EditorGameObject? sel = EditorContext.Instance.SelectedObject;
        if (sel is null) return;
        Microsoft.Maui.Graphics.PointF pos = _viewportRenderer.Orientation switch
        {
            ViewOrientation.Top   => new Microsoft.Maui.Graphics.PointF(sel.Position.X, sel.PositionZ),
            ViewOrientation.Right => new Microsoft.Maui.Graphics.PointF(sel.PositionZ,  sel.Position.Y),
            _                     => new Microsoft.Maui.Graphics.PointF(sel.Position.X, sel.Position.Y),
        };
        _viewportRenderer.Camera.Position = pos;
        Viewport.Invalidate();
    }

    private void OnViewportTapped(object sender, TappedEventArgs e)
    {
        EditorContext.Instance.SetFocus(EditorFocusContext.Viewport);

        Point? tapPos = e.GetPosition(Viewport);
        if (tapPos is null) return;

        // Orientation gizmo click — consumes the event before any selection logic
        Microsoft.Maui.Graphics.PointF tapPtF = new((float)tapPos.Value.X, (float)tapPos.Value.Y);
        Microsoft.Maui.Graphics.RectF  vpRect  = new(0, 0, (float)Viewport.Width, (float)Viewport.Height);
        ViewOrientation? newOrientation = _viewportRenderer.OrientationGizmoHitTest(tapPtF, vpRect);
        if (newOrientation.HasValue)
        {
            _viewportRenderer.Orientation = newOrientation.Value;
            EditorContext.Instance.Gizmos.Orientation = newOrientation.Value;
            Viewport.Invalidate();
            return;
        }

        if (EditorContext.Instance.Gizmos.Mode != GizmoMode.Select) return;

        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;

        Microsoft.Maui.Graphics.SizeF viewSize = new((float)Viewport.Width, (float)Viewport.Height);
        Microsoft.Maui.Graphics.PointF worldPos = _viewportRenderer.Camera.ScreenToWorld(tapPtF, viewSize);

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
                if (sel is not null && _vm.ActiveTool is not "Select" and not "Pan")
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
                else if (_vm.ActiveTool == "Pan")
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
                            EditorContext.Instance.EventBus.Publish(new GameObjectSelectedEvent(dragSel));
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

    #region Panel resizing (native pointer interop)

    private void AttachSeparatorCursors()
    {
        AddSeparatorDrag(
            HierarchySep,
            isVertical: true,
            onDrag: (dx, _) =>
            {
                _hierPanelWidth = Math.Clamp(_hierPanelWidth + dx, 120, 600);
                BodyGrid.ColumnDefinitions[0].Width = new GridLength(_hierPanelWidth);
                BodyGrid.InvalidateMeasure();
            },
            onDragEnd: () =>
            {
                _vm.Preferences.LeftPanelWidth = (int)_hierPanelWidth;
                SavePreferences();
            });

        AddSeparatorDrag(
            InspectorSep,
            isVertical: true,
            onDrag: (dx, _) =>
            {
                _inspPanelWidth = Math.Clamp(_inspPanelWidth - dx, 120, 600);
                BodyGrid.ColumnDefinitions[4].Width = new GridLength(_inspPanelWidth);
                BodyGrid.InvalidateMeasure();
            },
            onDragEnd: () =>
            {
                _vm.Preferences.RightPanelWidth = (int)_inspPanelWidth;
                SavePreferences();
            });

        AddSeparatorDrag(
            DockSep,
            isVertical: false,
            onDrag: (_, dy) =>
            {
                _dockPanelHeight = Math.Clamp(_dockPanelHeight - dy, 80, 500);
                MainGrid.RowDefinitions[3].Height = new GridLength(_dockPanelHeight);
                MainGrid.InvalidateMeasure();
            },
            onDragEnd: () =>
            {
                _vm.Preferences.ConsolePanelHeight = (int)_dockPanelHeight;
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

        // Store so the window-level PointerPressed handler can hit-test against it.
        _sepEntries.Add((sep, onDrag, onDragEnd));

#if WINDOWS
        // HandlerChanged on a child BoxView often fires BEFORE the parent's OnHandlerChanged
        // subscribes, so we call SetupNative() immediately as well as on future changes.
        Microsoft.UI.Xaml.UIElement? lastSetupEl = null;

        void SetupNative()
        {
            if (sep.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement uiEl) return;
            if (ReferenceEquals(uiEl, lastSetupEl)) return;
            lastSetupEl = uiEl;

            var shape = isVertical
                ? Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast
                : Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth;
            typeof(Microsoft.UI.Xaml.UIElement)
                .GetProperty("ProtectedCursor",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(uiEl, Microsoft.UI.Input.InputSystemCursor.Create(shape));
        }

        sep.HandlerChanged += (_, _) => SetupNative();
        SetupNative();
#endif
    }

    private void OnNativeSepDragStarted(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(null);

        // Viewport pan con botón central o derecho
        var kind = pt.Properties.PointerUpdateKind;
        if (_pointerOverViewport &&
            (kind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonPressed ||
             kind == Microsoft.UI.Input.PointerUpdateKind.RightButtonPressed))
        {
            EditorContext.Instance.SetFocus(EditorFocusContext.Viewport);
            _nativeViewportPanActive = true;
            _nativeViewportPanLastX  = pt.Position.X;
            _nativeViewportPanLastY  = pt.Position.Y;
            e.Handled = true;
            return;
        }

        if (pt.Properties.PointerUpdateKind
            != Microsoft.UI.Input.PointerUpdateKind.LeftButtonPressed) return;

        var source = e.OriginalSource as Microsoft.UI.Xaml.DependencyObject;

        foreach (var (sep, onDrag, onDragEnd) in _sepEntries)
        {
            var sepEl = sep.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (sepEl is null) continue;

            var current = source;
            while (current is not null)
            {
                if (ReferenceEquals(current, sepEl))
                {
                    _activeSepOnDrag    = onDrag;
                    _activeSepOnDragEnd = onDragEnd;
                    _sepDragLastX       = pt.Position.X;
                    _sepDragLastY       = pt.Position.Y;
                    return;
                }
                current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            }
        }
    }

    private void OnNativeSepDragMoved(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_nativeViewportPanActive)
        {
            var npt = e.GetCurrentPoint(null);
            if (!npt.Properties.IsMiddleButtonPressed && !npt.Properties.IsRightButtonPressed)
            {
                _nativeViewportPanActive = false;
                return;
            }
            double panDx = npt.Position.X - _nativeViewportPanLastX;
            double panDy = npt.Position.Y - _nativeViewportPanLastY;
            _nativeViewportPanLastX = npt.Position.X;
            _nativeViewportPanLastY = npt.Position.Y;
            if (Math.Abs(panDx) >= 0.5 || Math.Abs(panDy) >= 0.5)
            {
                float zoom = _viewportRenderer.Camera.Zoom;
                Dispatcher.Dispatch(() =>
                {
                    _viewportRenderer.Camera.Pan(
                        new Microsoft.Maui.Graphics.PointF((float)(-panDx / zoom), (float)(-panDy / zoom)));
                    Viewport.Invalidate();
                });
            }
            return;
        }

        if (_activeSepOnDrag is null) return;

        var pt = e.GetCurrentPoint(null);
        if (!pt.Properties.IsLeftButtonPressed)
        {
            var endAction = _activeSepOnDragEnd;
            _activeSepOnDrag    = null;
            _activeSepOnDragEnd = null;
            Dispatcher.Dispatch(() => endAction?.Invoke());
            return;
        }

        double dx = pt.Position.X - _sepDragLastX;
        double dy = pt.Position.Y - _sepDragLastY;
        _sepDragLastX = pt.Position.X;
        _sepDragLastY = pt.Position.Y;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5) return;

        var action     = _activeSepOnDrag;
        var capturedDx = dx;
        var capturedDy = dy;
        Dispatcher.Dispatch(() => action(capturedDx, capturedDy));
    }

    private void OnNativeSepDragReleased(object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_nativeViewportPanActive)
        {
            _nativeViewportPanActive = false;
            return;
        }

        if (_activeSepOnDrag is null) return;
        var endAction   = _activeSepOnDragEnd;
        _activeSepOnDrag    = null;
        _activeSepOnDragEnd = null;
        Dispatcher.Dispatch(() => endAction?.Invoke());
    }

    #endregion
}
