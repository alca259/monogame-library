namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>
/// Steers the agent away from a set of nearby neighbor positions.
/// Populate <see cref="Neighbors"/> before calling <see cref="CalculateSteering"/> each frame.
/// </summary>
public sealed class SeparationBehavior : ISteeringBehavior
{
    /// <summary>Gets or sets the radius within which neighbors trigger separation.</summary>
    public float SeparationRadius { get; set; } = 60f;

    /// <summary>Gets or sets the maximum separation speed in world units per second.</summary>
    public float MaxSpeed { get; set; } = 200f;

    /// <summary>
    /// Gets the pre-allocated neighbor list. The caller populates this before calling
    /// <see cref="CalculateSteering"/>. Contents are not cleared automatically.
    /// </summary>
    public List<Vector2> Neighbors { get; } = new(16);

    /// <inheritdoc/>
    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)
    {
        Vector2 force = Vector2.Zero;
        int count = Neighbors.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 diff = agentPosition - Neighbors[i];
            float distance = diff.Length();
            if (distance < 0.001f || distance > SeparationRadius) continue;
            force += (diff / distance) * (SeparationRadius - distance);
        }

        if (force == Vector2.Zero) return Vector2.Zero;
        float len = force.Length();
        return len < 0.001f ? Vector2.Zero : (force / len) * MaxSpeed;
    }
}
