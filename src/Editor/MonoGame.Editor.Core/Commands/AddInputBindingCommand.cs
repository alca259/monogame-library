namespace MonoGame.Editor.Core.Commands;

/// <summary>Añade un enlace a una acción en un <see cref="InputEditorModel"/>.</summary>
public sealed class AddInputBindingCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;
    private readonly InputBindingEntry _binding;

    /// <param name="model">Modelo de destino.</param>
    /// <param name="actionName">Nombre de la acción.</param>
    /// <param name="binding">Enlace a añadir.</param>
    public AddInputBindingCommand(InputEditorModel model, string actionName, InputBindingEntry binding)
    {
        _model = model;
        _actionName = actionName;
        _binding = binding;
    }

    /// <inheritdoc/>
    public string Description => $"Add binding to '{_actionName}'";

    /// <inheritdoc/>
    public void Execute() => _model.AddBinding(_actionName, _binding.DeviceType, _binding.Code);

    /// <inheritdoc/>
    public void Undo() => _model.RemoveBinding(_actionName, _binding.DeviceType, _binding.Code);
}
