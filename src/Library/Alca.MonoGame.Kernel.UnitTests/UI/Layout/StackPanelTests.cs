using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Layout;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Layout;

public sealed class StackPanelTests
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

    #region Measure — Vertical

    [Fact]
    public void Measure_Vertical_NoChildren_ReturnsZero()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.Measure(new Vector2(200f, 400f));
        Assert.Equal(Vector2.Zero, panel.DesiredSize);
    }

    [Fact]
    public void Measure_Vertical_SumsHeightsAndMaxWidth()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0f };
        panel.Add(new StubElement(80f, 30f));
        panel.Add(new StubElement(60f, 20f));
        panel.Measure(new Vector2(200f, 400f));
        Assert.Equal(80f, panel.DesiredSize.X, 3);
        Assert.Equal(50f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_Vertical_IncludesSpacing()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 10f };
        panel.Add(new StubElement(50f, 30f));
        panel.Add(new StubElement(50f, 20f));
        panel.Measure(new Vector2(200f, 400f));
        // 30 + 10 (spacing) + 20 = 60
        Assert.Equal(60f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_Vertical_ThreeChildren_SpacingCountedCorrectly()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5f };
        panel.Add(new StubElement(40f, 10f));
        panel.Add(new StubElement(40f, 10f));
        panel.Add(new StubElement(40f, 10f));
        panel.Measure(new Vector2(200f, 400f));
        // 10 + 5 + 10 + 5 + 10 = 40 (2 gaps for 3 children)
        Assert.Equal(40f, panel.DesiredSize.Y, 3);
    }

    #endregion

    #region Measure — Horizontal

    [Fact]
    public void Measure_Horizontal_SumsWidthsAndMaxHeight()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0f };
        panel.Add(new StubElement(50f, 40f));
        panel.Add(new StubElement(30f, 20f));
        panel.Measure(new Vector2(400f, 200f));
        Assert.Equal(80f, panel.DesiredSize.X, 3);
        Assert.Equal(40f, panel.DesiredSize.Y, 3);
    }

    [Fact]
    public void Measure_Horizontal_IncludesSpacing()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8f };
        panel.Add(new StubElement(50f, 20f));
        panel.Add(new StubElement(50f, 20f));
        panel.Measure(new Vector2(400f, 200f));
        // 50 + 8 + 50 = 108
        Assert.Equal(108f, panel.DesiredSize.X, 3);
    }

    #endregion

    #region Arrange — Vertical

    [Fact]
    public void Arrange_Vertical_PositionsChildrenSequentially()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 0f };
        var a = new StubElement(50f, 30f);
        var b = new StubElement(50f, 20f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(10, 10, 200, 200));

        Assert.Equal(10, a.Bounds.Y);
        Assert.Equal(40, b.Bounds.Y); // 10 + 30
    }

    [Fact]
    public void Arrange_Vertical_AppliesSpacing()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5f };
        var a = new StubElement(50f, 30f);
        var b = new StubElement(50f, 20f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(0, a.Bounds.Y);
        Assert.Equal(35, b.Bounds.Y); // 30 + 5
    }

    [Fact]
    public void Arrange_Vertical_ChildrenInheritContainerX()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        var a = new StubElement(50f, 20f);
        panel.Add(a);
        panel.Measure(new Vector2(200f, 200f));
        panel.Arrange(new Rectangle(15, 0, 200, 200));

        Assert.Equal(15, a.Bounds.X);
    }

    #endregion

    #region Arrange — Horizontal

    [Fact]
    public void Arrange_Horizontal_PositionsChildrenSequentially()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0f };
        var a = new StubElement(50f, 30f);
        var b = new StubElement(40f, 30f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(400f, 100f));
        panel.Arrange(new Rectangle(5, 5, 400, 100));

        Assert.Equal(5, a.Bounds.X);
        Assert.Equal(55, b.Bounds.X); // 5 + 50
    }

    [Fact]
    public void Arrange_Horizontal_AppliesSpacing()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10f };
        var a = new StubElement(50f, 20f);
        var b = new StubElement(50f, 20f);
        panel.Add(a);
        panel.Add(b);
        panel.Measure(new Vector2(400f, 100f));
        panel.Arrange(new Rectangle(0, 0, 400, 100));

        Assert.Equal(0, a.Bounds.X);
        Assert.Equal(60, b.Bounds.X); // 50 + 10
    }

    #endregion
}
