namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel izquierdo: árbol de jerarquía de entidades. La lógica vive en
/// <see cref="SceneHierarchyViewModel"/>; el code-behind enlaza la VM y gestiona su
/// ciclo de vida (Attach/Detach). El drag &amp; drop de reparenting usa eventos nativos
/// de WinUI a nivel de ventana (no a nivel de ListView) porque WinUI3's ListView
/// captura internamente el pointer para su selección, impidiendo que los eventos
/// lleguen al handler del control hijo.
/// </summary>
public sealed partial class SceneHierarchyView : ContentView
{
    private readonly SceneHierarchyViewModel _vm = new();

    public SceneHierarchyView()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
        {
            _vm.Attach();
            AttachWindowDragDrop();
        }
        else
        {
            DetachWindowDragDrop();
            _vm.Detach();
        }
    }

    // ── Drag & drop via eventos nativos de ventana (WinUI3) ───────────────────

#if WINDOWS
    private Microsoft.UI.Xaml.FrameworkElement? _windowContent;
    private HierarchyItem? _pointerDownItem;
    private double _dragStartX, _dragStartY;
    private bool _isDragging;
    private const double DragThreshold = 14.0;

    private void AttachWindowDragDrop()
    {
        Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
            ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        _windowContent = win?.Content as Microsoft.UI.Xaml.FrameworkElement;
        if (_windowContent is null) return;

        _windowContent.PointerPressed     += OnWindowPointerPressed;
        _windowContent.PointerMoved       += OnWindowPointerMoved;
        _windowContent.PointerReleased    += OnWindowPointerReleased;
        _windowContent.PointerCaptureLost += OnWindowPointerCaptureLost;
    }

    private void DetachWindowDragDrop()
    {
        if (_windowContent is null) return;
        _windowContent.PointerPressed     -= OnWindowPointerPressed;
        _windowContent.PointerMoved       -= OnWindowPointerMoved;
        _windowContent.PointerReleased    -= OnWindowPointerReleased;
        _windowContent.PointerCaptureLost -= OnWindowPointerCaptureLost;
        _windowContent = null;
    }

    private void OnWindowPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_windowContent is null) return;
        Windows.Foundation.Point pt = e.GetCurrentPoint(_windowContent).Position;
        _pointerDownItem = FindHierarchyItemAt(pt);
        if (_pointerDownItem is not null)
        {
            _dragStartX = pt.X;
            _dragStartY = pt.Y;
            _isDragging = false;
        }
    }

    private void OnWindowPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_pointerDownItem is null || _windowContent is null) return;
        Windows.Foundation.Point pt = e.GetCurrentPoint(_windowContent).Position;
        if (!_isDragging)
        {
            double dx = pt.X - _dragStartX;
            double dy = pt.Y - _dragStartY;
            if (Math.Sqrt(dx * dx + dy * dy) > DragThreshold)
            {
                _isDragging = true;
                _vm.StartDrag(_pointerDownItem);
            }
        }
    }

    private void OnWindowPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isDragging && _windowContent is not null)
        {
            Windows.Foundation.Point pt = e.GetCurrentPoint(_windowContent).Position;
            HierarchyItem? target = FindHierarchyItemAt(pt);
            if (target is not null)
                _vm.HandleDrop(target);
        }
        _isDragging = false;
        _pointerDownItem = null;
    }

    private void OnWindowPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isDragging = false;
        _pointerDownItem = null;
    }

    private HierarchyItem? FindHierarchyItemAt(Windows.Foundation.Point windowPt)
    {
        if (_windowContent is null) return null;
        try
        {
            System.Collections.Generic.IEnumerable<Microsoft.UI.Xaml.UIElement> elements =
                Microsoft.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(windowPt, _windowContent);
            foreach (Microsoft.UI.Xaml.UIElement el in elements)
            {
                if (el is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is HierarchyItem item)
                    return item;
            }
        }
        catch { }
        return null;
    }

#else
    private void AttachWindowDragDrop() { }
    private void DetachWindowDragDrop() { }
#endif
}
