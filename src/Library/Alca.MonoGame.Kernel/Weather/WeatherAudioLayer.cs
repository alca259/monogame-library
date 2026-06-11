using Alca.MonoGame.Kernel.Audio.Mixer;
using Alca.MonoGame.Kernel.Audio.Spatial;

namespace Alca.MonoGame.Kernel.Weather;

/// <summary>
/// Manages three looping ambient tracks (rain, wind, thunder) and a pre-allocated pool of
/// spatial thunder-strike instances for <see cref="LightningController"/>.
/// Volume targets are read from <see cref="WeatherProfile"/> each frame; actual volumes
/// interpolate toward the target at <see cref="FadeSpeed"/> units per second — no allocations in <see cref="Update"/>.
/// Call <see cref="LoadSounds"/> once, then call <see cref="Update"/> each frame.
/// Dispose when no longer needed.
/// </summary>
public sealed class WeatherAudioLayer : IDisposable
{
    private SoundEffectInstance? _rainInstance;
    private SoundEffectInstance? _windInstance;
    private SoundEffectInstance? _thunderInstance;
    private SoundEffectInstance[]? _strikePool;
    private int _strikePoolIndex;
    private readonly AudioEmitter3D _strikeEmitter = new();

    private float _currentRainVolume;
    private float _currentWindVolume;
    private float _currentThunderVolume;

    private bool _disposed;

    #region Configuration
    /// <summary>Gets or sets the volume fade speed in units per second. Default 1 (full fade in 1 second).</summary>
    public float FadeSpeed { get; set; } = 1f;

    /// <summary>Gets or sets the mixer channel all weather audio is routed through. Null means no channel routing.</summary>
    public AudioMixerChannel? Channel { get; set; }
    #endregion

    #region Content loading
    /// <summary>
    /// Creates looping instances for ambient tracks and pre-allocates the strike pool.
    /// Must be called once before the first <see cref="Update"/>.
    /// Any of the <see cref="SoundEffect"/> parameters may be <see langword="null"/>; that track will be silent.
    /// </summary>
    /// <param name="rain">Looping rain ambient sound.</param>
    /// <param name="wind">Looping wind ambient sound.</param>
    /// <param name="thunderAmbient">Looping distant thunder ambient sound.</param>
    /// <param name="thunderStrike">One-shot thunder strike sound for spatial playback. Used by the strike pool.</param>
    /// <param name="strikePoolSize">Number of pre-allocated concurrent strike instances. Default 4.</param>
    public void LoadSounds(
        SoundEffect? rain,
        SoundEffect? wind,
        SoundEffect? thunderAmbient,
        SoundEffect? thunderStrike,
        int strikePoolSize = 4)
    {
        _rainInstance    = CreateLoop(rain);
        _windInstance    = CreateLoop(wind);
        _thunderInstance = CreateLoop(thunderAmbient);

        if (thunderStrike is not null && strikePoolSize > 0)
        {
            _strikePool = new SoundEffectInstance[strikePoolSize];
            for (int i = 0; i < strikePoolSize; i++)
                _strikePool[i] = thunderStrike.CreateInstance();
        }
    }
    #endregion

    #region Game loop
    /// <summary>
    /// Interpolates current volumes toward the targets in <paramref name="profile"/>,
    /// applies optional channel routing, and ensures looping instances are playing or paused.
    /// No heap allocations.
    /// </summary>
    public void Update(GameTime gameTime, in WeatherProfile profile, Audio.AudioController audioController)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float masterVolume = audioController.Master.EffectiveVolume;
        float channelVolume = Channel?.EffectiveVolume ?? 1f;
        float effectiveVolume = masterVolume * channelVolume;

        _currentRainVolume    = ApproachVolume(_currentRainVolume,    profile.RainVolume,    dt);
        _currentWindVolume    = ApproachVolume(_currentWindVolume,    profile.WindVolume,    dt);
        _currentThunderVolume = ApproachVolume(_currentThunderVolume, profile.ThunderVolume, dt);

        SetInstanceVolume(_rainInstance,    _currentRainVolume,    effectiveVolume);
        SetInstanceVolume(_windInstance,    _currentWindVolume,    effectiveVolume);
        SetInstanceVolume(_thunderInstance, _currentThunderVolume, effectiveVolume);
    }
    #endregion

    #region Lightning audio
    /// <summary>
    /// Plays a spatial thunder strike sound at <paramref name="strikePosition"/> using
    /// a round-robin pre-allocated instance from the pool.
    /// No heap allocations. No-op if the pool was not initialized.
    /// </summary>
    public void PlayThunderStrike(Vector2 strikePosition, Audio.AudioController audioController)
    {
        if (_strikePool is null || _strikePool.Length == 0) return;

        SoundEffectInstance instance = _strikePool[_strikePoolIndex];
        _strikePoolIndex = (_strikePoolIndex + 1) % _strikePool.Length;

        if (instance.State == SoundState.Playing)
            instance.Stop();

        _strikeEmitter.Position = new Vector3(strikePosition, 0f);
        audioController.ApplySpatialAudio(instance, _strikeEmitter);

        float masterVolume = audioController.Master.EffectiveVolume;
        float channelVolume = Channel?.EffectiveVolume ?? 1f;
        instance.Volume = Math.Clamp(masterVolume * channelVolume, 0f, 1f);
        instance.Play();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        DisposeInstance(_rainInstance);
        DisposeInstance(_windInstance);
        DisposeInstance(_thunderInstance);
        if (_strikePool is not null)
            for (int i = 0; i < _strikePool.Length; i++)
                _strikePool[i].Dispose();
        _disposed = true;
    }
    #endregion

    #region Private helpers
    private float ApproachVolume(float current, float target, float dt)
    {
        float diff = target - current;
        float step = FadeSpeed * dt;
        return Math.Abs(diff) <= step ? target : current + Math.Sign(diff) * step;
    }

    private void SetInstanceVolume(SoundEffectInstance? instance, float volume, float channelVolume)
    {
        if (instance is null) return;

        float effective = Math.Clamp(volume * channelVolume, 0f, 1f);
        instance.Volume = effective;

        if (effective > 0.001f && instance.State != SoundState.Playing)
            instance.Play();
        else if (effective <= 0.001f && instance.State == SoundState.Playing)
            instance.Pause();
    }

    private static SoundEffectInstance? CreateLoop(SoundEffect? sfx)
    {
        if (sfx is null) return null;
        var inst = sfx.CreateInstance();
        inst.IsLooped = true;
        inst.Volume = 0f;
        return inst;
    }

    private static void DisposeInstance(SoundEffectInstance? instance)
    {
        if (instance is null) return;
        instance.Stop();
        instance.Dispose();
    }
    #endregion
}
