using System.Runtime.InteropServices;
using Microsoft.Maui.Handlers;
using Microsoft.Xna.Framework;
using WinRT.Interop;
using MauiCanvas = Microsoft.UI.Xaml.Controls.Canvas;

namespace MonoGame.Editor.Maui.Platforms.Windows;

/// <summary>
/// Handler de Windows para <see cref="MonoGameView"/>.
/// Crea un HWND Win32 hijo sobre el Canvas nativo de WinUI 3 y lo usa
/// como superficie de renderizado para MonoGame (SwapChainRenderTarget).
/// El input de ratón (pan / zoom / selección) se captura mediante subclassing del WndProc.
/// </summary>
internal sealed class MonoGameViewHandler : ViewHandler<MonoGameView, MauiCanvas>
{
    public static readonly PropertyMapper<MonoGameView, MonoGameViewHandler> Mapper =
        new(ViewHandler.ViewMapper);

    public MonoGameViewHandler() : base(Mapper) { }

    // ── Win32 window styles ───────────────────────────────────────────────────

    private const uint WS_CHILD         = 0x40000000;
    private const uint WS_VISIBLE       = 0x10000000;
    private const uint WS_CLIPSIBLINGS  = 0x04000000;
    private const uint WS_CLIPCHILDREN  = 0x02000000;

    // ── Win32 messages ────────────────────────────────────────────────────────

    private const uint WM_MOUSEMOVE   = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_MBUTTONDOWN = 0x0207;
    private const uint WM_MBUTTONUP   = 0x0208;
    private const uint WM_MOUSEWHEEL  = 0x020A;
    private const int  GWLP_WNDPROC   = -4;

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint   dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int    x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCapture(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly EditorGameLoop _gameLoop = new();
    private IntPtr                  _childHwnd   = IntPtr.Zero;
    private IntPtr                  _oldWndProc  = IntPtr.Zero;
    private WndProcDelegate?        _wndProcDelegate;   // must be a field — prevents GC

    private bool _panActive;
    private int  _panLastX;
    private int  _panLastY;

    // ── ViewHandler overrides ─────────────────────────────────────────────────

    protected override MauiCanvas CreatePlatformView() => new MauiCanvas();

    protected override void ConnectHandler(MauiCanvas nativeView)
    {
        base.ConnectHandler(nativeView);
        nativeView.Loaded      += OnNativeLoaded;
        nativeView.SizeChanged += OnNativeSizeChanged;
    }

    protected override void DisconnectHandler(MauiCanvas nativeView)
    {
        nativeView.Loaded      -= OnNativeLoaded;
        nativeView.SizeChanged -= OnNativeSizeChanged;

        _gameLoop.Stop();

        if (_childHwnd != IntPtr.Zero)
        {
            if (_oldWndProc != IntPtr.Zero)
                SetWindowLongPtrW(_childHwnd, GWLP_WNDPROC, _oldWndProc);

            DestroyWindow(_childHwnd);
            _childHwnd  = IntPtr.Zero;
            _oldWndProc = IntPtr.Zero;
        }

        _wndProcDelegate = null;
        base.DisconnectHandler(nativeView);
    }

    // ── Native event handlers ─────────────────────────────────────────────────

    private void OnNativeLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        IntPtr appHwnd = GetAppHwnd();
        if (appHwnd == IntPtr.Zero) return;

        GetPixelBounds(out int x, out int y, out int w, out int h);
        if (w <= 0 || h <= 0) return;

        _childHwnd = CreateWindowExW(
            0, "STATIC", string.Empty,
            WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
            x, y, w, h,
            appHwnd, IntPtr.Zero, GetModuleHandleW(null), IntPtr.Zero);

        if (_childHwnd == IntPtr.Zero) return;

        _gameLoop.Start(_childHwnd, w, h);
        InstallWndProc();
    }

    private void OnNativeSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        if (_childHwnd == IntPtr.Zero) return;

        GetPixelBounds(out int x, out int y, out int w, out int h);
        if (w <= 0 || h <= 0) return;

