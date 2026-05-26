namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>Produces a wandering steering vector using a jittered angle on a projected circle.</summary>
public sealed class WanderBehavior : ISteeringBehavior
{
    private float _wanderAngle;

    /// <summary>Gets or sets the radius of the wander circle projected ahead of the agent.</summary>
    public float WanderRadius { get; set; } = 50f;

    /// <summary>Gets or sets how far ahead the wander circle is projected.</summary>
    public float WanderDistance { get; set; } = 100f;

    /// <summary>Gets or sets the maximum random angle change per second (radians).</summary>
    public float WanderJitter { get; set; } = 1f;

    /// <summary>Gets or sets the maximum speed in world units per second.</summary>
    public float MaxSpeed { get; set; } = 200f;

    /// <inheritdoc/>
    public Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _wanderAngle += (float)(Random.Shared.NextDouble() * 2.0 - 1.0) * WanderJitter * dt;

        // Project a circle ahead of the agent, then pick a point on its edge.
        Vector2 ahead = agentVelocity.LengthSquared() > 0.001f
            ? Vector2.Normalize(agentVelocity) * WanderDistance
            : new Vector2(WanderDistance, 0f);

        Vector2 circleCenter = agentPosition + ahead;
        Vector2 displacement = new(MathF.Cos(_wanderAngle) * WanderRadius, MathF.Sin(_wanderAngle) * WanderRadius);
        Vector2 target = circleCenter + displacement;

        Vector2 desired = target - agentPosition;
        float len = desired.Length();
        if (len < 0.001f) return Vector2.Zero;
        return (desired / len) * MaxSpeed;
    }
}
