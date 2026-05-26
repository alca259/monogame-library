using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Core.Commands;

/// <summary>Removes a binding from an action in an <see cref="InputEditorModel"/>.</summary>
public sealed class RemoveInputBindingCommand : IEditorCommand
{
    private readonly InputEditorModel _model;
    private readonly string _actionName;
    private readonly InputBindingEntry _binding;

    /// <param name="model">Target model.</param>
    /// <param name="actionName">Name of the action.</param>
    /// <param name="binding">Binding to remove.</param>
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
