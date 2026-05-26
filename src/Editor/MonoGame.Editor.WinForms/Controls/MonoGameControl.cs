using System.Diagnostics;

namespace MonoGame.Editor.WinForms.Controls;

/// <summary>Arguments passed to the <see cref="MonoGameControl.RenderFrame"/> event.</summary>
public sealed class RenderEventArgs(GraphicsDevice graphicsDevice, TimeSpan elapsed, int width, int height) : EventArgs
{
    /// <summary>The active graphics device, usable for drawing operations.</summary>
    public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;

    /// <summary>Time elapsed since the previous frame.</summary>
    public TimeSpan Elapsed { get; } = elapsed;

    /// <summary>Current render-target width in pixels (safe to read from render thread).</summary>
    public int Width { get; } = width;

    /// <summary>Current render-target height in pixels (safe to read from render thread).</summary>
    public int Height { get; } = height;
}

/// <summary>
/// WinForms control that hosts a MonoGame render loop using <see cref="SwapChainRenderTarget"/>.
/// The render loop runs on a dedicated background thread at ~60 fps.
/// </summary>
public sealed class MonoGameControl : Control
{
    private GraphicsDevice? _graphicsDevice;
    private SwapChainRenderTarget? _swapChain;
    private Thread? _renderThread;
    private volatile bool _running;
    private volatile bool _resizePending;
    private IntPtr _windowHandle;

    private readonly Lock _resizeLock = new();
    private int _pendingWidth;
    private int _pendingHeight;

    // Camera input state (written on UI thread, read on render thread — floats are atomic)
    private bool _panActive;
    private bool _handToolActive;
    private System.Drawing.Point _lastPanPos;

    /// <summary>Editor camera used to transform the viewport.</summary>
    public EditorCamera2D Camera { get; } = new();

    /// <summary>
    /// When <c>false</c> the render loop ticks but skips all drawing.
    /// Set to <c>false</c> for the inactive tab so only the visible viewport renders.
    /// </summary>
    public volatile bool IsActive = true;

    /// <summary>Color used to clear the render target at the start of each frame.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Microsoft.Xna.Framework.Color ClearColor { get; set; } = new Microsoft.Xna.Framework.Color(30, 30, 30);

    /// <summary>
    /// Raised from the render thread once per frame, after the device is cleared.
    /// Subscribers can draw content using the provided <see cref="GraphicsDevice"/>.
    /// </summary>
    public event EventHandler<RenderEventArgs>? RenderFrame;

    /// <summary>When true, left mouse drag pans the scene like a hand tool.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool HandToolEnabled
    {
        get => _handToolActive;
        set => _handToolActive = value;
    }

    public MonoGameControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.Opaque,
            true);
        UpdateStyles();
    }

    /// <inheritdoc/>
    protected override void OnPaintBackground(PaintEventArgs e) { }

    /// <inheritdoc/>
    protected override void OnPaint(PaintEventArgs e) { }

    /// <inheritdoc/>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (DesignMode) return;

        _windowHandle = Handle;
        InitializeGraphics();
        StartRenderLoop();
    }

    /// <inheritdoc/>
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _running = false;
        _renderThread?.Join(2000);
        _swapChain?.Dispose();
        _graphicsDevice?.Dispose();
        _windowHandle = IntPtr.Zero;
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_graphicsDevice == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        lock (_resizeLock)
        {
            _pendingWidth = ClientSize.Width;
            _pendingHeight = ClientSize.Height;
            _resizePending = true;
        }
    }

    #region Mouse input — camera pan and zoom

    /// <inheritdoc/>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Middle || (_handToolActive && e.Button == MouseButtons.Left))
        {
            _panActive = true;
            _lastPanPos = e.Location;
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_panActive) return;

        System.Drawing.Point delta = new(e.X - _lastPanPos.X, e.Y - _lastPanPos.Y);
        _lastPanPos = e.Location;

        // Screen delta → world delta: divide by zoom so pan speed is constant in world space
        Camera.Pan(new Vector2(-delta.X, -delta.Y) / Camera.Zoom);
    }

    /// <inheritdoc/>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Middle || (_handToolActive && e.Button == MouseButtons.Left))
            _panActive = false;
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_graphicsDevice == null) return;

        float factor = e.Delta > 0 ? 1.1f : 0.9f;
        Viewport vp = new(0, 0, ClientSize.Width, ClientSize.Height);
        Camera.ZoomAt(factor, new Vector2(e.X, e.Y), vp);
    }

    #endregion

    #region Graphics initialization

    private void InitializeGraphics()
    {
        try
        {
            if (_windowHandle == IntPtr.Zero)
                return;

            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            PresentationParameters pp = new()
            {
                BackBufferWidth = w,
                BackBufferHeight = h,
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.Depth24,
                DeviceWindowHandle = _windowHandle,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false,
            };

            _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, pp);
            _swapChain = new SwapChainRenderTarget(_graphicsDevice, _windowHandle, w, h);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize graphics device:\n{ex.Message}",
                "MonoGame Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StartRenderLoop()
    {
        _running = true;
        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "MonoGame Render",
        };
        _renderThread.Start();
    }

    #endregion

    #region Render loop

    private void RenderLoop()
    {
        Stopwatch sw = Stopwatch.StartNew();
        long prevTicks = sw.ElapsedTicks;
        const double targetMs = 1000.0 / 60.0;

        while (_running)
        {
            long currentTicks = sw.ElapsedTicks;
            double deltaMs = (currentTicks - prevTicks) * 1000.0 / Stopwatch.Frequency;

            if (deltaMs < targetMs)
            {
                int sleepMs = (int)(targetMs - deltaMs) - 1;
                if (sleepMs > 0) Thread.Sleep(sleepMs);
                continue;
            }

            TimeSpan elapsed = TimeSpan.FromTicks(currentTicks - prevTicks);
            prevTicks = currentTicks;

            ApplyPendingResize();
            DoRender(elapsed);
        }
    }

    private void ApplyPendingResize()
    {
        if (!_resizePending) return;

        int w, h;
        lock (_resizeLock)
        {
            if (!_resizePending) return;
            w = _pendingWidth;
            h = _pendingHeight;
            _resizePending = false;
        }

        try
        {
            if (_windowHandle == IntPtr.Zero)
                return;

            _swapChain?.Dispose();
            _swapChain = new SwapChainRenderTarget(_graphicsDevice!, _windowHandle, w, h);
        }
        catch { /* ignore resize errors */ }
    }

    private void DoRender(TimeSpan elapsed)
    {
        if (!IsActive) return;
        if (_graphicsDevice == null || _swapChain == null || _swapChain.IsDisposed)
            return;

        try
        {
            _graphicsDevice.SetRenderTarget(_swapChain);
            _graphicsDevice.Clear(ClearColor);

            RenderFrame?.Invoke(this, new RenderEventArgs(_graphicsDevice, elapsed, _swapChain.Width, _swapChain.Height));

            _graphicsDevice.SetRenderTarget(null);
            _swapChain.Present();
        }
        catch { /* device lost — will recover on next frame */ }
    }

    #endregion
}
