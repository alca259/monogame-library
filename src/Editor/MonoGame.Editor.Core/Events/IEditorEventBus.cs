namespace MonoGame.Editor.Core.Events;

/// <summary>Decoupled event bus for communication between editor panels.</summary>
public interface IEditorEventBus
{
    /// <summary>Publishes an event to all registered subscribers.</summary>
    void Publish<TEvent>(TEvent e) where TEvent : IEditorEvent;

    /// <summary>Subscribes a handler to receive events of type <typeparamref name="TEvent"/>.</summary>
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent;

    /// <summary>Removes a previously registered handler.</summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent;
}
