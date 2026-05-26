namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated unsigned 32-bit integer field with dirty tracking and change notification.</summary>
public sealed class NetUInt : NetField
{
    private uint _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<uint, uint>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetUInt"/> with the given initial value.</summary>
    public NetUInt(uint initialValue = 0u) => _value = initialValue;

    /// <summary>Gets or sets the replicated unsigned integer value.</summary>
    public uint Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            uint old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="uint"/>.</summary>
    public static implicit operator uint(NetUInt field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadUInt();
}
