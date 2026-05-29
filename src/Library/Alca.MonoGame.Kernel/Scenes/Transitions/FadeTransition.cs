namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>
/// A scene transition that fades the screen to a solid color and back.
/// Requires a 1×1 white <see cref="Texture2D"/> passed at construction time.
/// </summary>
public sealed class FadeTransition : ISceneTransition
{
    private readonly Texture2D _pixel;
    private readonly Color _fadeColor;

    private float _alpha;
    private float _timer;
    private float _duration;
    private bool _fadingOut;

    /// <inheritdoc/>
    public bool IsTransitionOutComplete => _fadingOut && _alpha >= 1f;

    /// <inheritdoc/>
    public bool IsTransitionInComplete => !_fadingOut && _alpha <= 0f;

    /// <summary>
    /// Creates a new <see cref="FadeTransition"/>.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white texture used to render the full-screen overlay.</param>
    /// <param name="fadeColor">Color to fade to/from. Defaults to <see cref="Color.Black"/> when <see langword="null"/>.</param>
    public FadeTransition(Texture2D pixelTexture, Color? fadeColor = null)
    {
        _pixel = pixelTexture;
        _fadeColor = fadeColor ?? Color.Black;
        _fadingOut = true;
    }

    /// <inheritdoc/>
    public void BeginTransitionOut(float durationSeconds)
    {
        _fadingOut = true;
        _timer = 0f;
        _duration = durationSeconds;
        _alpha = 0f;
    }

    /// <inheritdoc/>
    public void BeginTransitionIn(float durationSeconds)
    {
        _fadingOut = false;
        _timer = 0f;
        _duration = durationSeconds;
        _alpha = 1f;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        float t = _duration > 0f ? Math.Min(1f, _timer / _duration) : 1f;

        _alpha = _fadingOut ? t : 1f - t;
    }

    /// <inheritdoc/>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_alpha <= 0f) return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        spriteBatch.Draw(_pixel, viewport.Bounds, _fadeColor * _alpha);
        spriteBatch.End();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _alpha = 0f;
        _timer = 0f;
        _fadingOut = true;
    }
}
