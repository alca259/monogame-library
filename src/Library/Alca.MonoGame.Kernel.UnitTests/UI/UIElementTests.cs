using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UnitTests.UI;

public sealed class UIElementTests
{
    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;

        public StubElement(float w = 0f, float h = 0f) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) { }
    }

    private sealed class CountingContainer : UIContainer
    {
        public int InvalidateCalls { get; private set; }

        public override void Invalidate()
        {
            InvalidateCalls++;
            base.Invalidate();
        }
    }

    #endregion

    #region Defaults

    [Fact]
    public void NewElement_HasExpectedDefaults()
    {
        var e = new StubElement();
        Assert.Equal(Rectangle.Empty, e.Bounds);
        Assert.Equal(Vector2.Zero, e.DesiredSize);
        Assert.True(e.IsVisible);
        Assert.True(e.IsEnabled);
        Assert.Equal(1f, e.Opacity, 3);
        Assert.Null(e.Parent);
    }

    #endregion

    #region Arrange / Measure

    [Fact]
    public void Arrange_SetsBounds()
    {
        var e = new StubElement();
        var bounds = new Rectangle(10, 20, 100, 50);
        e.Arrange(bounds);
        Assert.Equal(bounds, e.Bounds);
    }

    [Fact]
    public void Measure_SetsDesiredSize()
    {
        var e = new StubElement(80f, 40f);
        e.Measure(new Vector2(200f, 200f));
        Assert.Equal(new Vector2(80f, 40f), e.DesiredSize);
    }

    #endregion

    #region EffectiveOpacity

    [Fact]
    public void EffectiveOpacity_NoParent_ReturnsOwnOpacity()
    {
        var e = new StubElement { Opacity = 0.6f };
        Assert.Equal(0.6f, e.EffectiveOpacity, 3);
    }

    [Fact]
    public void EffectiveOpacity_WithParent_MultipliesChain()
    {
        var parent = new UIContainer { Opacity = 0.5f };
        var child = new StubElement { Opacity = 0.4f };
        parent.Add(child);
        Assert.Equal(0.2f, child.EffectiveOpacity, 3);
    }

    [Fact]
    public void EffectiveOpacity_WithGrandparent_MultipliesFullChain()
    {
        var root = new UIContainer { Opacity = 0.5f };
        var mid = new UIContainer { Opacity = 0.5f };
        var leaf = new StubElement { Opacity = 1f };
        root.Add(mid);
        mid.Add(leaf);
        Assert.Equal(0.25f, leaf.EffectiveOpacity, 3);
    }

    #endregion

    #region Invalidate propagation

    [Fact]
    public void Invalidate_PropagatesUpToParent()
    {
        var parent = new CountingContainer();
        var child = new StubElement();
        parent.Add(child);
        parent.InvalidateCalls.ToString(); // consume initial Add invalidation
        int before = parent.InvalidateCalls;

        child.Invalidate();

        Assert.True(parent.InvalidateCalls > before);
    }

    #endregion
}
