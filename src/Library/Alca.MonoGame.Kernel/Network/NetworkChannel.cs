namespace Alca.MonoGame.Kernel.Network;

/// <summary>Defines the delivery guarantee for outgoing network messages.</summary>
public enum NetworkChannel : byte
{
    /// <summary>No delivery guarantee; fastest channel. Best for frequently updated data (e.g., position).</summary>
    Unreliable,

    /// <summary>Guaranteed delivery, but packets may arrive out of order.</summary>
    ReliableUnordered,

    /// <summary>Guaranteed delivery in the original send order.</summary>
    ReliableOrdered,

    /// <summary>Only the latest packet is delivered; older packets in transit are discarded.</summary>
    Sequenced
}
