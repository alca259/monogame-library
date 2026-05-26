namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>
/// Carries the world-space transform of a networked entity.
/// Sent at a configurable rate by <see cref="NetworkTransformSync"/>.
/// Recommended channel: <see cref="NetworkChannel.Sequenced"/>.
/// </summary>
public sealed class TransformSyncMessage : INetworkMessage
{
    /// <inheritdoc/>
    public ushort MessageId => SystemMessageId.TransformSync;

    /// <summary>Gets or sets the network ID of the entity whose transform is being synced.</summary>
    public uint NetworkId { get; set; }

    /// <summary>Gets or sets the world-space position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Gets or sets the Z-axis world rotation (roll) in radians.</summary>
    public float Rotation { get; set; }

    /// <summary>Gets or sets the XY-plane local scale.</summary>
    public Vector2 LocalScale { get; set; }

    /// <inheritdoc/>
    public void Serialize(ref NetworkWriter writer)
    {
        writer.Write(NetworkId);
        writer.Write(Position);
        writer.Write(Rotation);
        writer.Write(LocalScale);
    }

    /// <inheritdoc/>
    public void Deserialize(ref NetworkReader reader)
    {
        NetworkId = reader.ReadUInt();
        Position = reader.ReadVector3();
        Rotation = reader.ReadFloat();
        LocalScale = reader.ReadVector2();
    }
}