        MoveWindow(_childHwnd, x, y, w, h, true);
        _gameLoop.Resize(w, h);
    }

    // ── WndProc subclassing ───────────────────────────────────────────────────

    private void InstallWndProc()
    {
        _wndProcDelegate = ViewportWndProc;
        _oldWndProc = GetWindowLongPtrW(_childHwnd, GWLP_WNDPROC);
        SetWindowLongPtrW(_childHwnd, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }

    private IntPtr ViewportWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_LBUTTONDOWN:
            {
                SetFocus(hWnd);
                int mx = GetLoWord(lParam);
                int my = GetHiWord(lParam);
                HandleClick(mx, my);
                return IntPtr.Zero;
            }

            case WM_MBUTTONDOWN:
            {
                SetFocus(hWnd);
                SetCapture(hWnd);
                _panActive = true;
                _panLastX  = GetLoWord(lParam);
                _panLastY  = GetHiWord(lParam);
                return IntPtr.Zero;
            }

            case WM_MBUTTONUP:
            {
                _panActive = false;
                ReleaseCapture();
                return IntPtr.Zero;
            }

            case WM_MOUSEMOVE:
            {
                if (!_panActive) break;
                int mx = GetLoWord(lParam);
                int my = GetHiWord(lParam);
                int dx = mx - _panLastX;
                int dy = my - _panLastY;
                _panLastX = mx;
                _panLastY = my;
                HandlePan(dx, dy);
                return IntPtr.Zero;
            }

            case WM_MOUSEWHEEL:
            {
                short delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                float factor = delta > 0 ? 1.1f : 0.9f;

                POINT pt = new() { x = GetLoWord(lParam), y = GetHiWord(lParam) };
                ScreenToClient(hWnd, ref pt);
                HandleZoom(factor, pt.x, pt.y);
                return IntPtr.Zero;
            }
        }

        return CallWindowProcW(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    // ── Input logic ───────────────────────────────────────────────────────────

    private void HandlePan(int dx, int dy)
    {
        EditorCamera2D cam = _gameLoop.Camera;
        cam.Pan(new Vector2(-dx, -dy) / cam.Zoom);
    }

    private void HandleZoom(float factor, int clientX, int clientY)
    {
        GetPixelBounds(out _, out _, out int w, out int h);
        Microsoft.Xna.Framework.Graphics.Viewport vp = new(0, 0, w, h);
        _gameLoop.Camera.ZoomAt(factor, new Vector2(clientX, clientY), vp);
    }

    private void HandleClick(int clientX, int clientY)
    {
        EditorScene? scene = EditorContext.Instance.ActiveScene;
        if (scene is null) return;
        if (EditorContext.Instance.Gizmos.Mode != GizmoMode.Select) return;

        GetPixelBounds(out _, out _, out int w, out int h);
        Microsoft.Xna.Framework.Graphics.Viewport vp = new(0, 0, w, h);
        Vector2 worldPos = _gameLoop.Camera.ScreenToWorld(new Vector2(clientX, clientY), vp);

        EditorGameObject? hit = HitTest(scene.RootGameObjects, worldPos);
        EditorContext.Instance.SetSelection(hit);
    }

    private static EditorGameObject? HitTest(List<EditorGameObject> objects, Vector2 worldPos)
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IntPtr GetAppHwnd()
    {
        if (VirtualView?.Window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window win)
            return WindowNative.GetWindowHandle(win);

        return IntPtr.Zero;
    }

    private void GetPixelBounds(out int x, out int y, out int w, out int h)
    {
        x = y = w = h = 0;
        if (PlatformView is null) return;

        double scale = PlatformView.XamlRoot?.RasterizationScale ?? 1.0;

        try
        {
            Microsoft.UI.Xaml.Media.GeneralTransform transform =
                PlatformView.TransformToVisual(null);

            global::Windows.Foundation.Point origin =
                transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));

            x = (int)(origin.X * scale);
            y = (int)(origin.Y * scale);
            w = (int)(PlatformView.ActualWidth  * scale);
            h = (int)(PlatformView.ActualHeight * scale);
        }
        catch { }
    }

    private static int GetLoWord(IntPtr lp) => (short)(lp.ToInt64() & 0xFFFF);
    private static int GetHiWord(IntPtr lp) => (short)((lp.ToInt64() >> 16) & 0xFFFF);
}
