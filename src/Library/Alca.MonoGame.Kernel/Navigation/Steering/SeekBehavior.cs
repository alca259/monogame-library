namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>Steers the agent directly toward a target position at maximum speed.</summary>
public sealed class SeekBehavior : ISteeringBehavior
{
    /// <summary>Gets or sets the world-space target position.</summary>
    public Vector2 Target { get; set; }

    /// <summary>Gets or sets the maximum movement speed in world units per second.</summary>
    public float MaxSpeed { get; set; } = 200f;

    /// <inheritdoc/>
    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)
    {
        Vector2 desired = Target - agentPosition;
        float length = desired.Length();
        if (length < 0.001f) return Vector2.Zero;
        return (desired / length) * MaxSpeed;
    }
}
