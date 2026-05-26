using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Layout;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Layout;

public sealed class CanvasTests
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
    public void Measure_ReturnsAvailableSize()
    {
        var canvas = new Canvas();
        var available = new Vector2(300f, 200f);
        canvas.Measure(available);
        Assert.Equal(available, canvas.DesiredSize);
    }

    [Fact]
    public void Measure_WithChildren_StillReturnsAvailableSize()
    {
        var canvas = new Canvas();
        canvas.Add(new StubElement(50f, 50f));
        canvas.Measure(new Vector2(400f, 300f));
        Assert.Equal(new Vector2(400f, 300f), canvas.DesiredSize);
    }

    #endregion

    #region Arrange — offset placement

    [Fact]
    public void Arrange_NoOffset_PlacesChildAtCanvasOrigin()
    {
        var canvas = new Canvas();
        var child = new StubElement(60f, 40f);
        canvas.Add(child);
        canvas.Measure(new Vector2(200f, 200f));
        canvas.Arrange(new Rectangle(10, 20, 200, 200));

        Assert.Equal(10, child.Bounds.X);
        Assert.Equal(20, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_WithOffset_PlacesChildAtOriginPlusOffset()
    {
        var canvas = new Canvas();
        var child = new StubElement(60f, 40f);
        canvas.Add(child);
        canvas.SetOffset(child, new Vector2(30f, 15f));
        canvas.Measure(new Vector2(200f, 200f));
        canvas.Arrange(new Rectangle(10, 20, 200, 200));

        Assert.Equal(40, child.Bounds.X); // 10 + 30
        Assert.Equal(35, child.Bounds.Y); // 20 + 15
    }

    [Fact]
    public void Arrange_MultipleChildren_EachUsesItsOwnOffset()
    {
        var canvas = new Canvas();
        var a = new StubElement(20f, 20f);
        var b = new StubElement(20f, 20f);
        canvas.Add(a);
        canvas.Add(b);
        canvas.SetOffset(a, new Vector2(5f, 10f));
        canvas.SetOffset(b, new Vector2(50f, 80f));
        canvas.Measure(new Vector2(200f, 200f));
        canvas.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(5, a.Bounds.X);
        Assert.Equal(10, a.Bounds.Y);
        Assert.Equal(50, b.Bounds.X);
        Assert.Equal(80, b.Bounds.Y);
    }

    [Fact]
    public void Arrange_ChildBoundsMatchDesiredSize()
    {
        var canvas = new Canvas();
        var child = new StubElement(70f, 35f);
        canvas.Add(child);
        canvas.Measure(new Vector2(200f, 200f));
        canvas.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(70, child.Bounds.Width);
        Assert.Equal(35, child.Bounds.Height);
    }

    #endregion

    #region SetOffset / GetOffset

    [Fact]
    public void GetOffset_NoOffsetSet_ReturnsZero()
    {
        var canvas = new Canvas();
        var child = new StubElement(10f, 10f);
        canvas.Add(child);
        Assert.Equal(Vector2.Zero, canvas.GetOffset(child));
    }

    [Fact]
    public void GetOffset_AfterSetOffset_ReturnsSetValue()
    {
        var canvas = new Canvas();
        var child = new StubElement(10f, 10f);
        canvas.Add(child);
        canvas.SetOffset(child, new Vector2(25f, 50f));
        Assert.Equal(new Vector2(25f, 50f), canvas.GetOffset(child));
    }

    [Fact]
    public void Remove_CleansUpOffset()
    {
        var canvas = new Canvas();
        var child = new StubElement(10f, 10f);
        canvas.Add(child);
        canvas.SetOffset(child, new Vector2(10f, 10f));
        canvas.Remove(child);
        // After removal the offset entry should no longer be present.
        Assert.Equal(Vector2.Zero, canvas.GetOffset(child));
    }

    #endregion
}
