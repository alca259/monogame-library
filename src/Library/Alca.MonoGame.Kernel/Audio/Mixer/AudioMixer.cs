namespace Alca.MonoGame.Kernel.Audio.Mixer;

/// <summary>
/// Singleton mixer that manages named volume channels for independent audio routing.
/// Pre-allocates Master, Music, SFX, and Ambient channels.
/// </summary>
public sealed class AudioMixer
{
    private readonly Dictionary<string, AudioMixerChannel> _channels = new(8);

    /// <summary>The name of the master output channel.</summary>
    public const string MasterChannelName = "Master";

    /// <summary>The name of the music channel.</summary>
    public const string MusicChannelName = "Music";

    /// <summary>The name of the sound effects channel.</summary>
    public const string SfxChannelName = "SFX";

    /// <summary>The name of the ambient channel.</summary>
    public const string AmbientChannelName = "Ambient";

    /// <summary>Gets the master output channel.</summary>
    public AudioMixerChannel Master { get; }

    /// <summary>Gets the music channel.</summary>
    public AudioMixerChannel Music { get; }

    /// <summary>Gets the sound effects channel.</summary>
    public AudioMixerChannel Sfx { get; }

    /// <summary>Gets the ambient channel.</summary>
    public AudioMixerChannel Ambient { get; }

    /// <summary>Initializes a new <see cref="AudioMixer"/> with the four default channels.</summary>
    public AudioMixer()
    {
        Master = RegisterChannel(MasterChannelName);
        Music = RegisterChannel(MusicChannelName);
        Sfx = RegisterChannel(SfxChannelName);
        Ambient = RegisterChannel(AmbientChannelName);
    }

    /// <summary>
    /// Registers a new channel with the given name and initial volume.
    /// If the channel already exists, returns the existing one without modifying it.
    /// </summary>
    public AudioMixerChannel RegisterChannel(string name, float volume = 1f)
    {
        if (_channels.TryGetValue(name, out AudioMixerChannel? existing))
            return existing;

        AudioMixerChannel channel = new(name, volume);
        _channels[name] = channel;
        return channel;
    }

    /// <summary>Returns the channel with the given name, or null if it has not been registered.</summary>
    public AudioMixerChannel? GetChannel(string name)
    {
        _channels.TryGetValue(name, out AudioMixerChannel? channel);
        return channel;
    }

    /// <summary>Returns true if a channel with the given name has been registered.</summary>
    public bool HasChannel(string name) => _channels.ContainsKey(name);
}
