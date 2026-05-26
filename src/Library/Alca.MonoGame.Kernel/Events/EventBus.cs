namespace Alca.MonoGame.Kernel.Events;

/// <summary>Global static event bus. Thread-safe for subscribe/unsubscribe from a single thread (the game thread).</summary>
public static class EventBus
{
    private record struct HandlerEntry(Delegate Handler, int Priority);

    private static readonly Dictionary<Type, List<HandlerEntry>> _handlers = new();

    // ── Subscribe ─────────────────────────────────────────────────────────────

    /// <summary>Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/> with default priority 0.</summary>
    public static void Subscribe<T>(Action<T> handler) => SubscribeWithPriority(handler, 0);

    /// <summary>
    /// Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/> and automatically
    /// unsubscribes it after the first invocation.
    /// </summary>
    public static void SubscribeOnce<T>(Action<T> handler)
    {
        Action<T>? wrapper = null;
        wrapper = evt =>
        {
            Unsubscribe<T>(wrapper!);
            handler(evt);
        };
        Subscribe(wrapper);
    }

    /// <summary>
    /// Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/> with the given
    /// <paramref name="priority"/>. Handlers with a higher priority value are invoked first.
    /// </summary>
    public static void SubscribeWithPriority<T>(Action<T> handler, int priority)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = [];
            _handlers[type] = list;
        }

        int insertAt = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Priority < priority)
            {
                insertAt = i;
                break;
            }
        }
        list.Insert(insertAt, new HandlerEntry(handler, priority));
    }

    /// <summary>Removes a previously subscribed handler.</summary>
    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Handler.Equals(handler))
            {
                list.RemoveAt(i);
                return;
            }
        }
    }

    // ── Publish ───────────────────────────────────────────────────────────────

    /// <summary>Dispatches <paramref name="evt"/> to all subscribed handlers in priority order (highest first).</summary>
    public static void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        for (int i = 0; i < list.Count; i++)
            ((Action<T>)list[i].Handler)(evt);
    }

    /// <summary>
    /// Dispatches a cancellable event, stopping propagation as soon as a handler sets
    /// <see cref="ICancellableEvent.IsCancelled"/> to <c>true</c>.
    /// </summary>
    public static void PublishCancellable<T>(T evt) where T : ICancellableEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        for (int i = 0; i < list.Count; i++)
        {
            ((Action<T>)list[i].Handler)(evt);
            if (evt.IsCancelled) break;
        }
    }

    /// <summary>Removes all subscriptions for all event types.</summary>
    public static void Clear() => _handlers.Clear();
}
