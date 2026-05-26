namespace MonoGame.Editor.Core.Attributes;

/// <summary>
/// Marks a public property on a <c>GameBehaviour</c> subclass as visible and editable
/// in the editor Inspector panel.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorPropertyAttribute : Attribute
{
    /// <summary>Custom label shown next to the control. Defaults to the property name.</summary>
    public string? Label { get; init; }

    /// <summary>Minimum allowed value (for numeric types).</summary>
    public float Min { get; init; } = float.MinValue;

    /// <summary>Maximum allowed value (for numeric types).</summary>
    public float Max { get; init; } = float.MaxValue;

    /// <summary>Tooltip text displayed on hover.</summary>
    public string? Tooltip { get; init; }
}
