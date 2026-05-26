namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated double-precision floating-point field with dirty tracking and change notification.</summary>
public sealed class NetDouble : NetField
{
    private double _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<double, double>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetDouble"/> with the given initial value.</summary>
    public NetDouble(double initialValue = 0d) => _value = initialValue;

    /// <summary>Gets or sets the replicated double value.</summary>
    public double Value
    {
        get => _value;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_value == value) return;
            double old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="double"/>.</summary>
    public static implicit operator double(NetDouble field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadDouble();
}
