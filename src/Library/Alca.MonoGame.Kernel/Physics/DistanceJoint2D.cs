using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherJoint = nkast.Aether.Physics2D.Dynamics.Joints.Joint;
using AetherBody = nkast.Aether.Physics2D.Dynamics.Body;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Maintains a fixed distance between the anchor points of two bodies.</summary>
public sealed class DistanceJoint2D : Joint2D
{
    /// <summary>Gets or sets the target distance between the two anchor points in world units. Default 1.</summary>
    public float Distance { get; set; } = 1f;

    /// <summary>Gets or sets the damping ratio (0 = no damping, 1 = critical damping).</summary>
    public float DampingRatio { get; set; }

    /// <summary>Gets or sets the oscillation frequency in Hz when acting as a soft spring. Default 4.</summary>
    public float Frequency { get; set; } = 4f;

    /// <inheritdoc/>
    protected override AetherJoint CreateJoint(Physics2DWorld physicsWorld, AetherBody bodyA, AetherBody? bodyB)
    {
        if (bodyB is null)
            throw new InvalidOperationException("DistanceJoint2D requires a ConnectedBody.");

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
