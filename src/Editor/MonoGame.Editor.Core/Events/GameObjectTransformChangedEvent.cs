namespace MonoGame.Editor.Core.Events;

/// <summary>Publicado cuando cambia la transformación de un objeto del juego en el viewport del editor.</summary>
public sealed record GameObjectTransformChangedEvent(EditorGameObject GameObject) : IEditorEvent;
