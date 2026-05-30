using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Editor.Core.PlayMode;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGame.Editor.Maui.Rendering;

/// <summary>
/// Gestiona el GraphicsDevice, SwapChainRenderTarget y el hilo de renderizado de MonoGame
/// para el viewport del editor. Espejo del patrón de MonoGameControl en WinForms.
/// </summary>
internal sealed class EditorGameLoop
{
    // ── Singleton de acceso entre capas ──────────────────────────────────────

    private static volatile EditorGameLoop? _current;

    /// <summary>
    /// La instancia activa del bucle, o <c>null</c> si el viewport no está montado.
    /// Establecido en <see cref="Start"/> y borrado en <see cref="Stop"/>.
    /// </summary>
    public static EditorGameLoop? Current => _current;

    // ── Recursos de renderizado ──────────────────────────────────────────────

    private GraphicsDevice?        _gd;
    private SwapChainRenderTarget? _swapChain;
    private Thread?                _renderThread;
    private volatile bool          _running;
    private IntPtr                 _hwnd;

    private volatile bool _resizePending;
    private int           _pendingWidth;
    private int           _pendingHeight;
    private readonly Lock _resizeLock = new();

    private EditModeRenderer? _editRenderer;
    private GridRenderer?     _gridRenderer;

    // ── Play mode ────────────────────────────────────────────────────────────

    private volatile PlayModeRunner? _playRunner;
    private volatile bool            _isPaused;

    // ── FPS ──────────────────────────────────────────────────────────────────

    private int  _frameCount;
    private long _lastFpsTick;

    /// <summary>Cámara 2D del viewport (pan / zoom).</summary>
    public EditorCamera2D Camera { get; } = new();

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Inicializa el GraphicsDevice y arranca el hilo de render a 60 fps.</summary>
    public void Start(IntPtr hwnd, int width, int height)
    {
        _hwnd = hwnd;
        InitializeGraphics(width, height);
        _running = true;
        _current = this;
        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name         = "MonoGame Render",
        };
        _renderThread.Start();
    }

    /// <summary>Encola un cambio de tamaño que se aplica al inicio del siguiente frame.</summary>
    public void Resize(int width, int height)
    {
        lock (_resizeLock)
        {
            _pendingWidth  = width;
            _pendingHeight = height;
            _resizePending = true;
        }
    }

    /// <summary>Detiene el hilo de render y libera todos los recursos.</summary>
    public void Stop()
    {
        if (_current == this) _current = null;
        _running = false;

        PlayModeRunner? runner = _playRunner;
        _playRunner = null;
        runner?.Dispose();

        _renderThread?.Join(2000);
        _editRenderer?.Dispose();
        _editRenderer = null;
        _gridRenderer?.Dispose();
        _gridRenderer = null;
        _swapChain?.Dispose();
        _swapChain = null;
        _gd?.Dispose();
        _gd = null;
    }

    // ── Play mode API ─────────────────────────────────────────────────────────

    /// <summary>Activa el modo play con el runner proporcionado. La inicialización del SpriteBatch ocurre en el hilo de render.</summary>
    public void EnterPlay(PlayModeRunner runner)
    {
        _isPaused   = false;
        _playRunner = runner;
    }

    /// <summary>Congela la lógica del juego; el render sigue ejecutándose.</summary>
    public void Pause() => _isPaused = true;

    /// <summary>Reanuda la lógica del juego desde el estado congelado.</summary>
    public void Resume() => _isPaused = false;

    /// <summary>Detiene el modo play y devuelve el runner para que el llamador lo disponga.</summary>
    public PlayModeRunner? ExitPlay()
    {
        PlayModeRunner? runner = _playRunner;
        _playRunner = null;
        _isPaused   = false;
        return runner;
    }

    // ── Inicialización ────────────────────────────────────────────────────────

    private void InitializeGraphics(int width, int height)
    {
        PresentationParameters pp = new()
        {
            BackBufferWidth    = width,
            BackBufferHeight   = height,
            BackBufferFormat   = SurfaceFormat.Color,
            DepthStencilFormat = DepthFormat.Depth24,
            DeviceWindowHandle = _hwnd,
            PresentationInterval = PresentInterval.Immediate,
            IsFullScreen       = false,
        };

        _gd        = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, pp);
        _swapChain = new SwapChainRenderTarget(_gd, _hwnd, width, height);

        _editRenderer = new EditModeRenderer(EditorContext.Instance);
        _editRenderer.Initialize(_gd);

        _gridRenderer = new GridRenderer();
        _gridRenderer.Initialize(_gd);
    }

    // ── Bucle de renderizado ──────────────────────────────────────────────────

    private void RenderLoop()
    {
        Stopwatch sw = Stopwatch.StartNew();
        long prevTicks = sw.ElapsedTicks;
        _lastFpsTick   = sw.ElapsedTicks;
        const double targetMs = 1000.0 / 60.0;

        while (_running)
        {
            long   currentTicks = sw.ElapsedTicks;
            double deltaMs = (currentTicks - prevTicks) * 1000.0 / Stopwatch.Frequency;

            if (deltaMs < targetMs)
            {
                int sleepMs = (int)(targetMs - deltaMs) - 1;
                if (sleepMs > 0) Thread.Sleep(sleepMs);
                continue;
            }

            prevTicks = currentTicks;
            _frameCount++;

            if (currentTicks - _lastFpsTick >= Stopwatch.Frequency)
            {
                EditorContext.Instance.EventBus.Publish(new FpsUpdatedEvent(_frameCount));
                _frameCount  = 0;
                _lastFpsTick = currentTicks;
            }

            ApplyPendingResize();

            TimeSpan elapsed = TimeSpan.FromMilliseconds(deltaMs);

            PlayModeRunner? runner = _playRunner;
            if (runner is not null)
            {
                runner.EnsureInitialized(_gd!);
                if (!_isPaused)
                    runner.Update(elapsed);
            }

            DoRender(elapsed, runner);
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
            _swapChain?.Dispose();
            _swapChain = new SwapChainRenderTarget(_gd!, _hwnd, w, h);
        }
        catch { }
    }

    private void DoRender(TimeSpan elapsed, PlayModeRunner? runner)
    {
        if (_gd is null || _swapChain is null || _swapChain.IsDisposed) return;

        try
        {
            _gd.SetRenderTarget(_swapChain);

            if (runner is { IsInitialized: true })
            {
                // Play mode — black backdrop, game world draws itself
                _gd.Clear(XnaColor.Black);
                runner.Draw(elapsed);
            }
            else
            {
                // Edit mode
                _gd.Clear(new XnaColor(29, 29, 30));

                Viewport vp        = new(0, 0, _swapChain.Width, _swapChain.Height);
                Matrix   camMatrix = Camera.GetTransformMatrix(vp);

                GizmoController gizmos = EditorContext.Instance.Gizmos;

                if (gizmos.ShowGrid)
                    _gridRenderer?.DrawGrid(Camera, vp, gizmos.GridCellSize);

                if (EditorContext.Instance.ActiveScene is EditorScene scene
                    && _editRenderer?.IsInitialized == true)
                {
                    _editRenderer.DrawScene(scene, camMatrix);
                }
            }

            _gd.SetRenderTarget(null);
            _swapChain.Present();
        }
        catch { /* device lost — se recupera en el siguiente frame */ }
    }
}
