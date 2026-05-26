namespace Alca.MonoGame.Kernel.Network.NetSync;

/// <summary>
/// Marks a property or field on a <see cref="Alca.MonoGame.Kernel.ECS.GameBehaviour"/> for automatic
/// network replication by <see cref="Alca.MonoGame.Kernel.Network.NetworkReplicator"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class NetSyncAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the sync interval in seconds. A value of <c>-1</c> (default) inherits
    /// from the <see cref="NetworkIdentity.SyncInterval"/> of the owning entity.
    /// </summary>
    public float SyncInterval { get; set; } = -1f;

    /// <summary>Gets or sets the network channel used to send updates.</summary>
    public NetworkChannel Channel { get; set; } = NetworkChannel.ReliableOrdered;

    /// <summary>Gets or sets a value indicating whether the value should be interpolated on non-owner clients.</summary>
    public bool Interpolate { get; set; } = false;

    /// <summary>
    /// Gets or sets the explicit <see cref="NetFields.NetField"/> subtype to use.
    /// When <c>null</c> (default), the type is inferred automatically from the member's declared type.
    /// </summary>
    public Type? NetFieldType { get; set; } = null;
}
