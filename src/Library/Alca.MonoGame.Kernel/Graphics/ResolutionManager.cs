namespace Alca.MonoGame.Kernel.Graphics;

/// <summary>Manages virtual resolution and letterboxing for resolution-independent rendering.</summary>
public sealed class ResolutionManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GameWindow? _window;
    private Matrix _scaleMatrix;
    private Matrix _worldScaleMatrix;
    private Viewport _letterboxViewport;

    /// <summary>Gets the virtual design width in pixels.</summary>
    public int VirtualWidth { get; }

    /// <summary>Gets the virtual design height in pixels.</summary>
    public int VirtualHeight { get; }

    /// <summary>
    /// Gets the non-uniform scale matrix for full-screen UI rendering.
    /// Stretches virtual space to fill the entire screen. Pass to <see cref="SpriteBatch.Begin"/>.
    /// </summary>
    public Matrix ScaleMatrix => _scaleMatrix;

    /// <summary>
    /// Gets the uniform scale matrix for 2D world rendering with letterboxing.
    /// Preserves the virtual aspect ratio; combine with <see cref="Camera.Camera2D.GetTransformMatrix"/>.
    /// </summary>
    public Matrix WorldScaleMatrix => _worldScaleMatrix;

    /// <summary>Gets the letterboxed viewport that preserves the virtual aspect ratio.</summary>
    public Viewport LetterboxViewport => _letterboxViewport;

    /// <summary>Creates a ResolutionManager and subscribes to window resize events.</summary>
    /// <param name="graphicsDevice">The graphics device used to query the current screen size.</param>
    /// <param name="window">The game window whose <c>ClientSizeChanged</c> event will trigger recalculation.</param>
    /// <param name="virtualWidth">Design resolution width in pixels. Defaults to 1920.</param>
    /// <param name="virtualHeight">Design resolution height in pixels. Defaults to 1080.</param>
    public ResolutionManager(GraphicsDevice graphicsDevice, GameWindow window, int virtualWidth = 1920, int virtualHeight = 1080)
    {
        _graphicsDevice = graphicsDevice;
        _window = window;
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;

        window.ClientSizeChanged += OnClientSizeChanged;

        Update(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
    }

    /// <summary>Internal constructor for unit testing — skips hardware dependencies.</summary>
    internal ResolutionManager(int virtualWidth, int virtualHeight)
    {
        _graphicsDevice = null!;
        _window = null;
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;
    }

    /// <summary>Recalculates scale matrices and the letterbox viewport for the given screen dimensions.</summary>
    public void Update(int screenWidth, int screenHeight)
    {
        float scaleX = (float)screenWidth / VirtualWidth;
        float scaleY = (float)screenHeight / VirtualHeight;
        float scale  = MathF.Min(scaleX, scaleY);

        int viewportWidth  = (int)(VirtualWidth  * scale);
        int viewportHeight = (int)(VirtualHeight * scale);
        int offsetX = (screenWidth  - viewportWidth)  / 2;
        int offsetY = (screenHeight - viewportHeight) / 2;

        _scaleMatrix       = Matrix.CreateScale(scaleX, scaleY, 1f);
        _worldScaleMatrix  = Matrix.CreateScale(scale,  scale,  1f);
        _letterboxViewport = new Viewport(offsetX, offsetY, viewportWidth, viewportHeight);
    }

    /// <summary>Converts a raw screen-space position to virtual resolution space, accounting for letterboxing.</summary>
    public Vector2 ScreenToVirtual(Vector2 screenPos)
    {
        float scaleX = (float)_letterboxViewport.Width  / VirtualWidth;
        float scaleY = (float)_letterboxViewport.Height / VirtualHeight;

        return new Vector2(
            (screenPos.X - _letterboxViewport.X) / scaleX,
            (screenPos.Y - _letterboxViewport.Y) / scaleY);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_window is not null)
            _window.ClientSizeChanged -= OnClientSizeChanged;
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        Update(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
    }
}
