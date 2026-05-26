using Alca.MonoGame.Kernel.Tweening;
using MonoGame.Extended.Tweening;

namespace Alca.MonoGame.Kernel.Graphics.Camera;

/// <summary>
/// Per-camera helper that provides screen-shake, zoom, and pan animations.
/// Create one instance per camera that needs effects; call <see cref="Update"/> each frame.
/// </summary>
public sealed class CameraEffects
{
    // Two coprime frequencies for a natural-looking Lissajous-style shake.
    private const float FreqA = 37f;
    private const float FreqB = 59f;

    private float _shakeElapsed;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector2 _shakeOffset;
    private Camera2D? _shakeCamera;

    private bool _isPanning;

    /// <summary>Gets a value indicating whether a shake effect is currently running.</summary>
    public bool IsShaking => _shakeElapsed < _shakeDuration;

    /// <summary>Gets a value indicating whether a pan animation is currently running.</summary>
    public bool IsPanning => _isPanning;

    /// <summary>
    /// Begins a screen-shake effect on <paramref name="camera"/>.
    /// </summary>
    /// <param name="camera">Camera to shake.</param>
    /// <param name="magnitude">Maximum displacement in world units.</param>
    /// <param name="duration">Total shake duration in seconds.</param>
    public void Shake(Camera2D camera, float magnitude, float duration)
    {
        if (IsShaking && _shakeCamera == camera)
            camera.Position -= _shakeOffset;

        _shakeCamera = camera;
        _shakeMagnitude = magnitude;
        _shakeDuration = duration;
        _shakeElapsed = 0f;
        _shakeOffset = Vector2.Zero;
    }

    /// <summary>Tweens the camera's <see cref="Camera2D.Zoom"/> to <paramref name="targetZoom"/> over <paramref name="duration"/> seconds.</summary>
    public Tween ZoomTo(Camera2D camera, float targetZoom, float duration, Func<float, float>? easing = null)
        => Core.Tweening.TweenTo(camera, c => c.Zoom, targetZoom, duration,
            easing ?? EasingCatalog.Linear);

    /// <summary>Tweens the camera's <see cref="Camera2D.Position"/> to <paramref name="target"/> over <paramref name="duration"/> seconds.</summary>
    public void PanTo(Camera2D camera, Vector2 target, float duration, Func<float, float>? easing = null)
    {
        var ease = easing ?? EasingCatalog.Linear;
        _isPanning = true;
        Core.Tweening.TweenTo(camera, c => c.Position.X, target.X, duration, ease);
        Core.Tweening.TweenTo(camera, c => c.Position.Y, target.Y, duration, ease)
            .OnEnd(_ => _isPanning = false);
    }

    /// <summary>Advances the shake timer and applies the oscillating offset to the camera.</summary>
    public void Update(GameTime gameTime)
    {
        if (!IsShaking || _shakeCamera is null)
        {
            _shakeOffset = Vector2.Zero;
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _shakeElapsed += dt;

        if (!IsShaking)
        {
            _shakeCamera.Position -= _shakeOffset;
            _shakeOffset = Vector2.Zero;
            return;
        }

        float t = _shakeElapsed;
        float intensity = 1f - (_shakeElapsed / _shakeDuration);
        var newOffset = new Vector2(
            MathF.Sin(t * FreqA) * _shakeMagnitude * intensity,
            MathF.Sin(t * FreqB) * _shakeMagnitude * intensity);

        _shakeCamera.Position += newOffset - _shakeOffset;
        _shakeOffset = newOffset;
    }
}
