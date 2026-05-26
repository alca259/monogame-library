using Alca.MonoGame.Kernel.Graphics.Fonts;

namespace Alca.MonoGame.Kernel.UnitTests.Graphics.Fonts;

// BitmapFont requires the content pipeline and GraphicsDevice to instantiate,
// so all tests exercise behaviour available before Load is called (null-guard paths).

public sealed class BitmapFontRendererTests
{
    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Font_BeforeLoad_IsNull()
    {
        BitmapFontRenderer sut = new();
        Assert.Null(sut.Font);
    }

    // ── MeasureString ─────────────────────────────────────────────────────────

    [Fact]
    public void MeasureString_BeforeLoad_ReturnsZero()
    {
        BitmapFontRenderer sut = new();
        Vector2 result = sut.MeasureString("hello");
        Assert.Equal(Vector2.Zero, result);
    }

    [Fact]
    public void MeasureString_EmptyString_BeforeLoad_ReturnsZero()
    {
        BitmapFontRenderer sut = new();
        Vector2 result = sut.MeasureString(string.Empty);
        Assert.Equal(Vector2.Zero, result);
    }

    // ── DrawString (basic) ────────────────────────────────────────────────────

    [Fact]
    public void DrawString_BeforeLoad_DoesNotThrow()
    {
        BitmapFontRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.DrawString(null!, "text", Vector2.Zero, Color.White));
        Assert.Null(ex);
    }

    // ── DrawString (scale + rotation) ─────────────────────────────────────────

    [Fact]
    public void DrawStringScaled_BeforeLoad_DoesNotThrow()
    {
        BitmapFontRenderer sut = new();
        Exception? ex = Record.Exception(() => sut.DrawString(null!, "text", Vector2.Zero, Color.White, 2f, 0.5f));
        Assert.Null(ex);
    }

    // ── DrawCentered ──────────────────────────────────────────────────────────

    [Fact]
    public void DrawCentered_BeforeLoad_DoesNotThrow()
    {
        BitmapFontRenderer sut = new();
        Rectangle bounds = new(0, 0, 800, 600);
        Exception? ex = Record.Exception(() => sut.DrawCentered(null!, "text", bounds, Color.White));
        Assert.Null(ex);
    }

}
