namespace MonoGame.Editor.Core.Commands;

/// <summary>Elimina una acción (y sus enlaces) de un <see cref="InputEditorModel"/>, con soporte completo de Deshacer.</summary>
public sealed class RemoveInputActionCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;
    private List<InputBindingEntry>? _savedBindings;

    /// <param name="model">Modelo de destino.</param>
    /// <param name="actionName">Nombre de la acción a eliminar.</param>
    public RemoveInputActionCommand(InputEditorModel model, string actionName)
    {
        _model = model;
        _actionName = actionName;
    }

    /// <inheritdoc/>
    public string Description => $"Remove action '{_actionName}'";

    /// <inheritdoc/>
    public void Execute()
    {
        InputActionEntry? entry = _model.GetAction(_actionName);
        _savedBindings = entry is not null ? new List<InputBindingEntry>(entry.Bindings) : [];
        _model.RemoveAction(_actionName);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        _model.AddAction(_actionName);
        if (_savedBindings is null) return;
        foreach (InputBindingEntry b in _savedBindings)
            _model.AddBinding(_actionName, b.DeviceType, b.Code);
    }
}
