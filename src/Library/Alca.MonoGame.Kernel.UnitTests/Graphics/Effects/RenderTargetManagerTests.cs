using Alca.MonoGame.Kernel.Graphics.Effects;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Effects;

public sealed class RenderTargetManagerApiSurfaceTests
{
    [Fact]
    public void RenderTargetManager_ImplementsIDisposable()
    {
        bool implements = typeof(RenderTargetManager)
            .GetInterfaces()
            .Any(i => i == typeof(IDisposable));

        Assert.True(implements);
    }

    [Fact]
    public void RenderTargetManager_HasExpectedPublicMethods()
    {
        Type t = typeof(RenderTargetManager);

        Assert.NotNull(t.GetMethod("BeginCapture"));
        Assert.NotNull(t.GetMethod("EndCapture"));
        Assert.NotNull(t.GetMethod("Apply"));
        Assert.NotNull(t.GetMethod("ApplyChain"));
        Assert.NotNull(t.GetMethod("Dispose"));
    }
}

[Collection(GraphicsCollection.Name)]
public sealed class RenderTargetManagerGpuTests
{
    private readonly GraphicsDeviceFixture _fx;

    public RenderTargetManagerGpuTests(GraphicsDeviceFixture fx) => _fx = fx;

    [Fact]
    public void Constructor_WithValidDimensions_DoesNotThrow()
    {
        using var rtm = new RenderTargetManager(_fx.GraphicsDevice, 64, 64);
        Assert.NotNull(rtm);
    }

    [Fact]
    public void BeginCapture_ThenEndCapture_DoesNotThrow()
    {
        using var rtm = new RenderTargetManager(_fx.GraphicsDevice, 64, 64);
        rtm.BeginCapture();
        rtm.EndCapture();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var rtm = new RenderTargetManager(_fx.GraphicsDevice, 64, 64);
        rtm.Dispose();
        rtm.Dispose();
    }
}
