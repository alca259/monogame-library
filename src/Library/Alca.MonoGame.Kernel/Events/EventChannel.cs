namespace Alca.MonoGame.Kernel.Events;

/// <summary>
/// Scoped event bus with the same API as <see cref="EventBus"/> but isolated to a single channel.
/// Use one per <see cref="Scenes.Scene"/> to cleanly subscribe/unsubscribe when the scene exits.
/// Dispose or call <see cref="Clear"/> to unsubscribe all handlers.
/// </summary>
public sealed class EventChannel : IDisposable
{
    private record struct HandlerEntry(Delegate Handler, int Priority);

    private readonly Dictionary<Type, List<HandlerEntry>> _handlers = [];

    #region Subscribe
    /// <summary>Subscribes <paramref name="handler"/> to events of type <typeparamref name="T"/> with default priority 0.</summary>
    public void Subscribe<T>(Action<T> handler) => SubscribeWithPriority(handler, 0);

    /// <summary>Subscribes <paramref name="handler"/> and automatically unsubscribes it after the first invocation.</summary>
    public void SubscribeOnce<T>(Action<T> handler)
    {
        Action<T>? wrapper = null;
        wrapper = evt =>
        {
            Unsubscribe<T>(wrapper!);
            handler(evt);
        };
        Subscribe(wrapper);
    }

    /// <summary>Subscribes <paramref name="handler"/> with the given <paramref name="priority"/>. Higher = first.</summary>
    public void SubscribeWithPriority<T>(Action<T> handler, int priority)
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
    public void Unsubscribe<T>(Action<T> handler)
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
    #endregion

    #region Publish
    /// <summary>Dispatches <paramref name="evt"/> to all subscribed handlers in priority order.</summary>
    public void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        for (int i = 0; i < list.Count; i++)
            ((Action<T>)list[i].Handler)(evt);
    }

    /// <summary>Dispatches a cancellable event, stopping propagation when a handler cancels it.</summary>
    public void PublishCancellable<T>(T evt) where T : ICancellableEvent
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        for (int i = 0; i < list.Count; i++)
        {
            ((Action<T>)list[i].Handler)(evt);
            if (evt.IsCancelled) break;
        }
    }
    #endregion

    #region Lifecycle
    /// <summary>Removes all subscriptions on this channel only. Does not affect the global <see cref="EventBus"/>.</summary>
    public void Clear() => _handlers.Clear();

    /// <inheritdoc/>
    public void Dispose() => Clear();
    #endregion
}
