namespace MonoGame.Editor.Core.Commands;

/// <summary>Añade una nueva acción con nombre a un <see cref="InputEditorModel"/>.</summary>
public sealed class AddInputActionCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;

    /// <param name="model">Modelo de destino.</param>
    /// <param name="actionName">Nombre de la acción a añadir.</param>
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
