namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando se adjunta un behaviour a un objeto del juego.</summary>
/// <param name="GameObject">Objeto del juego destino.</param>
/// <param name="Behaviour">El behaviour que fue añadido.</param>
public sealed record BehaviourAddedEvent(EditorGameObject GameObject, EditorBehaviour Behaviour) : IEditorEvent;
