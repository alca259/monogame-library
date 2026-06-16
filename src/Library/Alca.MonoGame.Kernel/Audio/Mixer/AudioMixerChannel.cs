namespace Alca.MonoGame.Kernel.Audio.Mixer;

/// <summary>Named volume channel in an <see cref="AudioMixer"/>. Controls volume and mute state independently.</summary>
public sealed class AudioMixerChannel
{
    private float _volume;

    /// <summary>Gets the channel name.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the volume of this channel, clamped to the range [0, 1].</summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Gets or sets a value indicating whether this channel is muted.</summary>
    public bool Muted { get; set; }

    /// <summary>Gets the effective playback volume: <see cref="Volume"/> when not muted, 0 when muted.</summary>
    public float EffectiveVolume => Muted ? 0f : _volume;

    /// <summary>Initializes a new <see cref="AudioMixerChannel"/> with the given name and initial volume.</summary>
    public AudioMixerChannel(string name, float volume = 1f)
    {
        Name = name;
        _volume = Math.Clamp(volume, 0f, 1f);
    }
}
