namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>
/// A scene transition that performs a circular wipe effect.
/// When a <paramref name="circleShader"/> is provided the wipe is performed via the shader (circle opens/closes).
/// When no shader is available the transition degrades gracefully to a simple fade.
/// </summary>
public sealed class CircleWipeTransition : ISceneTransition
{
    private readonly Texture2D _pixel;
    private readonly Effect? _circleShader;
    private readonly Color _maskColor;

    private float _progress;
    private float _timer;
    private float _duration;
    private bool _closingCircle;

    /// <inheritdoc/>
    public bool IsTransitionOutComplete => _closingCircle && _progress >= 1f;

    /// <inheritdoc/>
    public bool IsTransitionInComplete => !_closingCircle && _progress <= 0f;

    /// <summary>
    /// Creates a new <see cref="CircleWipeTransition"/>.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white texture used for the fallback fade overlay.</param>
    /// <param name="circleShader">
    /// Optional shader that implements the circle-wipe effect via a <c>float Progress</c> parameter.
    /// When <see langword="null"/> the transition falls back to a simple fade.
    /// </param>
    /// <param name="maskColor">Mask / overlay color. Defaults to <see cref="Color.Black"/> when <see langword="null"/>.</param>
    public CircleWipeTransition(Texture2D pixelTexture, Effect? circleShader = null, Color? maskColor = null)
    {
        _pixel = pixelTexture;
        _circleShader = circleShader;
        _maskColor = maskColor ?? Color.Black;
        _closingCircle = true;
    }

    /// <inheritdoc/>
    public void BeginTransitionOut(float durationSeconds)
    {
        _closingCircle = true;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 0f;
    }

    /// <inheritdoc/>
    public void BeginTransitionIn(float durationSeconds)
    {
        _closingCircle = false;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 1f;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        float t = _duration > 0f ? Math.Min(1f, _timer / _duration) : 1f;
        _progress = _closingCircle ? t : 1f - t;
    }

    /// <inheritdoc/>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_progress <= 0f) return;

        if (_circleShader is not null)
        {
            _circleShader.Parameters["Progress"]?.SetValue(_progress);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, _circleShader);
            spriteBatch.Draw(_pixel, viewport.Bounds, Color.White);
            spriteBatch.End();
        }
        else
        {
            // Fallback: simple fade overlay
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            spriteBatch.Draw(_pixel, viewport.Bounds, _maskColor * _progress);
            spriteBatch.End();
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _progress = 0f;
        _timer = 0f;
        _closingCircle = true;
    }
}
