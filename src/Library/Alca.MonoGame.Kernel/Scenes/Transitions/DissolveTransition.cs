namespace Alca.MonoGame.Kernel.Scenes.Transitions;

/// <summary>
/// A scene transition that dissolves the screen using a noise texture and a dissolve shader.
/// When no shader is provided the transition degrades gracefully to a simple fade.
/// </summary>
public sealed class DissolveTransition : ISceneTransition
{
    private readonly Texture2D _pixel;
    private readonly Effect? _dissolveShader;
    private readonly Texture2D? _noiseTexture;

    private float _progress;
    private float _timer;
    private float _duration;
    private bool _dissolving;

    /// <inheritdoc/>
    public bool IsTransitionOutComplete => _dissolving && _progress >= 1f;

    /// <inheritdoc/>
    public bool IsTransitionInComplete => !_dissolving && _progress <= 0f;

    /// <summary>
    /// Creates a new <see cref="DissolveTransition"/>.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white texture used for the fallback fade overlay.</param>
    /// <param name="dissolveShader">
    /// Optional shader implementing the dissolve effect. Expected parameters: <c>float Progress</c>, <c>Texture2D NoiseSampler</c>.
    /// When <see langword="null"/> the transition falls back to a simple fade.
    /// </param>
    /// <param name="noiseTexture">
    /// Optional noise texture fed to the shader as a dissolve mask. Ignored when <paramref name="dissolveShader"/> is <see langword="null"/>.
    /// </param>
    public DissolveTransition(Texture2D pixelTexture, Effect? dissolveShader = null, Texture2D? noiseTexture = null)
    {
        _pixel = pixelTexture;
        _dissolveShader = dissolveShader;
        _noiseTexture = noiseTexture;
        _dissolving = true;
    }

    /// <inheritdoc/>
    public void BeginTransitionOut(float durationSeconds)
    {
        _dissolving = true;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 0f;
    }

    /// <inheritdoc/>
    public void BeginTransitionIn(float durationSeconds)
    {
        _dissolving = false;
        _timer = 0f;
        _duration = durationSeconds;
        _progress = 1f;
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        _timer += deltaTime;
        float t = _duration > 0f ? Math.Min(1f, _timer / _duration) : 1f;
        _progress = _dissolving ? t : 1f - t;
    }

    /// <inheritdoc/>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_progress <= 0f) return;

        if (_dissolveShader is not null)
        {
            _dissolveShader.Parameters["Progress"]?.SetValue(_progress);
            if (_noiseTexture is not null)
                _dissolveShader.Parameters["NoiseSampler"]?.SetValue(_noiseTexture);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, null, null, _dissolveShader);
            spriteBatch.Draw(_pixel, viewport.Bounds, Color.White);
            spriteBatch.End();
        }
        else
        {
            // Fallback: simple fade overlay
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            spriteBatch.Draw(_pixel, viewport.Bounds, Color.Black * _progress);
            spriteBatch.End();
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _progress = 0f;
        _timer = 0f;
        _dissolving = true;
    }
}
