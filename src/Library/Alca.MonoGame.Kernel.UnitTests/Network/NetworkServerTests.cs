using Alca.MonoGame.Kernel.Network;

namespace Alca.MonoGame.Kernel.UnitTests.Network;

/// <summary>
/// Tests for <see cref="NetworkServer"/> that do not require live network connections.
/// Tests that require real socket connections are tagged with [Trait("Category", "RequiresNetwork")].
/// </summary>
public sealed class NetworkServerTests : IDisposable
{
    // Use a high ephemeral port range to avoid conflicts with system services.
    private const int TestPort = 49152;

    private readonly NetworkServer _sut = new();

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void Start_ValidPort_IsRunningTrue()
    {
        _sut.Start(TestPort);
        Assert.True(_sut.IsRunning);
    }

    [Fact]
    public void Start_AlreadyRunning_Throws()
    {
        _sut.Start(TestPort);
        Assert.Throws<InvalidOperationException>(() => _sut.Start(TestPort + 1));
    }

    [Fact]
    public void Stop_WhenRunning_IsRunningFalse()
    {
        _sut.Start(TestPort + 2);
        _sut.Stop();
        Assert.False(_sut.IsRunning);
    }

    [Fact]
    public void Stop_WhenNotRunning_NoException()
    {
        var ex = Record.Exception(() => _sut.Stop());
        Assert.Null(ex);
    }

    [Fact]
    public void ConnectedPeers_Initially_Zero()
    {
        Assert.Equal(0, _sut.ConnectedPeers);
    }

    [Fact]
    public void RegisterHandler_CanRegisterMultiple()
    {
        int callCount = 0;
        _sut.RegisterHandler<DummyMessage>((_, _) => callCount++);
        _sut.RegisterHandler<AnotherMessage>((_, _) => callCount++);

        // No assertion on callCount because messages are never dispatched without a live peer.
        // The test just ensures registration does not throw.
    }

    [Fact]
    public void UnregisterHandler_DoesNotThrowWhenNotRegistered()
    {
        var ex = Record.Exception(() => _sut.UnregisterHandler<DummyMessage>());
        Assert.Null(ex);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private sealed class DummyMessage : INetworkMessage
    {
        public ushort MessageId => 0xF001;
        public void Serialize(ref NetworkWriter writer) { }
        public void Deserialize(ref NetworkReader reader) { }
    }

    private sealed class AnotherMessage : INetworkMessage
    {
        public ushort MessageId => 0xF002;
        public void Serialize(ref NetworkWriter writer) { }
        public void Deserialize(ref NetworkReader reader) { }
    }
}
