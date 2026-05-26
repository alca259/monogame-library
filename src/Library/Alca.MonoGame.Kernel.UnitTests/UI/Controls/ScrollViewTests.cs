using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Controls;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class ScrollViewTests
{
    // Note: ScrollView requires a real GraphicsDevice only for Draw calls.
    // These tests cover layout and scroll-clamping logic without GPU resources.

    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;

        public StubElement(float w, float h) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) { }
    }

    private static ScrollView CreateWithContent(Rectangle viewBounds, float contentHeight)
    {
        var view = new ScrollView(null!); // null GraphicsDevice: Draw is never called in these tests
        var child = new StubElement(viewBounds.Width, contentHeight);
        view.Add(child);
        child.Measure(new Vector2(viewBounds.Width, contentHeight + 1000f));
        view.Arrange(viewBounds);
        return view;
    }

    #endregion

    #region Initial state

    [Fact]
    public void NewScrollView_ScrollOffset_IsZero()
    {
        var view = new ScrollView(null!);
        Assert.Equal(Vector2.Zero, view.ScrollOffset);
    }

    [Fact]
    public void NewScrollView_ContentSize_IsZero()
    {
        var view = new ScrollView(null!);
        Assert.Equal(Vector2.Zero, view.ContentSize);
    }

    #endregion

    #region ScrollBy clamping

    [Fact]
    public void ScrollBy_NegativeDelta_StaysAtZero()
    {
        var view = new ScrollView(null!);
        view.ScrollBy(new Vector2(0f, -100f));
        Assert.Equal(0f, view.ScrollOffset.Y, 3);
    }

    [Fact]
    public void ScrollBy_DeltaBeyondContent_ClampsToMax()
    {
        // view 200×100, content height 400 → max scroll = 300
        var view = CreateWithContent(new Rectangle(0, 0, 200, 100), 400f);

        view.ScrollBy(new Vector2(0f, 1000f));
        Assert.Equal(300f, view.ScrollOffset.Y, 3);
    }

    [Fact]
    public void ScrollBy_ContentSmallerThanView_OffsetRemainsZero()
    {
        // view 200×400, content height 100 → no scrollable space
        var view = CreateWithContent(new Rectangle(0, 0, 200, 400), 100f);

        view.ScrollBy(new Vector2(0f, 50f));
        Assert.Equal(0f, view.ScrollOffset.Y, 3);
    }

    [Fact]
    public void ScrollBy_ValidDelta_MovesOffset()
    {
        var view = CreateWithContent(new Rectangle(0, 0, 200, 100), 500f);

        view.ScrollBy(new Vector2(0f, 30f));
        Assert.Equal(30f, view.ScrollOffset.Y, 3);
    }

    #endregion
}
