using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherJoint = nkast.Aether.Physics2D.Dynamics.Joints.Joint;
using AetherBody = nkast.Aether.Physics2D.Dynamics.Body;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Spring-like joint that drives two bodies to a target distance with damping and frequency control.</summary>
public sealed class SpringJoint2D : Joint2D
{
    /// <summary>Gets or sets the rest length of the spring in world units. Default 1.</summary>
    public float Distance { get; set; } = 1f;

    /// <summary>Gets or sets the damping ratio (0 = no damping, 1 = critical damping). Default 0.5.</summary>
    public float DampingRatio { get; set; } = 0.5f;

    /// <summary>Gets or sets the oscillation frequency of the spring in Hz. Default 4.</summary>
    public float Frequency { get; set; } = 4f;

    /// <inheritdoc/>
    protected override AetherJoint CreateJoint(Physics2DWorld physicsWorld, AetherBody bodyA, AetherBody? bodyB)
    {
        if (bodyB is null)
            throw new InvalidOperationException("SpringJoint2D requires a ConnectedBody.");

        var joint = JointFactory.CreateDistanceJoint(
            physicsWorld.AetherWorld,
            bodyA, bodyB,
            Vector2.Zero, Vector2.Zero);

        joint.Length = Distance;
        joint.DampingRatio = DampingRatio;
        joint.Frequency = Frequency;

        return joint;
    }
}
