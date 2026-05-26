namespace MonoGame.Editor.Core.Commands;

/// <summary>Attaches a new <see cref="EditorBehaviour"/> to a <see cref="EditorGameObject"/>.</summary>
public sealed class AddBehaviourCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorBehaviour _behaviour;

    /// <param name="target">Object that will receive the behaviour.</param>
    /// <param name="behaviour">Behaviour to attach.</param>
    public AddBehaviourCommand(EditorGameObject target, EditorBehaviour behaviour)
    {
        _target = target;
        _behaviour = behaviour;
    }

    /// <inheritdoc/>
    public string Description => $"Add behaviour '{_behaviour.TypeName}' to '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute() => _target.Behaviours.Add(_behaviour);

    /// <inheritdoc/>
    public void Undo() => _target.Behaviours.Remove(_behaviour);
}
