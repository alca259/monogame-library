using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Layout;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Layout;

public sealed class FlowLayoutPanelTests
{
    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;

        public StubElement(float w, float h) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) { }
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_AllFitOnOneLine_ReturnsRowHeight()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        panel.Add(new StubElement(50f, 20f));
        panel.Add(new StubElement(50f, 20f));
        panel.Measure(new Vector2(200f, 300f));
        Assert.Equal(20f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_WrapsToSecondRow_AddsBothRowHeights()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        // Width = 100. Items are 60px wide → second item wraps.
        panel.Add(new StubElement(60f, 20f));
        panel.Add(new StubElement(60f, 30f));
        panel.Measure(new Vector2(100f, 400f));
        // Row 1: height 20, Row 2: height 30 → total 50
        Assert.Equal(50f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_WrapsWithSpacing_IncludesLineSpacing()
    {
        var panel = new FlowLayoutPanel { Spacing = 10f };
        panel.Add(new StubElement(60f, 20f));
        panel.Add(new StubElement(60f, 20f));
        panel.Measure(new Vector2(100f, 400f));
        // Row 1: height 20, line spacing 10, Row 2: height 20 → total 50
        Assert.Equal(50f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_NoChildren_ReturnsZeroHeight()
    {
        var panel = new FlowLayoutPanel();
        panel.Measure(new Vector2(200f, 200f));
        Assert.Equal(0f, panel.DesiredSize.Y, 3);
    }

    #endregion

    #region Arrange

    [Fact]
    public void Arrange_AllFitOnOneLine_SameRowY()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        var a = new StubElement(50f, 20f);
        var b = new StubElement(50f, 20f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(a.Bounds.Y, b.Bounds.Y);
        Assert.Equal(0, a.Bounds.X);
        Assert.Equal(50, b.Bounds.X);
    }

    [Fact]
    public void Arrange_WrapsToNewRow_SecondItemOnLowerRow()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        var a = new StubElement(60f, 20f);
        var b = new StubElement(60f, 30f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(100f, 400f));
        panel.Arrange(new Rectangle(0, 0, 100, 400));

        Assert.Equal(0, a.Bounds.Y);
        Assert.Equal(20, b.Bounds.Y); // below first row
        Assert.Equal(0, b.Bounds.X);  // starts at left after wrap
    }

    [Fact]
    public void Arrange_ItemsOnSameRow_VerticallycenteredToTallest()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        var tall = new StubElement(50f, 40f);
        var short1 = new StubElement(50f, 20f);
        panel.Add(tall);
        panel.Add(short1);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(0, 0, 200, 200));

        // Short item should be centered: Y = 0 + (40 - 20) / 2 = 10
        Assert.Equal(10, short1.Bounds.Y);
    }

    [Fact]
    public void Arrange_PanelBoundsOffset_ChildrenShiftedAccordingly()
    {
        var panel = new FlowLayoutPanel { Spacing = 0f };
        var a = new StubElement(40f, 20f);
        panel.Add(a);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(20, 30, 200, 200));

        Assert.Equal(20, a.Bounds.X);
        Assert.Equal(30, a.Bounds.Y);
    }

    #endregion
}
