namespace Alca.MonoGame.Kernel.Audio.Utilities;

/// <summary>Pre-allocated pool of SoundEffectInstances that rotates round-robin to avoid InstancePlayLimitException.</summary>
public sealed class SoundEffectPool : IDisposable
{
    private readonly SoundEffectInstance[] _instances;
    private int _currentIndex;

    /// <summary>Gets whether this pool has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Creates a pool by pre-allocating <paramref name="capacity"/> instances from <paramref name="effect"/>.</summary>
    public SoundEffectPool(SoundEffect effect, int capacity)
    {
        _instances = new SoundEffectInstance[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _instances[i] = effect.CreateInstance();
        }
    }

    /// <summary>Plays the next available instance using round-robin rotation. Silently ignores InstancePlayLimitException.</summary>
    public void Play(float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        try
        {
            SoundEffectInstance instance = _instances[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _instances.Length;

            instance.Stop();
            instance.Volume = volume;
            instance.Pitch = pitch;
            instance.Pan = pan;
            instance.Play();
        }
        catch (InstancePlayLimitException)
        {
        }
    }

    /// <summary>Stops all instances in the pool immediately.</summary>
    public void StopAll()
    {
        for (int i = 0; i < _instances.Length; i++)
        {
            _instances[i].Stop();
        }
    }

    /// <summary>Disposes all instances in the pool.</summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        for (int i = 0; i < _instances.Length; i++)
        {
            _instances[i].Dispose();
        }

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}
