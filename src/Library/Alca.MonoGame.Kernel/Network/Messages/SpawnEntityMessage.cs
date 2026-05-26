namespace Alca.MonoGame.Kernel.Network.Messages;

/// <summary>
/// Instructs clients to instantiate a network entity with the given ID, name, and initial transform.
/// Sent by the server when a new networked entity is spawned.
/// </summary>
public sealed class SpawnEntityMessage : INetworkMessage
{
    /// <inheritdoc/>
    public ushort MessageId => SystemMessageId.Spawn;

    /// <summary>Gets or sets the network-unique identifier assigned to the spawned entity.</summary>
    public uint NetworkId { get; set; }

    /// <summary>Gets or sets the prefab or entity name used to instantiate the entity on clients.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial world-space position of the entity.</summary>
    public Vector3 InitialPosition { get; set; }

    /// <summary>Gets or sets the initial Z-axis rotation (roll) of the entity in radians.</summary>
    public float InitialRotation { get; set; }

    /// <inheritdoc/>
    public void Serialize(ref NetworkWriter writer)
    {
        writer.Write(NetworkId);
        writer.WriteString(EntityName);
        writer.Write(InitialPosition);
        writer.Write(InitialRotation);
    }

    /// <inheritdoc/>
    public void Deserialize(ref NetworkReader reader)
    {
        NetworkId = reader.ReadUInt();
        EntityName = reader.ReadString();
        InitialPosition = reader.ReadVector3();
        InitialRotation = reader.ReadFloat();
    }
}
