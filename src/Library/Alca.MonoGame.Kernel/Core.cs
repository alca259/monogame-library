using Alca.MonoGame.Kernel.Audio;
using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.Localization;
using Alca.MonoGame.Kernel.Platform;
using Alca.MonoGame.Kernel.Scenes;
using Alca.MonoGame.Kernel.Tweening;
using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel;

/// <summary>Core extended Game class for MonoGame applications.</summary>
public abstract class Core : Game
{
    private static Core _instance = null!;
    private IServiceProvider _serviceProvider = null!;

    /// <summary>Indicates if the game is currently in fullscreen mode.</summary>
    private bool _isFullScreen;

    /// <summary>Gets reference to the Core instance.</summary>
    public static Core Instance => _instance;
    /// <summary>Gets the graphics device manager to control the presentation of graphics.</summary>
    public static GraphicsDeviceManager Graphics { get; private set; } = null!;
    /// <summary>Gets the graphics device used to create graphical resources and perform primitive rendering.</summary>
    public static new GraphicsDevice GraphicsDevice { get; private set; } = null!;
    /// <summary>Gets the sprite batch used for all 2D rendering.</summary>
    public static SpriteBatch SpriteBatch { get; private set; } = null!;
    /// <summary>Gets the content manager used to load global assets.</summary>
    public static new ContentManager Content { get; private set; } = null!;
    /// <summary>Gets a reference to the input management system.</summary>
    public static InputManager Input { get; private set; } = null!;
    /// <summary>Gets a reference to the audio control system.</summary>
    public static AudioController Audio { get; private set; } = null!;
    /// <summary>Gets the scene manager responsible for scene transitions and fade effects.</summary>
    public static SceneManager SceneManager { get; private set; } = null!;
    /// <summary>Gets the tweening manager for animating float properties.</summary>
    public static TweeningManager Tweening { get; private set; } = null!;
    /// <summary>Gets the localization manager for multi-language string lookup.</summary>
    public static LocalizationManager Localization { get; private set; } = null!;
    /// <summary>Gets the resolution manager for virtual-resolution scaling and letterboxing.</summary>
    public static ResolutionManager Resolution { get; private set; } = null!;
    /// <summary>Gets the platform manager for platform detection and lifecycle events.</summary>
    public static PlatformManager Platform { get; private set; } = null!;
    /// <summary>Gets the UI interaction manager for pointer hit testing and event dispatch.</summary>
    public static UIInteractionManager UIInteraction { get; private set; } = null!;
    /// <summary>Gets the UI focus manager for keyboard and gamepad navigation.</summary>
    public static UIFocusManager UIFocus { get; private set; } = null!;
    /// <summary>Gets the UI overlay manager for floating elements such as dropdowns and tooltips.</summary>
    public static UIOverlayManager UIOverlay { get; private set; } = null!;
    /// <summary>Gets the game window (for TextInput event subscription and window title changes).</summary>
    public static new GameWindow Window { get; private set; } = null!;
    /// <summary>Gets or sets a value that indicates if the game should exit when the Escape key is pressed.</summary>
    public static bool ExitOnEscape { get; set; }

    /// <summary>Creates a new Core instance.</summary>
    /// <param name="title">The title to display in the title bar of the game window.</param>
    /// <param name="width">The initial width, in pixels, of the game window.</param>
    /// <param name="height">The initial height, in pixels, of the game window.</param>
    /// <param name="fullScreen">Indicates if the game should start in fullscreen mode.</param>
    protected Core(string title, int width, int height, bool fullScreen)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("Only a single Core instance can be created");
        }

        _instance = this;
        _isFullScreen = fullScreen;

        Graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = width,
            PreferredBackBufferHeight = height,
            IsFullScreen = fullScreen
        };

        Graphics.ApplyChanges();

        Window = base.Window;
        Window.Title = title;
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        ExitOnEscape = true;
    }

    /// <summary>Called before the game is initialized.</summary>
    protected virtual void PreInitialize() { }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        PreInitialize();

        base.Initialize();

        ServiceCollection services = new();

        services.AddSingleton<GraphicsDeviceManager>(Graphics);
        services.AddSingleton<GraphicsDevice>(base.GraphicsDevice);
        services.AddSingleton<ContentManager>(base.Content);
        services.AddSingleton<SpriteBatch>(sp =>
            new SpriteBatch(sp.GetRequiredService<GraphicsDevice>()));
        services.AddSingleton<InputManager>();
        services.AddSingleton<AudioController>();
        services.AddSingleton<SceneManager>(_ => new SceneManager(this));
        services.AddSingleton<TweeningManager>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizer>(sp => sp.GetRequiredService<LocalizationManager>());
        services.AddSingleton<ResolutionManager>(sp =>
            new ResolutionManager(sp.GetRequiredService<GraphicsDevice>(), Window));
        services.AddSingleton<PlatformManager>(sp =>
            new PlatformManager(this, sp.GetRequiredService<ResolutionManager>()));
        services.AddSingleton<UIInteractionManager>();
        services.AddSingleton<UIFocusManager>();
        services.AddSingleton<UIOverlayManager>();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        GraphicsDevice = _serviceProvider.GetRequiredService<GraphicsDevice>();
        SpriteBatch = _serviceProvider.GetRequiredService<SpriteBatch>();
        Content = _serviceProvider.GetRequiredService<ContentManager>();
        Input = _serviceProvider.GetRequiredService<InputManager>();
        Audio = _serviceProvider.GetRequiredService<AudioController>();
        SceneManager = _serviceProvider.GetRequiredService<SceneManager>();
        Tweening = _serviceProvider.GetRequiredService<TweeningManager>();
        Localization = _serviceProvider.GetRequiredService<LocalizationManager>();
        Resolution = _serviceProvider.GetRequiredService<ResolutionManager>();
        Platform = _serviceProvider.GetRequiredService<PlatformManager>();
        UIInteraction = _serviceProvider.GetRequiredService<UIInteractionManager>();
        UIFocus = _serviceProvider.GetRequiredService<UIFocusManager>();
        UIOverlay = _serviceProvider.GetRequiredService<UIOverlayManager>();

        PostInitialize();
    }

    /// <summary>Resolves a registered service from the DI container.</summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    public static T GetService<T>() where T : notnull
        => _instance._serviceProvider.GetRequiredService<T>();

    /// <summary>Override to register additional services into the DI container.
    /// Called during Initialize() after built-in kernel services are registered
    /// but before the container is built.</summary>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>Called after the game has been initialized but before the first Update method is called.</summary>
    protected virtual void PostInitialize() { }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
        Audio.Update();
        Tweening.Update(gameTime);

        if (ExitOnEscape && Input.Keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (Input.Keyboard.IsKeyDown(Keys.F11))
        {
            _isFullScreen = !_isFullScreen;
            Graphics.IsFullScreen = _isFullScreen;
            Graphics.ApplyChanges();
        }

        UIRoot? activeUI = SceneManager.ActiveUIRoot;
        if (activeUI is not null)
            UIInteraction.Update(activeUI, Input.Mouse, UIFocus);

        SceneManager.Update(gameTime);

        base.Update(gameTime);
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);

        base.Draw(gameTime);
    }

    /// <inheritdoc/>
    protected override void UnloadContent()
    {
        Audio.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.UnloadContent();
    }
}
