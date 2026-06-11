using Alca.MonoGame.Kernel.Audio.Mixer;
using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Audio.Spatial;

/// <summary>
/// GameBehaviour that emits 3D spatial audio from the entity's world-space position.
/// Syncs <see cref="AudioEmitter3D.Position"/> with <see cref="TransformBehaviour.Position"/> (X, Y, Z)
/// every frame and applies 3D positioning via <see cref="AudioController"/>.
/// </summary>
/// <example>
/// - Un NPC que habla.
/// - Un disparo.
/// - Efectos de entidad.
/// </example>
public sealed class SpatialAudioSourceBehaviour : GameBehaviour
{
    private readonly AudioEmitter3D _emitter = new();
    private AudioController? _controller;
    private SoundEffectInstance? _instance;

    /// <summary>Gets or sets the sound effect to emit from this source.</summary>
    public SoundEffect? Sound { get; set; }

    /// <summary>Gets or sets the base volume (0–1, default 1).</summary>
    public float Volume { get; set; } = 1f;

    /// <summary>Gets or sets the pitch adjustment (-1 to 1, default 0).</summary>
    public float Pitch { get; set; }

    /// <summary>Gets or sets whether playback loops.</summary>
    public bool Loop { get; set; }

    /// <summary>Gets or sets whether the sound starts playing when the entity awakens.</summary>
    public bool PlayOnAwake { get; set; }

    /// <summary>Gets or sets the mixer channel for independent volume routing. Null means no channel routing.</summary>
    public AudioMixerChannel? MixerChannel { get; set; }

    /// <summary>Gets the current playback state. Returns <see cref="SoundState.Stopped"/> when no instance is active.</summary>
    public SoundState State => _instance?.State ?? SoundState.Stopped;

    /// <inheritdoc/>
    public override void Awake()
    {
        _controller = Entity.World.AudioController;
        if (PlayOnAwake)
            Play();
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (_instance is null || _controller is null) return;
        if (_instance.State != SoundState.Playing) return;

        _emitter.Position = Entity.Transform.Position;
        _emitter.Velocity = Entity.Transform.Velocity;

        float effectiveVolume = MixerChannel is not null
            ? Volume * MixerChannel.EffectiveVolume
            : Volume;
        _instance.Volume = Math.Clamp(effectiveVolume, 0f, 1f);
        _controller.ApplySpatialAudio(_instance, _emitter);
    }

    /// <summary>Plays the assigned sound from the entity's current world position. Stops any active playback first.</summary>
    public void Play()
    {
        if (Sound is null || _controller is null) return;

        _instance?.Dispose();
        _instance = Sound.CreateInstance();
        _instance.IsLooped = Loop;
        _instance.Pitch = Pitch;

        float effectiveVolume = MixerChannel is not null
            ? Volume * MixerChannel.EffectiveVolume
            : Volume;
        _instance.Volume = Math.Clamp(effectiveVolume, 0f, 1f);
        _instance.Play();
    }

    /// <summary>Stops playback and disposes the current instance.</summary>
    public void Stop()
    {
        if (_instance is null) return;
        _instance.Stop();
        _instance.Dispose();
        _instance = null;
    }

    /// <summary>Pauses current playback.</summary>
    public void Pause() => _instance?.Pause();

    /// <summary>Resumes paused playback.</summary>
    public void Resume() => _instance?.Resume();

    /// <inheritdoc/>
    public override void OnDestroy() => Stop();
}
