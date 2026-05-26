namespace Alca.MonoGame.Kernel.Audio;

public sealed class AudioController : IDisposable
{
    /// <summary> Tracks sound effect instances created so they can be paused, unpaused, and/or disposed.</summary>
    private readonly List<SoundEffectInstance> _activeSoundEffectInstances = [];

    private readonly AudioListener3D _listener = new();

    /// <summary> Tracks the volume for song playback when muting and unmuting.</summary>
    private float _previousSongVolume;

    /// <summary> Tracks the volume for sound effect playback when muting and unmuting.</summary>
    private float _previousSoundEffectVolume;

    /// <summary>Gets a value that indicates if audio is muted.</summary>
    public bool IsMuted { get; private set; }

    /// <summary>Gets or Sets the global volume of songs.</summary>
    /// <remarks>If IsMuted is true, the getter will always return back 0.0f and the setter will ignore setting the volume.</remarks>
    public float SongVolume
    {
        get => IsMuted ? 0.0f : MediaPlayer.Volume;
        set
        {
            if (IsMuted)
            {
                return;
            }

            MediaPlayer.Volume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    /// <summary>Gets or Sets the global volume of sound effects.</summary>
    /// <remarks>If IsMuted is true, the getter will always return back 0.0f and the setter will ignore setting the volume.</remarks>
    public float SoundEffectVolume
    {
        get => IsMuted ? 0.0f : SoundEffect.MasterVolume;
        set
        {
            if (IsMuted)
            {
                return;
            }

            SoundEffect.MasterVolume = Math.Clamp(value, 0.0f, 1.0f);
        }
    }

    /// <summary>Gets a value that indicates if this audio controller has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Finalizer called when object is collected by the garbage collector.</summary>
    ~AudioController() => Dispose(false);

    /// <summary>Updates this audio controller.</summary>
    public void Update()
    {
        for (int i = _activeSoundEffectInstances.Count - 1; i >= 0; i--)
        {
            SoundEffectInstance instance = _activeSoundEffectInstances[i];

            if (instance.State == SoundState.Stopped)
            {
                if (!instance.IsDisposed)
                {
                    instance.Dispose();
                }
                _activeSoundEffectInstances.RemoveAt(i);
            }
        }
    }

    /// <summary>Plays the given sound effect with the specified properties.</summary>
    /// <param name="soundEffect">The sound effect to play.</param>
    /// <param name="volume">The volume, ranging from 0.0 (silence) to 1.0 (full volume).</param>
    /// <param name="pitch">The pitch adjustment, ranging from -1.0 (down an octave) to 0.0 (no change) to 1.0 (up an octave).</param>
    /// <param name="pan">The panning, ranging from -1.0 (left speaker) to 0.0 (centered), 1.0 (right speaker).</param>
    /// <param name="isLooped">Whether the the sound effect should loop after playback.</param>
    /// <returns>The sound effect instance created by playing the sound effect.</returns>
    /// <returns>The sound effect instance created by this method.</returns>
    public SoundEffectInstance PlaySoundEffect(SoundEffect soundEffect, float volume = 1.0f, float pitch = 1.0f, float pan = 0.0f, bool isLooped = false)
    {
        // Create an instance from the sound effect given.
        SoundEffectInstance soundEffectInstance = soundEffect.CreateInstance();

        // Apply the volume, pitch, pan, and loop values specified.
        soundEffectInstance.Volume = volume;
        soundEffectInstance.Pitch = pitch;
        soundEffectInstance.Pan = pan;
        soundEffectInstance.IsLooped = isLooped;

        // Tell the instance to play
        soundEffectInstance.Play();

        // Add it to the active instances for tracking
        _activeSoundEffectInstances.Add(soundEffectInstance);

        return soundEffectInstance;
    }

    /// <summary>Plays the given song.</summary>
    /// <param name="song">The song to play.</param>
    /// <param name="isRepeating">Optionally specify if the song should repeat. Default is true.</param>
    public static void PlaySong(Song song, bool isRepeating = true)
    {
        // Check if the media player is already playing, if so, stop it.
        // If we do not stop it, this could cause issues on some platforms
        if (MediaPlayer.State == MediaState.Playing)
        {
            MediaPlayer.Stop();
        }

        MediaPlayer.Play(song);
        MediaPlayer.IsRepeating = isRepeating;
    }

    /// <summary>Pauses all audio.</summary>
    public void PauseAudio()
    {
        // Pause any active songs playing.
        MediaPlayer.Pause();

        // Pause any active sound effects.
        foreach (SoundEffectInstance soundEffectInstance in _activeSoundEffectInstances)
        {
            soundEffectInstance.Pause();
        }
    }

    /// <summary>Resumes play of all previous paused audio.</summary>
    public void ResumeAudio()
    {
        // Resume paused music
        MediaPlayer.Resume();

        // Resume any active sound effects.
        foreach (SoundEffectInstance soundEffectInstance in _activeSoundEffectInstances)
        {
            soundEffectInstance.Resume();
        }
    }

    /// <summary>Mutes all audio.</summary>
    public void MuteAudio()
    {
        // Store the volume so they can be restored during ResumeAudio
        _previousSongVolume = MediaPlayer.Volume;
        _previousSoundEffectVolume = SoundEffect.MasterVolume;

        // Set all volumes to 0
        MediaPlayer.Volume = 0.0f;
        SoundEffect.MasterVolume = 0.0f;

        IsMuted = true;
    }

    /// <summary>Unmutes all audio to the volume level prior to muting.</summary>
    public void UnmuteAudio()
    {
        // Restore the previous volume values.
        MediaPlayer.Volume = _previousSongVolume;
        SoundEffect.MasterVolume = _previousSoundEffectVolume;

        IsMuted = false;
    }

    /// <summary>Toggles the current audio mute state.</summary>
    public void ToggleMute()
    {
        if (IsMuted)
        {
            UnmuteAudio();
        }
        else
        {
            MuteAudio();
        }
    }

    /// <summary>Creates a pre-allocated sound effect pool with the given capacity for high-frequency playback.</summary>
    public static SoundEffectPool CreatePool(SoundEffect effect, int capacity)
    {
        return new SoundEffectPool(effect, capacity);
    }

    /// <summary>Gets the world-space position of the current 3D audio listener.</summary>
    public Vector3 ListenerPosition => _listener.Position;

    /// <summary>Updates the 3D listener position and forward direction for spatial audio calculations.</summary>
    public void UpdateListener(Vector3 position, Vector3 forward)
    {
        _listener.Update(position, forward);
    }

    /// <summary>Applies 3D spatial positioning to the given sound instance using the current listener and the provided emitter.</summary>
    public void ApplySpatialAudio(SoundEffectInstance instance, AudioEmitter3D emitter)
    {
        emitter.Apply3D(instance, _listener);
    }

    /// <summary>Disposes of this audio controller and cleans up resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes this audio controller and cleans up resources.</summary>
    /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
    private void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (SoundEffectInstance soundEffectInstance in _activeSoundEffectInstances)
            {
                soundEffectInstance.Dispose();
            }
            _activeSoundEffectInstances.Clear();
        }

        IsDisposed = true;
    }
}

