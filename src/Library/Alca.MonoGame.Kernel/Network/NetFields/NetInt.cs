namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated signed 32-bit integer field with dirty tracking and change notification.</summary>
public sealed class NetInt : NetField
{
    private int _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<int, int>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetInt"/> with the given initial value.</summary>
    public NetInt(int initialValue = 0) => _value = initialValue;

    /// <summary>Gets or sets the replicated integer value.</summary>
    public int Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            int old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="int"/>.</summary>
    public static implicit operator int(NetInt field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadInt();

    /// <inheritdoc/>
    public override void SetValue(object value) => Value = (int)value;

    /// <inheritdoc/>
    public override object? GetValue() => Value;
}
