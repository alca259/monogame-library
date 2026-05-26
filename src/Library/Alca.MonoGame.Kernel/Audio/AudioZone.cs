using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Audio;

/// <summary>
/// GameBehaviour that defines a spherical audio zone for ambient sounds.
/// All three axes (X, Y, Z) are used to compute distance from the listener.
/// When the listener enters the zone the sound fades in; when it exits, it fades out.
/// </summary>
public sealed class AudioZone : GameBehaviour
{
    private AudioController? _controller;
    private SoundEffectInstance? _instance;
    private float _currentVolume;

    /// <summary>Gets or sets the ambient sound to play in loop inside the zone.</summary>
    public SoundEffect? AmbientSound { get; set; }

    /// <summary>Gets or sets the radius of the zone in world units. Default is 50.</summary>
    public float Radius { get; set; } = 50f;

    /// <summary>Gets or sets the fade-in duration in seconds when entering the zone. Default is 1.</summary>
    public float FadeInTime { get; set; } = 1f;

    /// <summary>Gets or sets the fade-out duration in seconds when leaving the zone. Default is 1.</summary>
    public float FadeOutTime { get; set; } = 1f;

    /// <summary>Gets or sets the mixer channel for independent volume routing.</summary>
    public AudioMixerChannel? MixerChannel { get; set; }

    /// <inheritdoc/>
    public override void Awake()
    {
        _controller = Entity.World.AudioController;
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (_controller is null || AmbientSound is null) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 zonePosition = Entity.Transform.Position;
        Vector3 listenerPosition = _controller.ListenerPosition;
        float distance = Vector3.Distance(zonePosition, listenerPosition);
        bool inZone = distance <= Radius;

        if (inZone)
            EnsurePlaying();

        float targetVolume = inZone ? ComputeVolumeAtDistance(distance) : 0f;
        float fadeTime = targetVolume > _currentVolume ? FadeInTime : FadeOutTime;
        float fadeSpeed = fadeTime > 0f ? deltaTime / fadeTime : 1f;

        if (Math.Abs(_currentVolume - targetVolume) > 0.0001f)
        {
            float direction = targetVolume > _currentVolume ? 1f : -1f;
            _currentVolume = Math.Clamp(_currentVolume + direction * fadeSpeed, 0f, 1f);
        }
        else
        {
            _currentVolume = targetVolume;
        }

        ApplyVolume();

        if (!inZone && _currentVolume <= 0f)
            StopInstance();
    }

    /// <inheritdoc/>
    public override void OnDestroy() => StopInstance();

    private float ComputeVolumeAtDistance(float distance)
    {
        if (Radius <= 0f || distance >= Radius) return 0f;
        return 1f - (distance / Radius);
    }

    private void EnsurePlaying()
    {
        if (_instance is not null && _instance.State == SoundState.Playing) return;

        _instance?.Dispose();
        _instance = AmbientSound!.CreateInstance();
        _instance.IsLooped = true;
        _instance.Volume = 0f;
        _instance.Play();
    }

    private void ApplyVolume()
    {
        if (_instance is null) return;
        float channelVolume = MixerChannel is not null ? MixerChannel.EffectiveVolume : 1f;
        _instance.Volume = Math.Clamp(_currentVolume * channelVolume, 0f, 1f);
    }

    private void StopInstance()
    {
        if (_instance is null) return;
        _instance.Stop();
        _instance.Dispose();
        _instance = null;
        _currentVolume = 0f;
    }
}
