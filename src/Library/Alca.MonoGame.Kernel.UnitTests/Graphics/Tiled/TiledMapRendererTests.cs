using Alca.MonoGame.Kernel.Graphics.Tiled;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Tiled;

// TiledMapRenderer requires GraphicsDevice for its public constructor.
// The internal parameterless constructor bypasses hardware setup and allows
// testing all behaviour that is gated on LoadedMap being null.

public sealed class TiledMapRendererTests
{
    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void LoadedMap_BeforeLoad_IsNull()
    {
        TiledMapRenderer sut = new();
        Assert.Null(sut.LoadedMap);
    }

    [Fact]
    public void GetLayer_BeforeLoad_ReturnsNull()
    {
        TiledMapRenderer sut = new();
        Assert.Null(sut.GetLayer("any"));
    }

    // ── Null-guard methods: must not throw before Load is called ──────────────

    [Fact]
    public void Update_BeforeLoad_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.Update(new GameTime()));
        Assert.Null(ex);
    }

    [Fact]
    public void Draw_BeforeLoad_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.Draw(new Camera2D(), null!));
        Assert.Null(ex);
    }

    [Fact]
    public void DrawLayer_BeforeLoad_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.DrawLayer(new Camera2D(), null!, "Ground"));
        Assert.Null(ex);
    }

    [Fact]
    public void DrawLayers_BeforeLoad_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.DrawLayers(new Camera2D(), null!, "Background", "Ground"));
        Assert.Null(ex);
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_BeforeLoad_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        TiledMapRenderer sut = new();
        sut.Dispose();
        Exception? ex = Record.Exception(() => sut.Dispose());
        Assert.Null(ex);
    }
}
