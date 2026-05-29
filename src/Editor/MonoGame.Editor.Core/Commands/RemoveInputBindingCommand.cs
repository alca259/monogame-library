using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Core.Commands;

/// <summary>Elimina un enlace de una acción en un <see cref="InputEditorModel"/>.</summary>
public sealed class RemoveInputBindingCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;
    private readonly InputBindingEntry _binding;

    /// <param name="model">Modelo de destino.</param>
    /// <param name="actionName">Nombre de la acción.</param>
    /// <param name="binding">Enlace a eliminar.</param>
    public RemoveInputBindingCommand(InputEditorModel model, string actionName, InputBindingEntry binding)
    {
        _model = model;
        _actionName = actionName;
        _binding = binding;
    }

    /// <inheritdoc/>
    public string Description => $"Remove binding from '{_actionName}'";

    /// <inheritdoc/>
    public void Execute() => _model.RemoveBinding(_actionName, _binding.DeviceType, _binding.Code);

    /// <inheritdoc/>
    public void Undo() => _model.AddBinding(_actionName, _binding.DeviceType, _binding.Code);
}
