namespace Alca.MonoGame.Kernel.Audio.Spatial;

/// <summary>Wraps AudioListener and exposes typed Vector3 properties for 3D audio positioning.</summary>
public sealed class AudioListener3D
{
    private readonly AudioListener _listener = new();

    /// <summary>Gets or sets the listener's world position.</summary>
    public Vector3 Position
    {
        get => _listener.Position;
        set => _listener.Position = value;
    }

    /// <summary>Gets or sets the direction the listener is facing.</summary>
    public Vector3 Forward
    {
        get => _listener.Forward;
        set => _listener.Forward = value;
    }

    /// <summary>Gets or sets the listener's up vector.</summary>
    public Vector3 Up
    {
        get => _listener.Up;
        set => _listener.Up = value;
    }

    /// <summary>Gets or sets the listener's velocity for Doppler calculations.</summary>
    public Vector3 Velocity
    {
        get => _listener.Velocity;
        set => _listener.Velocity = value;
    }

    /// <summary>Gets the underlying XNA AudioListener for passing to Apply3D calls.</summary>
    internal AudioListener Listener => _listener;

    /// <summary>Updates position and forward direction in a single allocation-free call.</summary>
    public void Update(Vector3 position, Vector3 forward)
    {
        _listener.Position = position;
        _listener.Forward = forward;
    }
}
