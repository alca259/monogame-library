namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>
/// Base class for replicated fields that track whether their value has changed since
/// the last network flush. Concrete subtypes hold the actual typed value.
/// </summary>
public abstract class NetField
{
    /// <summary>Gets a value indicating whether the field value has changed since the last <see cref="ClearDirty"/> call.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Clears the dirty flag. Called by <see cref="NetworkIdentity"/> after flushing.</summary>
    internal void ClearDirty() => IsDirty = false;

    /// <summary>Marks the field as dirty, indicating it should be included in the next sync.</summary>
    protected void MarkDirty() => IsDirty = true;

    /// <summary>Serializes the current value into <paramref name="writer"/>.</summary>
    public abstract void Serialize(ref NetworkWriter writer);

    /// <summary>Deserializes a value from <paramref name="reader"/> and applies it.</summary>
    public abstract void Deserialize(ref NetworkReader reader);
}
