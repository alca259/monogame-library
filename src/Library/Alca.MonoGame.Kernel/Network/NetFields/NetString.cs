namespace Alca.MonoGame.Kernel.Network.NetFields;

/// <summary>Replicated string field with dirty tracking and change notification.</summary>
public sealed class NetString : NetField
{
    private string _value = string.Empty;

    /// <summary>Raised when the value changes. Parameters are (oldValue, newValue).</summary>
    public event Action<string, string>? OnValueChanged;

    /// <summary>Initializes a new <see cref="NetString"/> with the given initial value.</summary>
    public NetString(string initialValue = "")
    {
        _value = initialValue ?? string.Empty;
    }

    /// <summary>Gets or sets the replicated string value. Never null; empty string is used in place of null.</summary>
    public string Value
    {
        get => _value;
        set
        {
            string safeValue = value ?? string.Empty;
            if (string.Equals(_value, safeValue, StringComparison.Ordinal)) return;
            string old = _value;
            _value = safeValue;
            MarkDirty();
            OnValueChanged?.Invoke(old, safeValue);
        }
    }

    /// <summary>Implicit conversion to <see cref="string"/>.</summary>
    public static implicit operator string(NetString field) => field.Value;

    /// <inheritdoc/>
    public override void Serialize(ref NetworkWriter writer) => writer.WriteString(_value);

    /// <inheritdoc/>
    public override void Deserialize(ref NetworkReader reader) => Value = reader.ReadString();
}
