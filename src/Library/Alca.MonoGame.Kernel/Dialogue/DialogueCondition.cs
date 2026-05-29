namespace Alca.MonoGame.Kernel.Dialogue;

/// <summary>
/// A lightweight key-value condition that gates whether a <see cref="DialogueChoice"/> is available to the player.
/// Use <see cref="None"/> when no condition should be applied.
/// </summary>
public readonly struct DialogueCondition
{
    /// <summary>A sentinel value representing the absence of a condition. All checks against this value return <see langword="true"/>.</summary>
    public static readonly DialogueCondition None = default;

    /// <summary>Gets the condition key (e.g. a flag name or variable name).</summary>
    public string Key { get; }

    /// <summary>Gets the expected value for the condition to be satisfied.</summary>
    public string Value { get; }

    /// <summary>Creates a new <see cref="DialogueCondition"/>.</summary>
    /// <param name="key">The condition key.</param>
    /// <param name="value">The expected condition value.</param>
    public DialogueCondition(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>Gets a value indicating whether this condition is empty (i.e., always satisfied).</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Key);
}
