namespace Alca.MonoGame.Kernel.Events;

/// <summary>
/// Implement this interface on an event class to allow handlers to stop propagation
/// during a <see cref="EventBus.PublishCancellable{T}"/> dispatch.
/// </summary>
public interface ICancellableEvent
{
    /// <summary>Gets or sets a value indicating whether event propagation has been cancelled.</summary>
    bool IsCancelled { get; set; }
}
