using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Core.Commands;

/// <summary>Adds a binding to an action in an <see cref="InputEditorModel"/>.</summary>
public sealed class AddInputBindingCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;
    private readonly InputBindingEntry _binding;

    /// <param name="model">Target model.</param>
    /// <param name="actionName">Name of the action.</param>
    /// <param name="binding">Binding to add.</param>
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
