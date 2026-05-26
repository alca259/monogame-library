namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated boolean field with dirty tracking and change notification.</summary>
public sealed class NetBool : NetField
{
    private bool _value;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<bool, bool>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetBool"/> with the given initial value.</summary>
    public NetBool(bool initialValue = false) => _value = initialValue;

    /// <summary>Gets or sets the replicated boolean value.</summary>
    public bool Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            bool old = _value;
            _value = value;
            MarkDirty();
            OnValueChanged?.Invoke(old, value);
        }
    }

    /// <summary>Implicit conversion to <see cref="bool"/>.</summary>
    public static implicit operator bool(NetBool field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.Write(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadBool();
}
