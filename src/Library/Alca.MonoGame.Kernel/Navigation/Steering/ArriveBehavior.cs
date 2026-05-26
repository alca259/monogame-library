namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>Seeks the target and decelerates smoothly when within <see cref="SlowRadius"/>.</summary>
public sealed class ArriveBehavior : ISteeringBehavior
{
    /// <summary>Gets or sets the world-space target position.</summary>
    public Vector2 Target { get; set; }

    /// <summary>Gets or sets the radius at which the agent begins to decelerate.</summary>
    public float SlowRadius { get; set; } = 80f;

    /// <summary>Gets or sets the maximum speed in world units per second.</summary>
    public float MaxSpeed { get; set; } = 200f;

    /// <inheritdoc/>
    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)
    {
        Vector2 desired = Target - agentPosition;
        float distance = desired.Length();
        if (distance < 0.001f) return Vector2.Zero;

        float speed = distance < SlowRadius
            ? MaxSpeed * (distance / SlowRadius)
            : MaxSpeed;

        return (desired / distance) * speed;
    }
}
