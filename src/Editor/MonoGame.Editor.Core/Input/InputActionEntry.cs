namespace MonoGame.Editor.Core.Input;

/// <summary>Editor-side representation of a named input action with its bindings.</summary>
public sealed class InputActionEntry
{
    /// <summary>Gets or sets the unique action name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the list of bindings for this action.</summary>
    public List<InputBindingEntry> Bindings { get; } = [];
}
