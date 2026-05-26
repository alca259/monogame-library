namespace Alca.MonoGame.Kernel.Audio;

/// <summary>
/// Smoothly transitions between two audio tracks using a linear crossfade.
/// Call <see cref="Update"/> each frame. Dispose when done to free track instances.
/// </summary>
public sealed class AudioCrossfader : IDisposable
{
    private SoundEffectInstance? _trackA;
    private SoundEffectInstance? _trackB;

    private float _crossfadeTimer;
    private float _crossfadeDuration;
    private float _targetVolume;
    private bool _isCrossfading;
    private bool _isFadingOut;

    /// <summary>Gets a value indicating whether a crossfade is currently in progress.</summary>
    public bool IsCrossfading => _isCrossfading;

    /// <summary>Gets the volume of the currently dominant track (0–1).</summary>
    public float CurrentVolume => _trackA?.Volume ?? 0f;

    /// <summary>
    /// Begins a crossfade from the current track to <paramref name="newTrack"/>.
    /// If a crossfade is already running, it completes instantly before the new one starts.
    /// </summary>
    public void CrossfadeTo(SoundEffect newTrack, float duration, float targetVolume = 1f)
    {
        if (_isCrossfading)
            CompleteCurrentCrossfade();

        if (_trackA is not null)
        {
            _trackA.Stop();
            _trackA.Dispose();
        }

        _trackB = newTrack.CreateInstance();
        _trackB.Volume = 0f;
        _trackB.Play();

        _crossfadeTimer = 0f;
        _crossfadeDuration = MathF.Max(duration, 0.001f);
        _targetVolume = MathHelper.Clamp(targetVolume, 0f, 1f);
        _isCrossfading = true;
        _isFadingOut = false;

        // If trackA was null (first track), promote B immediately with a very short fade.
        if (_trackA is null)
        {
            _trackA = _trackB;
            _trackB = null;
            _trackA.Volume = 0f;
            _isCrossfading = true;
        }
    }

    /// <summary>
    /// Fades out the active track. Stops immediately when <paramref name="fadeOutDuration"/> &lt;= 0.
    /// </summary>
    public void Stop(float fadeOutDuration = 0f)
    {
        if (fadeOutDuration <= 0f)
        {
            _trackA?.Stop();
            _trackB?.Stop();
            _isCrossfading = false;
            return;
        }

        _crossfadeTimer = 0f;
        _crossfadeDuration = fadeOutDuration;
        _isCrossfading = true;
        _isFadingOut = true;
    }

    /// <summary>Advances the crossfade timer and adjusts track volumes. Call once per frame.</summary>
    public void Update(GameTime gameTime)
    {
        if (!_isCrossfading) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _crossfadeTimer += dt;

        float t = MathHelper.Clamp(_crossfadeTimer / _crossfadeDuration, 0f, 1f);

        if (_isFadingOut)
        {
            if (_trackA is not null) _trackA.Volume = _targetVolume > 0f
                ? MathHelper.Lerp(_targetVolume, 0f, t)
                : MathHelper.Lerp(1f, 0f, t);
        }
        else if (_trackA is not null && _trackB is not null)
        {
            float startVolA = _targetVolume;
            _trackA.Volume = MathHelper.Lerp(startVolA, 0f, t);
            _trackB.Volume = MathHelper.Lerp(0f, _targetVolume, t);
        }

        if (t >= 1f)
        {
            if (_isFadingOut)
            {
                _trackA?.Stop();
            }
            else if (_trackB is not null)
            {
                _trackA?.Stop();
                _trackA?.Dispose();
                _trackA = _trackB;
                _trackB = null;
                _trackA.Volume = _targetVolume;
            }

            _isCrossfading = false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _trackA?.Stop();
        _trackA?.Dispose();
        _trackA = null;

        _trackB?.Stop();
        _trackB?.Dispose();
        _trackB = null;

        _isCrossfading = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void CompleteCurrentCrossfade()
    {
        if (_trackB is not null)
        {
            _trackA?.Stop();
            _trackA?.Dispose();
            _trackA = _trackB;
            _trackA.Volume = _targetVolume;
            _trackB = null;
        }

        _isCrossfading = false;
    }
}
