namespace Alca.MonoGame.Kernel.Navigation.Steering;

/// <summary>Contract for a single steering behavior contributing a desired velocity vector.</summary>
public interface ISteeringBehavior
{
    /// <summary>
    /// Calculates the desired velocity contribution for this behavior.
    /// </summary>
    /// <param name="agentPosition">Current world position of the agent.</param>
    /// <param name="agentVelocity">Current velocity of the agent.</param>
    /// <param name="gameTime">Current game time.</param>
    /// <returns>Desired velocity vector (unscaled by weight).</returns>
    Vector2 CalculateSteering(Vector2 agentPosition, Vector2 agentVelocity, GameTime gameTime);
}
