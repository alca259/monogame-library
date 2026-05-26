namespace Alca.MonoGame.Kernel.Scenes;

/// <summary>Manages scene transitions with fade in/out effects and an overlay stack.</summary>
public sealed class SceneManager
{
    private enum FadeState { None, FadingOut, FadingIn }

    private const int StackCapacity = 4;
    private const float FadeDuration = 0.3f;

    // Retained for future lifecycle hooks
#pragma warning disable IDE0052
    private readonly Game _game;
#pragma warning restore IDE0052

    private Scene? _currentScene;
    private Scene? _queuedScene;

    private FadeState _fadeState = FadeState.None;
    private float _fadeTimer;
    private float _fadeAlpha;

    /// <summary>Overlay stack. Last element is the top (most recently pushed).</summary>
    private readonly Stack<Scene> _sceneStack = new(StackCapacity);

    /// <summary>Pre-allocated buffer used during Draw to iterate overlays bottom-to-top without allocations.</summary>
    private readonly Scene[] _drawBuffer = new Scene[StackCapacity];

    /// <summary>Gets the currently active scene.</summary>
    public Scene? CurrentScene => _currentScene;

    /// <summary>Gets the number of overlay scenes currently on the stack.</summary>
    public int OverlayCount => _sceneStack.Count;

    /// <summary>Gets the <see cref="UI.UIRoot"/> of the currently active scene, or null if no scene is active or UI was not enabled.</summary>
    public UI.UIRoot? ActiveUIRoot => _currentScene?.UIRoot;

    /// <summary>Exposed for unit testing only. Returns the current fade alpha value.</summary>
    internal float FadeAlpha => _fadeAlpha;

    /// <summary>Creates a new SceneManager bound to the given game instance.</summary>
    public SceneManager(Game game)
    {
        _game = game;
    }

    internal SceneManager()
    {
        _game = null!;
    }

    /// <summary>Requests a full scene replacement with a fade effect. Disposes all stacked overlays.</summary>
    public void RequestChange(Scene scene)
    {
        while (_sceneStack.Count > 0)
            _sceneStack.Pop().Dispose();

        _queuedScene = scene;
        if (_fadeState == FadeState.None)
        {
            _fadeState = FadeState.FadingOut;
            _fadeTimer = 0f;
        }
    }

    /// <summary>Pushes an overlay scene on top of the current scene without destroying it.
    /// The overlay is initialized immediately; no fade is applied.</summary>
    public void PushScene(Scene overlay)
    {
        overlay.Initialize();
        _sceneStack.Push(overlay);
    }

    /// <summary>Removes and disposes the topmost overlay, resuming the scene beneath.</summary>
    public void PopScene()
    {
        if (_sceneStack.Count == 0) return;
        _sceneStack.Pop().Dispose();
    }

    /// <summary>Updates fade state and the active scene (or topmost overlay when the stack is non-empty).</summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_fadeState)
        {
            case FadeState.FadingOut:
                _fadeTimer += dt;
                _fadeAlpha = Math.Min(1f, _fadeTimer / FadeDuration);
                if (_fadeTimer >= FadeDuration)
                {
                    _fadeAlpha = 1f;
                    ApplyPendingChange();
                    _fadeState = FadeState.FadingIn;
                    _fadeTimer = 0f;
                }
                break;

            case FadeState.FadingIn:
                _fadeTimer += dt;
                _fadeAlpha = 1f - Math.Min(1f, _fadeTimer / FadeDuration);
                if (_fadeTimer >= FadeDuration)
                {
                    _fadeAlpha = 0f;
                    if (_queuedScene != null)
                    {
                        _fadeState = FadeState.FadingOut;
                        _fadeTimer = 0f;
                    }
                    else
                    {
                        _fadeState = FadeState.None;
                    }
                }
                break;
        }

        if (_sceneStack.Count > 0)
            _sceneStack.Peek().Update(gameTime);
        else
            _currentScene?.Update(gameTime);
    }

    /// <summary>Draws the scene hierarchy.
    /// When overlays are stacked: draws <see cref="Scene.CurrentScene"/> first (if the top overlay is an overlay scene),
    /// then all overlays from bottom to top. Only the topmost scene receives <see cref="Update"/>.</summary>
    public void Draw(GameTime gameTime)
    {
        if (_sceneStack.Count > 0)
        {
            if (_sceneStack.Peek().IsOverlay)
                _currentScene?.Draw(gameTime);

            int count = _sceneStack.Count;
            _sceneStack.CopyTo(_drawBuffer, 0); // index 0 = top, index count-1 = bottom
            for (int i = count - 1; i >= 0; i--)
                _drawBuffer[i].Draw(gameTime);
        }
        else
        {
            _currentScene?.Draw(gameTime);
        }
    }

    /// <summary>Draws a full-screen black overlay at the current fade alpha.
    /// Must be called explicitly by the game after base.Draw().</summary>
    public void DrawFadeOverlay(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Texture2D texture)
    {
        if (_fadeAlpha <= 0f) return;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        spriteBatch.Draw(texture, graphicsDevice.Viewport.Bounds, Color.Black * _fadeAlpha);
        spriteBatch.End();
    }

    private void ApplyPendingChange()
    {
        _currentScene?.UnloadContent();

        _currentScene = _queuedScene!;
        _queuedScene = null;

        SetupAndStartScene(_currentScene);
    }

    private static void SetupAndStartScene(Scene scene)
    {
        scene.Initialize();
    }
}
