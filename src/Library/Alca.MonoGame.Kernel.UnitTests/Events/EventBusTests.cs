using Alca.MonoGame.Kernel.Events;

namespace Alca.MonoGame.Kernel.UnitTests.Events;

public sealed class EventBusTests
{
    private sealed record TestEvent(string Value);

    private sealed class CancellableTestEvent : ICancellableEvent
    {
        public bool IsCancelled { get; set; }
        public string Value { get; init; } = "";
    }

    // Clean up after each test to avoid handler leakage between tests.
    private void Cleanup() => EventBus.Clear();

    [Fact]
    public void SubscribeOnce_FiresOnce_ThenAutoUnsubscribes()
    {
        Cleanup();
        int count = 0;
        EventBus.SubscribeOnce<TestEvent>(_ => count++);

        EventBus.Publish(new TestEvent("a"));
        EventBus.Publish(new TestEvent("b"));

        Assert.Equal(1, count);
        Cleanup();
    }

    [Fact]
    public void SubscribeWithPriority_HigherPriorityHandlerRunsFirst()
    {
        Cleanup();
        var order = new List<int>();
        EventBus.SubscribeWithPriority<TestEvent>(_ => order.Add(1), priority: 1);
        EventBus.SubscribeWithPriority<TestEvent>(_ => order.Add(10), priority: 10);
        EventBus.SubscribeWithPriority<TestEvent>(_ => order.Add(5), priority: 5);

        EventBus.Publish(new TestEvent("x"));

        Assert.Equal([10, 5, 1], order);
        Cleanup();
    }

    [Fact]
    public void PublishCancellable_StopsDispatchAfterCancelled()
    {
        Cleanup();
        var order = new List<int>();
        EventBus.SubscribeWithPriority<CancellableTestEvent>(e => { order.Add(1); e.IsCancelled = true; }, priority: 2);
        EventBus.SubscribeWithPriority<CancellableTestEvent>(_ => order.Add(2), priority: 1);

        EventBus.PublishCancellable(new CancellableTestEvent());

        Assert.Equal([1], order);
        Cleanup();
    }

    [Fact]
    public void EventChannel_Clear_DoesNotAffectGlobalBus()
    {
        Cleanup();
        int globalFired = 0;
        EventBus.Subscribe<TestEvent>(_ => globalFired++);

        using var channel = new EventChannel();
        channel.Subscribe<TestEvent>(_ => { });
        channel.Clear();

        EventBus.Publish(new TestEvent("y"));
        Assert.Equal(1, globalFired);
        Cleanup();
    }

    [Fact]
    public void EventChannel_Dispose_UnsubscribesAllHandlers()
    {
        var channel = new EventChannel();
        int fired = 0;
        channel.Subscribe<TestEvent>(_ => fired++);

        channel.Dispose();
        channel.Publish(new TestEvent("z"));

        Assert.Equal(0, fired);
    }
}
