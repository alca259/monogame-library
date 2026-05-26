using Alca.MonoGame.Kernel.Content;

namespace Alca.MonoGame.Kernel.Scenes;

/// <summary>Scene that displays a loading progress bar while assets are loaded asynchronously.</summary>
public sealed class LoadingScene : Scene
{
    private const int BarHeight = 20;

    private readonly AsyncContentLoader _loader = new();

    private ContentLoadGroup? _group;
    private Scene? _nextScene;
    private Task? _loadTask;
    private volatile float _progress;
    private Texture2D? _pixel;

    /// <summary>
    /// Configures the group of assets to load and the scene to transition to on completion.
    /// Must be called before the scene is activated.
    /// </summary>
    public void Configure(ContentLoadGroup group, Scene nextScene)
    {
        _group = group;
        _nextScene = nextScene;
    }

    /// <inheritdoc/>
    public override void LoadContent()
    {
        base.LoadContent();
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        if (_group is null || _nextScene is null) return;

        var progress = new Progress<float>(p => _progress = p);
        _loadTask = _group.LoadAllAsync(_loader, progress, _loader.Token);
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        _loader.FlushPending(Content);

        if (_loadTask is not null && _loadTask.IsCompleted)
        {
            if (_loadTask.IsFaulted)
                throw _loadTask.Exception!.GetBaseException();

            Core.SceneManager.RequestChange(_nextScene!);
        }
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        if (_pixel is null) return;

        Viewport viewport = Core.GraphicsDevice.Viewport;
        int barWidth = viewport.Width / 2;
        int barX = (viewport.Width - barWidth) / 2;
        int barY = viewport.Height / 2;

        Core.SpriteBatch.Begin();
        Core.SpriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, BarHeight), Color.DarkGray);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(barWidth * _progress), BarHeight), Color.LimeGreen);
        Core.SpriteBatch.End();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loader.Dispose();
            _pixel?.Dispose();
        }

        base.Dispose(disposing);
    }
}
