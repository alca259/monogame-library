using Alca.MonoGame.Kernel.Graphics;

namespace Alca.MonoGame.Kernel.Platform;

/// <summary>Manages platform detection, screen resize events, and app lifecycle notifications.</summary>
public sealed class PlatformManager : IDisposable
{
    private readonly Game? _game;
    private readonly ResolutionManager _resolution;

    /// <summary>Gets the platform type detected at startup.</summary>
    public PlatformType CurrentPlatform { get; }

    /// <summary>Gets whether the game is running on a desktop platform.</summary>
    public bool IsDesktop => CurrentPlatform == PlatformType.Desktop;

    /// <summary>Gets whether the game is running on a mobile platform.</summary>
    public bool IsMobile => CurrentPlatform == PlatformType.Mobile;

    /// <summary>Gets whether the game is running on a console platform.</summary>
    public bool IsConsole => CurrentPlatform == PlatformType.Console;

    /// <summary>Gets the virtual design width in pixels, delegated from <see cref="ResolutionManager"/>.</summary>
    public int VirtualWidth => _resolution.VirtualWidth;

    /// <summary>Gets the virtual design height in pixels, delegated from <see cref="ResolutionManager"/>.</summary>
    public int VirtualHeight => _resolution.VirtualHeight;

    /// <summary>Gets or sets the supported display orientations (relevant on mobile platforms).</summary>
    public DisplayOrientation SupportedOrientations { get; set; } = DisplayOrientation.Default;

    /// <summary>Fires when the game window is resized.</summary>
    public event Action? ScreenResized;

    /// <summary>Fires when the application is sent to the background.</summary>
    public event Action? AppPaused;

    /// <summary>Creates a new PlatformManager and subscribes to window and lifecycle events.</summary>
    /// <param name="game">The running <see cref="Game"/> instance.</param>
    /// <param name="resolution">The resolution manager to delegate virtual dimensions to.</param>
    public PlatformManager(Game game, ResolutionManager resolution)
    {
        _game = game;
        _resolution = resolution;

#if ANDROID || IOS
        CurrentPlatform = PlatformType.Mobile;
#elif XBOX
        CurrentPlatform = PlatformType.Console;
#else
        CurrentPlatform = PlatformType.Desktop;
#endif

        game.Window.ClientSizeChanged += OnClientSizeChanged;
        game.Deactivated += OnDeactivated;
    }

    /// <summary>Internal constructor for unit testing — does not subscribe to game events.</summary>
    internal PlatformManager(PlatformType platform, ResolutionManager resolution)
    {
        _game = null;
        CurrentPlatform = platform;
        _resolution = resolution;
    }

    /// <summary>Unsubscribes from game window and lifecycle events.</summary>
    public void Dispose()
    {
        if (_game is null)
            return;

        _game.Window.ClientSizeChanged -= OnClientSizeChanged;
        _game.Deactivated -= OnDeactivated;
    }

    private void OnClientSizeChanged(object? sender, EventArgs e) => ScreenResized?.Invoke();

    private void OnDeactivated(object? sender, EventArgs e) => AppPaused?.Invoke();
}
