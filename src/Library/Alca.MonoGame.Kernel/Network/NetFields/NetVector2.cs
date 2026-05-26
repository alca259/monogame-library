namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated <see cref="Vector2"/> field with dirty tracking and change notification.</summary>
public sealed class NetVector2 : NetField
{
    private Vector2 _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<Vector2, Vector2>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetVector2"/> with the given initial value.</summary>
    public NetVector2(Vector2 initialValue = default) => _value = initialValue;

    /// <summary>Gets or sets the replicated <see cref="Vector2"/> value.</summary>
    public Vector2 Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            Vector2 old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="Vector2"/>.</summary>
    public static implicit operator Vector2(NetVector2 field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadVector2();

    /// <inheritdoc/>
    public override void SetValue(object value) => Value = (Vector2)value;

    /// <inheritdoc/>
    public override object? GetValue() => Value;
}
