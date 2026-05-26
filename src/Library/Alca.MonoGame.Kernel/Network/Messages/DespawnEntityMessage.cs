namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>
/// Instructs clients to destroy the networked entity with the given ID.
/// Sent by the server when a networked entity is removed from the world.
/// </summary>
public sealed class DespawnEntityMessage : INetworkMessage
{
    /// <inheritdoc/>
    public ushort MessageId => SystemMessageId.Despawn;

    /// <summary>Gets or sets the network ID of the entity to destroy on clients.</summary>
    public uint NetworkId { get; set; }

    /// <inheritdoc/>
    public void Serialize(ref NetworkWriter writer) => writer.Write(NetworkId);

    /// <inheritdoc/>
    public void Deserialize(ref NetworkReader reader) => NetworkId = reader.ReadUInt();
}
