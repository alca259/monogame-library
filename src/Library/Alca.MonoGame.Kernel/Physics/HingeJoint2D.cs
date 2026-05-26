using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherJoint = nkast.Aether.Physics2D.Dynamics.Joints.Joint;
using AetherBody = nkast.Aether.Physics2D.Dynamics.Body;

namespace Alca.MonoGame.Kernel.Physics;

/// <summary>Constrains two bodies to rotate around a shared anchor point (revolute / hinge joint).</summary>
public sealed class HingeJoint2D : Joint2D
{
    /// <summary>Gets or sets the hinge anchor in world space.</summary>
    public Vector2 Anchor { get; set; }

    /// <summary>Gets or sets a value indicating whether the rotational motor is active.</summary>
    public bool UseMotor { get; set; }

    /// <summary>Gets or sets the target angular speed of the motor in radians per second.</summary>
    public float MotorSpeed { get; set; }

    /// <summary>Gets or sets the maximum torque the motor can apply in N·m. Default 10.</summary>
    public float MaxMotorTorque { get; set; } = 10f;

    /// <inheritdoc/>
    protected override AetherJoint CreateJoint(Physics2DWorld physicsWorld, AetherBody bodyA, AetherBody? bodyB)
    {
        RevoluteJoint joint;

        if (bodyB is not null)
        {
            joint = JointFactory.CreateRevoluteJoint(
                physicsWorld.AetherWorld,
                bodyA, bodyB,
                Anchor, Anchor, useWorldCoordinates: true);
        }
        else
        {
            // Anchor bodyA to a fixed world point using a static body.
            var anchor = physicsWorld.AetherWorld.CreateBody(Anchor, 0f, BodyType.Static);
            joint = JointFactory.CreateRevoluteJoint(
                physicsWorld.AetherWorld,
                bodyA, anchor,
                Vector2.Zero, Vector2.Zero, useWorldCoordinates: false);
        }

        if (UseMotor)
        {
            joint.MotorEnabled = true;
            joint.MotorSpeed = MotorSpeed;
            joint.MaxMotorTorque = MaxMotorTorque;
        }

        return joint;
    }
}
