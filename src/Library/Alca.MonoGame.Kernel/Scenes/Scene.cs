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

    /// <summary>Initializes the scene.</summary>
    /// <remarks>
    /// When overriding this in a derived class, ensure that base.Initialize()
    /// still called as this is when LoadContent, PreInitialize and PostInitialize is called.
    /// </remarks>
    public virtual void Initialize()
    {
        PreInitialize();
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
    /// <remarks>This method is called after LoadContent and PostInitialize.</remarks>
    protected virtual void InitializeUI()
    {
    }

    /// <summary>Override to provide logic to load content for the scene.</summary>
    public virtual void LoadContent() { }

    /// <summary>Unloads scene-specific content.</summary>
    public virtual void UnloadContent()
    {
        Content.Unload();
    }

    /// <summary>Updates this scene.</summary>
    /// <param name="gameTime">A snapshot of the timing values for the current frame.</param>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>Draws this scene.</summary>
    /// <param name="gameTime">A snapshot of the timing values for the current frame.</param>
    public virtual void Draw(GameTime gameTime) { }

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

