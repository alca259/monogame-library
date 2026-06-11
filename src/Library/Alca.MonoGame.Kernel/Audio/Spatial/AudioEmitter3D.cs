namespace Alca.MonoGame.Kernel.Audio.Spatial;

/// <summary>Wraps AudioEmitter and exposes typed Vector3 properties for 3D positional audio emission.</summary>
public sealed class AudioEmitter3D
{
    private readonly AudioEmitter _emitter = new();

    /// <summary>Gets or sets the emitter's world position.</summary>
    public Vector3 Position
    {
        get => _emitter.Position;
        set => _emitter.Position = value;
    }

    /// <summary>Gets or sets the direction the emitter is facing.</summary>
    public Vector3 Forward
    {
        get => _emitter.Forward;
        set => _emitter.Forward = value;
    }

    /// <summary>Gets or sets the emitter's velocity for Doppler calculations.</summary>
    public Vector3 Velocity
    {
        get => _emitter.Velocity;
        set => _emitter.Velocity = value;
    }

    /// <summary>Applies 3D spatialization to the given instance using this emitter and the provided listener.</summary>
    public void Apply3D(SoundEffectInstance instance, AudioListener3D listener)
    {
        instance.Apply3D(listener.Listener, _emitter);
    }
}
