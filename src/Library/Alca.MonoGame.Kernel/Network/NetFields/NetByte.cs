namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated byte field with dirty tracking and change notification.</summary>
public sealed class NetByte : NetField
{
    private byte _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<byte, byte>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetByte"/> with the given initial value.</summary>
    public NetByte(byte initialValue = 0) => _value = initialValue;

    /// <summary>Gets or sets the replicated byte value.</summary>
    public byte Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            byte old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="byte"/>.</summary>
    public static implicit operator byte(NetByte field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadByte();

    /// <inheritdoc/>
    public override void SetValue(object value) => Value = (byte)value;

    /// <inheritdoc/>
    public override object? GetValue() => Value;
}
