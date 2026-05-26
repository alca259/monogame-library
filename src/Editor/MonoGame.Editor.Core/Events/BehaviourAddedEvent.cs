namespace MonoGame.Editor.Core.Events;

/// <summary>Published when a behaviour is attached to a game object.</summary>
/// <param name="GameObject">Target game object.</param>
/// <param name="Behaviour">The behaviour that was added.</param>
public sealed record BehaviourAddedEvent(EditorGameObject GameObject, EditorBehaviour Behaviour) : IEditorEvent;
