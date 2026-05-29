namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>
/// A scene transition that slides a solid-color curtain across the screen to cover and reveal scenes.
/// During transition-out the curtain slides in from the specified edge until it covers the viewport entirely.
/// During transition-in the curtain slides back out, uncovering the new scene.
/// </summary>
public sealed class SlideTransition : ISceneTransition
{
    private readonly Texture2D _pixel;
    private readonly Color _curtainColor;
    private readonly SlideDirection _direction;

    private float _progress;
    private float _timer;
    private float _duration;
    private bool _slidingOut;

    /// <inheritdoc/>
    public bool IsTransitionOutComplete => _slidingOut && _progress >= 1f;

    /// <inheritdoc/>
    public bool IsTransitionInComplete => !_slidingOut && _progress <= 0f;

    /// <summary>
    /// Creates a new <see cref="SlideTransition"/>.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white texture used to draw the curtain.</param>
    /// <param name="direction">The edge from which the curtain enters the screen.</param>
    /// <param name="curtainColor">Color of the sliding curtain. Defaults to <see cref="Color.Black"/> when <see langword="null"/>.</param>
    public SlideTransition(Texture2D pixelTexture, SlideDirection direction = SlideDirection.Left, Color? curtainColor = null)
    {
        _pixel = pixelTexture;
        _direction = direction;
        _curtainColor = curtainColor ?? Color.Black;
        _slidingOut = true;
        _progress = 0f;
    }

    /// <inheritdoc/>
    public void BeginTransitionOut(float durationSeconds)
    {
        _slidingOut = true;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 0f;
    }

    /// <inheritdoc/>
    public void BeginTransitionIn(float durationSeconds)
    {
        _slidingOut = false;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 1f;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        float t = _duration > 0f ? Math.Min(1f, _timer / _duration) : 1f;
        _progress = _slidingOut ? t : 1f - t;
    }

    /// <inheritdoc/>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_progress <= 0f) return;

        int vw = viewport.Width;
        int vh = viewport.Height;
        Rectangle curtain;

        switch (_direction)
        {
            case SlideDirection.Left:
                curtain = new Rectangle(0, 0, (int)(vw * _progress), vh);
                break;
            case SlideDirection.Right:
                int rWidth = (int)(vw * _progress);
                curtain = new Rectangle(vw - rWidth, 0, rWidth, vh);
                break;
            case SlideDirection.Up:
                curtain = new Rectangle(0, 0, vw, (int)(vh * _progress));
                break;
            case SlideDirection.Down:
                int dHeight = (int)(vh * _progress);
                curtain = new Rectangle(0, vh - dHeight, vw, dHeight);
                break;
            default:
                curtain = new Rectangle(0, 0, (int)(vw * _progress), vh);
                break;
        }

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        spriteBatch.Draw(_pixel, curtain, _curtainColor);
        spriteBatch.End();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _progress = 0f;
        _timer = 0f;
        _slidingOut = true;
    }
}
