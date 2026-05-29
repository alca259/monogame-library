using System.Diagnostics;

namespace MonoGame.Editor.WinForms.Controls;

/// <summary>Argumentos pasados al evento <see cref="MonoGameControl.RenderFrame"/>.</summary>
public sealed class RenderEventArgs(GraphicsDevice graphicsDevice, TimeSpan elapsed, int width, int height) : EventArgs
{
    /// <summary>El GraphicsDevice activo, utilizable para operaciones de dibujo.</summary>
    public GraphicsDevice GraphicsDevice { get; } = graphicsDevice;

    /// <summary>Tiempo transcurrido desde el fotograma anterior.</summary>
    public TimeSpan Elapsed { get; } = elapsed;

    /// <summary>Anchura actual del destino de renderizado en píxeles (seguro de leer desde el hilo de renderizado).</summary>
    public int Width { get; } = width;

    /// <summary>Altura actual del destino de renderizado en píxeles (seguro de leer desde el hilo de renderizado).</summary>
    public int Height { get; } = height;
}

/// <summary>
/// Control de WinForms que aloja un bucle de renderizado de MonoGame usando <see cref="SwapChainRenderTarget"/>.
/// El bucle de renderizado se ejecuta en un hilo de fondo dedicado a ~60 fps.
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

    // Estado de entrada de la cámara (escrito en el hilo de UI, leído en el hilo de renderizado — los floats son atómicos)
    private bool _panActive;
    private bool _handToolActive;
    private System.Drawing.Point _lastPanPos;

    /// <summary>Cámara del editor utilizada para transformar el viewport.</summary>
    public EditorCamera2D Camera { get; } = new();

    /// <summary>
    /// Cuando es <c>false</c>, el bucle de renderizado avanza pero omite todo el dibujo.
    /// Establecer a <c>false</c> para la pestaña inactiva para que solo el viewport visible renderice.
    /// </summary>
    public volatile bool IsActive = true;

    /// <summary>Color utilizado para limpiar el destino de renderizado al inicio de cada fotograma.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Microsoft.Xna.Framework.Color ClearColor { get; set; } = new Microsoft.Xna.Framework.Color(30, 30, 30);

    /// <summary>
    /// Se lanza desde el hilo de renderizado una vez por fotograma, tras limpiar el dispositivo.
    /// Los suscriptores pueden dibujar contenido usando el <see cref="GraphicsDevice"/> proporcionado.
    /// </summary>
    public event EventHandler<RenderEventArgs>? RenderFrame;

    /// <summary>Cuando es true, arrastrar con el botón izquierdo del ratón desplaza la escena como la herramienta de mano.</summary>
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

        // Delta de pantalla → delta de mundo: dividir por el zoom para que la velocidad de paneo sea constante en el espacio mundo
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
        catch { /* ignorar errores de redimensionado */ }
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
        catch { /* dispositivo perdido — se recuperará en el siguiente fotograma */ }
    }

    #endregion
}
