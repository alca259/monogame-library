namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>
/// Carries the physics velocity state of a networked entity.
/// Sent at a configurable rate by <see cref="NetworkPhysicsSync"/>.
/// Recommended channel: <see cref="NetworkChannel.Sequenced"/>.
/// </summary>
public sealed class PhysicsSyncMessage : INetworkMessage
{
    /// <inheritdoc/>
    public ushort MessageId => SystemMessageId.PhysicsSync;

    /// <summary>Gets or sets the network ID of the entity whose physics state is being synced.</summary>
    public uint NetworkId { get; set; }

    /// <summary>Gets or sets the world-space linear velocity.</summary>
    public Vector2 LinearVelocity { get; set; }

    /// <summary>Gets or sets the angular velocity in radians per second.</summary>
    public float AngularVelocity { get; set; }

    /// <summary>Gets or sets the world-space position at the time of the snapshot.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Gets or sets the Z-axis rotation at the time of the snapshot.</summary>
    public float Rotation { get; set; }

    /// <inheritdoc/>
    public void Serialize(ref NetworkWriter writer)
    {
        writer.Write(NetworkId);
        writer.Write(LinearVelocity);
        writer.Write(AngularVelocity);
        writer.Write(Position);
        writer.Write(Rotation);
    }

    /// <inheritdoc/>
    public void Deserialize(ref NetworkReader reader)
    {
        NetworkId = reader.ReadUInt();
        LinearVelocity = reader.ReadVector2();
        AngularVelocity = reader.ReadFloat();
        Position = reader.ReadVector2();
        Rotation = reader.ReadFloat();
    }
}
