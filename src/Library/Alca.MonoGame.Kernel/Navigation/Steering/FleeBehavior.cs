namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>Steers the agent away from a target when within <see cref="FleeRadius"/>.</summary>
public sealed class FleeBehavior : ISteeringBehavior
{
    /// <summary>Gets or sets the position to flee from.</summary>
    public Vector2 Target { get; set; }

    /// <summary>Gets or sets the radius within which the agent will flee. Outside this, returns zero.</summary>
    public float FleeRadius { get; set; } = 150f;

    /// <summary>Gets or sets the maximum flee speed in world units per second.</summary>
    public float MaxSpeed { get; set; } = 200f;

    /// <inheritdoc/>
    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)
    {
        Vector2 diff = agentPosition - Target;
        float distance = diff.Length();
        if (distance > FleeRadius || distance < 0.001f) return Vector2.Zero;
        return (diff / distance) * MaxSpeed;
    }
}
