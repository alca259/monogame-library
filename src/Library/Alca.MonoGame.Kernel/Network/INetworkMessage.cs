namespace Alca.MonoGame.Kernel.Network;

/// <summary>
/// Contract for all messages sent over the network.
/// Implementations must be serializable to and from a <see cref="NetworkWriter"/>/<see cref="NetworkReader"/> pair.
/// </summary>
public interface INetworkMessage
{
    /// <summary>Gets the unique identifier for this message type. Must be unique across the application.</summary>
    ushort MessageId { get; }

    /// <summary>Serializes the message payload into <paramref name="writer"/>.</summary>
    void Serialize(ref NetworkWriter writer);

    /// <summary>Reads the message payload from <paramref name="reader"/>.</summary>
    void Deserialize(ref NetworkReader reader);
}
