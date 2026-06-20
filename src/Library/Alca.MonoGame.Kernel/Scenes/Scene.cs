using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.Scenes;

/// <summary>This is an abstract class for scenes that provides common functionality for all scenes</summary>
public abstract class Scene : IDisposable
{
    /// <summary>Gets the ContentManager used for loading scene-specific assets.</summary>
    /// <remarks>Assets loaded through this ContentManager will be automatically unloaded when this scene ends.</remarks>
    protected ContentManager Content { get; }

    /// <summary>Gets a value that indicates if the scene has been disposed of.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Gets a value indicating whether this scene is an overlay; when true, the scene beneath continues drawing.</summary>
    public virtual bool IsOverlay => false;

    /// <summary>
    /// Gets the <see cref="GameWorld"/> created by <see cref="CreateWorld"/>, or null if this scene
    /// does not use the ECS. Null by default; set once during <see cref="Initialize"/>.
    /// </summary>
    protected GameWorld? World { get; private set; }

    /// <summary>
    /// Gets the <see cref="UIRoot"/> created by <see cref="EnableUI"/>, or null if UI was not enabled.
    /// Use <see cref="EnableUI"/> in <see cref="PreInitialize"/> to opt in.
    /// </summary>
    protected internal UIRoot? UIRoot { get; private set; }

    /// <summary>
    /// For unit testing: when non-null, used instead of <see cref="Core.SpriteBatch"/> for UI and World drawing.
    /// </summary>
    internal SpriteBatch? _spriteBatchOverride;

    /// <summary>Creates a new scene instance.</summary>
    public Scene()
    {
        // Create a content manager for the scene
        Content = new ContentManager(Core.Content.ServiceProvider)
        {
            // Set the root directory for content to the same as the root directory
            // for the game's content.
            RootDirectory = Core.Content.RootDirectory
        };
    }

    /// <summary>Creates a scene with an explicit ContentManager. Used in unit tests to avoid Core dependency.</summary>
    internal Scene(ContentManager content)
    {
        Content = content;
    }

    /// <summary> Finalizer, called when object is cleaned up by garbage collector.</summary>
    ~Scene() => Dispose(false);

    /// <summary>Called before the game is initialized.</summary>
    protected virtual void PreInitialize()
    {
    }

    /// <summary>
    /// Override to create and configure a <see cref="GameWorld"/> for this scene.
    /// Return null (default) to opt out of ECS — no world will be created or updated.
    /// </summary>
    protected virtual GameWorld? CreateWorld() => null;

    /// <summary>
    /// Called after <see cref="CreateWorld"/> assigns <see cref="World"/> and before <see cref="LoadContent"/>.
    /// Override to populate the world with entities and configure subsystems.
    /// Only meaningful when <see cref="CreateWorld"/> returns a non-null world.
    /// </summary>
    protected virtual void InitializeWorld() { }

    /// <summary>
    /// Creates a full-screen <see cref="UIRoot"/> and wires it to the global <see cref="Core.UIOverlay"/>.
    /// Idempotent — safe to call multiple times. Call from <see cref="PreInitialize"/> so the root is
    /// available when <see cref="InitializeUI"/> runs.
    /// </summary>
    protected void EnableUI()
    {
        if (UIRoot is not null) return;

        var root = new UIRoot();
        root.OverlayManager = Core.UIOverlay;

        ResolutionManager? resolution = Core.Resolution;
        root.Bounds = resolution is not null
            ? new Rectangle(0, 0, resolution.VirtualWidth, resolution.VirtualHeight)
            : (Core.GraphicsDevice?.Viewport.Bounds ?? Rectangle.Empty);

        UIRoot = root;
    }

    /// <summary>Initializes the scene.</summary>
    /// <remarks>
    /// When overriding this in a derived class, ensure that base.Initialize()
    /// still called as this is when LoadContent, PreInitialize and PostInitialize is called.
    /// </remarks>
    public virtual void Initialize()
    {
        PreInitialize();
        World = CreateWorld();
        InitializeWorld();
        LoadContent();
        PostInitialize();
    }

    /// <summary>Called after the game has been initialized but before the first Update method is called.</summary>
    /// <remarks>
    /// When overriding this in a derived class, ensure that base.PostInitialize()
    /// still called as this is when InitializeUI is called.
    /// </remarks>
    protected virtual void PostInitialize()
    {
        InitializeUI();
    }

    /// <summary>Initializes the UI elements for the scene.</summary>
    /// <remarks>This method is called after LoadContent and PostInitialize. Call <see cref="EnableUI"/> first to create the root.</remarks>
    protected virtual void InitializeUI()
    {
    }

    /// <summary>Override to provide logic to load content for the scene.</summary>
    public virtual void LoadContent() { }

    /// <summary>
    /// Unloads scene-specific content and destroys the ECS world if one was created.
    /// </summary>
    public virtual void UnloadContent()
    {
        World?.Destroy();
        Content.Unload();
    }

    /// <summary>
    /// Updates this scene. When overriding, call <c>base.Update(gameTime)</c> first to
    /// ensure the <see cref="World"/> is stepped before custom scene logic runs.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current frame.</param>
    public virtual void Update(GameTime gameTime)
    {
        World?.Update(gameTime);
    }

    /// <summary>
    /// Draws this scene. When overriding, call <c>base.Draw(gameTime)</c> first to ensure the
    /// <see cref="World"/> and <see cref="UIRoot"/> are rendered before custom scene draw logic runs.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current frame.</param>
    public virtual void Draw(GameTime gameTime)
    {
        World?.Draw(gameTime, Core.SpriteBatch!);

        SpriteBatch? sb = _spriteBatchOverride ?? Core.SpriteBatch;
        if (UIRoot is not null && sb is not null)
            UIRoot.DrawAll(sb);
    }

    /// <summary>Disposes of this scene.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes of this scene.</summary>
    /// <param name="disposing">
    /// Indicates whether managed resources should be disposed.  This value is only true when called from the main
    /// Dispose method.  When called from the finalizer, this will be false.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        if (disposing)
        {
            UnloadContent();
            Content.Dispose();
        }
    }
}
