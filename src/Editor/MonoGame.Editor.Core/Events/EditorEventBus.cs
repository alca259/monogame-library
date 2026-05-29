namespace MonoGame.Editor.Core.Events;

/// <summary>Implementación de bus de eventos en proceso con seguridad para hilos.</summary>
public sealed class EditorEventBus : IEditorEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = [];
    private readonly Lock _lock = new();

    /// <inheritdoc/>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate>? list))
            {
                list = [];
                _handlers[typeof(TEvent)] = list;
            }

            list.Add(handler);
        }
    }

    /// <inheritdoc/>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEditorEvent
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out List<Delegate>? list))
                list.Remove(handler);
        }
    }

    /// <inheritdoc/>
    public void Publish<TEvent>(TEvent e) where TEvent : IEditorEvent
    {
        Delegate[]? snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate>? list) || list.Count == 0)
                return;

            snapshot = list.ToArray();
        }

        foreach (Delegate handler in snapshot)
            ((Action<TEvent>)handler)(e);
    }
}
