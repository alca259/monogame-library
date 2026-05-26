namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated <see cref="Vector3"/> field with dirty tracking and change notification.</summary>
public sealed class NetVector3 : NetField
{
    private Vector3 _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<Vector3, Vector3>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetVector3"/> with the given initial value.</summary>
    public NetVector3(Vector3 initialValue = default) => _value = initialValue;

    /// <summary>Gets or sets the replicated <see cref="Vector3"/> value.</summary>
    public Vector3 Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            Vector3 old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="Vector3"/>.</summary>
    public static implicit operator Vector3(NetVector3 field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadVector3();
}
