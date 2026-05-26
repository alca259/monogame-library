namespace MonoGame.Editor.Core.Commands;

/// <summary>Adds a new named action to an <see cref="InputEditorModel"/>.</summary>
public sealed class AddInputActionCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;

    /// <param name="model">Target model.</param>
    /// <param name="actionName">Name of the action to add.</param>
    public AddInputActionCommand(InputEditorModel model, string actionName)
    {
        _model = model;
        _actionName = actionName;
    }

    /// <inheritdoc/>
    public string Description => $"Add action '{_actionName}'";

    /// <inheritdoc/>
    public void Execute() => _model.AddAction(_actionName);

    /// <inheritdoc/>
    public void Undo() => _model.RemoveAction(_actionName);
}
