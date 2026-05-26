using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Layout;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Layout;

public sealed class AnchorLayoutTests
{
    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;

        public StubElement(float w, float h) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) { }
    }

    private static (AnchorLayout layout, StubElement child) CreateArranged(
        Anchor anchor, Vector2 offset = default,
        int parentW = 200, int parentH = 100,
        float childW = 40f, float childH = 20f)
    {
        var layout = new AnchorLayout();
        var child = new StubElement(childW, childH);
        layout.Add(child);
        layout.SetAnchor(child, anchor, offset);
        layout.Measure(new Vector2(parentW, parentH));
        layout.Arrange(new Rectangle(0, 0, parentW, parentH));
        return (layout, child);
    }

    #endregion

    #region Corner anchors

    [Fact]
    public void Arrange_TopLeft_PlacesAtOrigin()
    {
        var (_, child) = CreateArranged(Anchor.TopLeft);
        Assert.Equal(0, child.Bounds.X);
        Assert.Equal(0, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_TopRight_PlacesAtRightEdge()
    {
        var (_, child) = CreateArranged(Anchor.TopRight, childW: 40f, childH: 20f);
        Assert.Equal(200 - 40, child.Bounds.X);
        Assert.Equal(0, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_BottomLeft_PlacesAtBottomEdge()
    {
        var (_, child) = CreateArranged(Anchor.BottomLeft, childW: 40f, childH: 20f);
        Assert.Equal(0, child.Bounds.X);
        Assert.Equal(100 - 20, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_BottomRight_PlacesAtBottomRightCorner()
    {
        var (_, child) = CreateArranged(Anchor.BottomRight, childW: 40f, childH: 20f);
        Assert.Equal(200 - 40, child.Bounds.X);
        Assert.Equal(100 - 20, child.Bounds.Y);
    }

    #endregion

    #region Edge anchors

    [Fact]
    public void Arrange_TopCenter_CentersHorizontally()
    {
        var (_, child) = CreateArranged(Anchor.TopCenter, childW: 40f, childH: 20f);
        Assert.Equal((200 - 40) / 2, child.Bounds.X);
        Assert.Equal(0, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_BottomCenter_CentersHorizontallyAtBottom()
    {
        var (_, child) = CreateArranged(Anchor.BottomCenter, childW: 40f, childH: 20f);
        Assert.Equal((200 - 40) / 2, child.Bounds.X);
        Assert.Equal(100 - 20, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_MiddleLeft_CentersVertically()
    {
        var (_, child) = CreateArranged(Anchor.MiddleLeft, childW: 40f, childH: 20f);
        Assert.Equal(0, child.Bounds.X);
        Assert.Equal((100 - 20) / 2, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_MiddleRight_CentersVerticallyAtRight()
    {
        var (_, child) = CreateArranged(Anchor.MiddleRight, childW: 40f, childH: 20f);
        Assert.Equal(200 - 40, child.Bounds.X);
        Assert.Equal((100 - 20) / 2, child.Bounds.Y);
    }

    #endregion

    #region Center

    [Fact]
    public void Arrange_Center_CentersBothAxes()
    {
        var (_, child) = CreateArranged(Anchor.Center, childW: 40f, childH: 20f);
        Assert.Equal((200 - 40) / 2, child.Bounds.X);
        Assert.Equal((100 - 20) / 2, child.Bounds.Y);
    }

    #endregion

    #region Fill

    [Fact]
    public void Arrange_Fill_StretchesToContainerBounds()
    {
        var (_, child) = CreateArranged(Anchor.Fill, childW: 40f, childH: 20f);
        Assert.Equal(0, child.Bounds.X);
        Assert.Equal(0, child.Bounds.Y);
        Assert.Equal(200, child.Bounds.Width);
        Assert.Equal(100, child.Bounds.Height);
    }

    [Fact]
    public void Arrange_Fill_WithOffset_AppliesInsetMargin()
    {
        // offset acts as margin: X shrinks width by ox*2, Y shrinks height by oy*2
        var (_, child) = CreateArranged(Anchor.Fill, offset: new Vector2(10f, 5f), childW: 40f, childH: 20f);
        Assert.Equal(10, child.Bounds.X);
        Assert.Equal(5, child.Bounds.Y);
        Assert.Equal(200 - 20, child.Bounds.Width);
        Assert.Equal(100 - 10, child.Bounds.Height);
    }

    #endregion

    #region Offset

    [Fact]
    public void Arrange_TopLeft_WithOffset_ShiftsChild()
    {
        var (_, child) = CreateArranged(Anchor.TopLeft, offset: new Vector2(15f, 8f));
        Assert.Equal(15, child.Bounds.X);
        Assert.Equal(8, child.Bounds.Y);
    }

    #endregion

    #region Cleanup

    [Fact]
    public void Remove_CleansUpAnchorEntry_NoExceptionOnRearrange()
    {
        var layout = new AnchorLayout();
        var child = new StubElement(40f, 20f);
        layout.Add(child);
        layout.SetAnchor(child, Anchor.Center);
        layout.Remove(child);
        layout.Measure(new Vector2(200f, 100f));
        // Should not throw — no registered anchor entries remaining.
        layout.Arrange(new Rectangle(0, 0, 200, 100));
        Assert.Equal(Rectangle.Empty, child.Bounds);
    }

    #endregion
}
