namespace Alca.MonoGame.Kernel.UnitTests.Fixtures;

/// <summary>Shared xunit fixture that provides a real <see cref="GraphicsDevice"/> backed by a headless MonoGame instance.</summary>
/// <remarks>
/// Bootstraps SDL2+OpenGL by creating a 1x1 offscreen window, running one frame, then holding the live device.
/// The window appears and disappears immediately during test collection initialization.
/// Shared via <see cref="ICollectionFixture{T}"/> — one instance per xunit collection.
/// </remarks>
public sealed class GraphicsDeviceFixture : IDisposable
{
    private readonly HeadlessGame _game;

    /// <summary>Gets the live <see cref="GraphicsDevice"/>.</summary>
    public GraphicsDevice GraphicsDevice => _game.GraphicsDevice;

    /// <summary>Gets a <see cref="SpriteBatch"/> bound to <see cref="GraphicsDevice"/>.</summary>
    public SpriteBatch SpriteBatch { get; }

    public GraphicsDeviceFixture()
    {
        _game = new HeadlessGame();
        _game.Run();
        SpriteBatch = new SpriteBatch(_game.GraphicsDevice);
    }

    public void Dispose()
    {
        SpriteBatch.Dispose();
        _game.Dispose();
    }

    private sealed class HeadlessGame : Game
    {
        private readonly GraphicsDeviceManager _gdm;
        private bool _exiting;

        public HeadlessGame()
        {
            _gdm = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1,
                PreferredBackBufferHeight = 1,
            };
            IsFixedTimeStep = false;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_exiting)
                return;

            _exiting = true;
            Exit();
        }
    }
}
