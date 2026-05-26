using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Network;
using Alca.MonoGame.Kernel.Network.Messages;
using Alca.MonoGame.Kernel.Physics;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

public sealed class NetworkPhysicsSyncTests
{
    private static GameTime AnyGameTime() =>
        new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

    [Fact]
    public void Awake_WithoutNetworkIdentity_ThrowsInvalidOperationException()
    {
        GameWorld world = new();
        GameEntity entity = world.CreateEntity("E");
        // No NetworkIdentity added.
        Assert.Throws<InvalidOperationException>(() => entity.Add(new NetworkPhysicsSync()));
    }

    [Fact]
    public void Awake_WithoutRigidBody2D_ThrowsInvalidOperationException()
    {
        GameWorld world = new();
        GameEntity entity = world.CreateEntity("E");
        entity.Add(new NetworkIdentity());
        // No RigidBody2D added.
        Assert.Throws<InvalidOperationException>(() => entity.Add(new NetworkPhysicsSync()));
    }

    [Fact]
    public void Awake_WithBothRequiredComponents_DoesNotThrow()
    {
        Physics2DWorld physWorld = new();
        GameWorld world = new() { PhysicsWorld = physWorld };
        GameEntity entity = world.CreateEntity("E");
        entity.Add(new NetworkIdentity());
        entity.Add(new RigidBody2D());

        Exception? ex = Record.Exception(() => entity.Add(new NetworkPhysicsSync()));

        Assert.Null(ex);
    }

    [Fact]
    public void PhysicsSyncMessage_SerializeDeserialize_RoundTripsAllFields()
    {
        PhysicsSyncMessage original = new()
        {
            NetworkId = 42,
            LinearVelocity = new Vector2(3f, -1.5f),
            AngularVelocity = 0.75f,
            Position = new Vector2(10f, 20f),
            Rotation = 1.25f
        };

        Span<byte> buffer = stackalloc byte[128];
        NetworkWriter writer = new(buffer);
        original.Serialize(ref writer);

        NetworkReader reader = new(writer.WrittenSpan);
        PhysicsSyncMessage roundTripped = new();
        roundTripped.Deserialize(ref reader);

        Assert.Equal(original.NetworkId, roundTripped.NetworkId);
        Assert.Equal(original.LinearVelocity, roundTripped.LinearVelocity);
        Assert.Equal(original.AngularVelocity, roundTripped.AngularVelocity, precision: 5);
        Assert.Equal(original.Position, roundTripped.Position);
        Assert.Equal(original.Rotation, roundTripped.Rotation, precision: 5);
    }

    [Fact]
    public void Update_WhenNetworkIdIsZero_DoesNotThrow()
    {
        Physics2DWorld physWorld = new();
        GameWorld world = new() { PhysicsWorld = physWorld };
        GameEntity entity = world.CreateEntity("E");
        entity.Add(new NetworkIdentity()); // NetworkId defaults to 0
        entity.Add(new RigidBody2D());
        entity.Add(new NetworkPhysicsSync());

        Exception? ex = Record.Exception(() =>
        {
            world.Update(AnyGameTime());
            world.Update(AnyGameTime());
        });

        Assert.Null(ex);
    }
}
