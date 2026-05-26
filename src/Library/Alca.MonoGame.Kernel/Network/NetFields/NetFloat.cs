namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated single-precision floating-point field with dirty tracking and change notification.</summary>
public sealed class NetFloat : NetField
{
    private float _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<float, float>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetFloat"/> with the given initial value.</summary>
    public NetFloat(float initialValue = 0f) => _value = initialValue;

    /// <summary>Gets or sets the replicated float value.</summary>
    public float Value
    {
        get => _value;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_value == value) return;
            float old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="float"/>.</summary>
    public static implicit operator float(NetFloat field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadFloat();

    /// <inheritdoc/>
    public override void SetValue(object value) => Value = (float)value;

    /// <inheritdoc/>
    public override object? GetValue() => Value;
}
