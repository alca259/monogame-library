namespace MonoGame.Editor.Core.Commands;

/// <summary>Desconecta un <see cref="EditorBehaviour"/> de un <see cref="EditorGameObject"/>.</summary>
public sealed class RemoveBehaviourCommand : IEditorCommand
{
    private readonly EditorGameObject _target;
    private readonly EditorBehaviour _behaviour;
    private int _previousIndex;

    /// <param name="target">Objeto propietario del behaviour.</param>
    /// <param name="behaviour">Behaviour a eliminar.</param>
    public RemoveBehaviourCommand(EditorGameObject target, EditorBehaviour behaviour)
    {
        _target = target;
        _behaviour = behaviour;
    }

    /// <inheritdoc/>
    public string Description => $"Remove behaviour '{_behaviour.TypeName}' from '{_target.Name}'";

    /// <inheritdoc/>
    public void Execute()
    {
        _previousIndex = _target.Behaviours.IndexOf(_behaviour);
        _target.Behaviours.RemoveAt(_previousIndex);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        int index = Math.Min(_previousIndex, _target.Behaviours.Count);
        _target.Behaviours.Insert(index, _behaviour);
    }
}
