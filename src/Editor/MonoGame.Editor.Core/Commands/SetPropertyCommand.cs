namespace MonoGame.Editor.Core.Commands;

/// <summary>
/// Generic command that sets any property from <typeparamref name="T"/> to a new value using a delegate.
/// Suitable for inspector-driven property edits and <see cref="EditorBehaviour"/> dictionary updates.
/// </summary>
public sealed class SetPropertyCommand<T> : IEditorCommand
{
    private readonly T _previousValue;
    private readonly T _newValue;
    private readonly Action<T> _setter;
    private readonly string _description;

    /// <param name="description">Human-readable description (e.g. "Set Speed").</param>
    /// <param name="previousValue">Value to restore on undo.</param>
    /// <param name="newValue">Value to apply on execute.</param>
    /// <param name="setter">Delegate that writes the value back to the target property.</param>
    public SetPropertyCommand(string description, T previousValue, T newValue, Action<T> setter)
    {
        _description = description;
        _previousValue = previousValue;
        _newValue = newValue;
        _setter = setter;
    }

    /// <inheritdoc/>
    public string Description => _description;

    /// <inheritdoc/>
    public void Execute() => _setter(_newValue);

    /// <inheritdoc/>
    public void Undo() => _setter(_previousValue);
}
