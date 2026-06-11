using Alca.MonoGame.Kernel.DebugHelpers;

namespace Alca.MonoGame.Kernel.UnitTests.DebugHelpers;

public sealed class DebugDrawTests
{
    public DebugDrawTests()
    {
        DebugDraw.Clear();
        DebugDraw.IsEnabled = true;
    }

    [Fact]
    public void DrawLine_AddsCommandToBuffer()
    {
        DebugDraw.DrawLine(Vector2.Zero, Vector2.One, Color.Red, duration: 5f);

        Assert.Equal(1, DebugDraw.CommandCount);
    }

    [Fact]
    public void Update_ReducesLifetime()
    {
        DebugDraw.DrawLine(Vector2.Zero, Vector2.One, Color.Green, duration: 2f);

        // Advance 1 second — command should still be alive (2s - 1s = 1s remaining).
        DebugDraw.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1f)));

        Assert.Equal(1, DebugDraw.CommandCount);
    }

    [Fact]
    public void Update_RemovesExpiredCommands()
    {
        DebugDraw.DrawLine(Vector2.Zero, Vector2.One, Color.Blue, duration: 0.5f);

        // First update: decrements Lifetime below zero.
        DebugDraw.Update(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1f)));
        // Second update: command has Lifetime <= 0 and is removed.
        DebugDraw.Update(new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.016f)));

        Assert.Equal(0, DebugDraw.CommandCount);
    }

    [Fact]
    public void Clear_ResetsCommandCount()
    {
        DebugDraw.DrawLine(Vector2.Zero, Vector2.One, Color.White, duration: 10f);
        DebugDraw.DrawRect(new Rectangle(0, 0, 10, 10), Color.White, duration: 10f);

        DebugDraw.Clear();

        Assert.Equal(0, DebugDraw.CommandCount);
    }

    [Fact]
    public void WhenNotEnabled_DrawCommandsAreNoOps()
    {
        DebugDraw.IsEnabled = false;
        DebugDraw.DrawLine(Vector2.Zero, Vector2.One, Color.Red, duration: 10f);
        DebugDraw.DrawRect(new Rectangle(0, 0, 10, 10), Color.Red, duration: 10f);

        Assert.Equal(0, DebugDraw.CommandCount);
    }

    [Fact]
    public void DebugOverlay_AddWatch_StoresEntry()
    {
        var overlay = new DebugOverlay();
        overlay.AddWatch("fps", () => "60");
        overlay.AddWatch("pos", () => "(0,0)");

        Assert.Equal(2, overlay.WatchCount);
    }

    [Fact]
    public void DebugOverlay_RemoveWatch_RemovesEntry()
    {
        var overlay = new DebugOverlay();
        overlay.AddWatch("fps", () => "60");
        overlay.AddWatch("pos", () => "(0,0)");

        overlay.RemoveWatch("fps");

        Assert.Equal(1, overlay.WatchCount);
    }
}
