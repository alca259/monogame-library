using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UnitTests.UI;

public sealed class UIContainerTests
{
    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;
        public bool DrawCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public StubElement(float w = 0f, float h = 0f) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) => DrawCalled = true;
        public override void Update(GameTime gameTime) => UpdateCalled = true;
    }

    private static GameTime AnyGameTime() => new GameTime();

    #endregion

    #region Add

    [Fact]
    public void Add_SetsChildParent()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Add(child);
        Assert.Same(container, child.Parent);
    }

    [Fact]
    public void Add_TransfersChildFromPreviousContainer()
    {
        var original = new UIContainer();
        var destination = new UIContainer();
        var child = new StubElement();
        original.Add(child);

        destination.Add(child);

        Assert.Same(destination, child.Parent);
    }

    #endregion

    #region Remove

    [Fact]
    public void Remove_ClearsChildParent()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Add(child);

        container.Remove(child);

        Assert.Null(child.Parent);
    }

    [Fact]
    public void Remove_NonExistentChild_DoesNotThrow()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Remove(child); // should be a no-op
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NoChildren_SetsDesiredSizeZero()
    {
        var container = new UIContainer();
        container.Measure(new Vector2(400f, 300f));
        Assert.Equal(Vector2.Zero, container.DesiredSize);
    }

    [Fact]
    public void Measure_TakesBoundingBoxOfChildren()
    {
        var container = new UIContainer();
        container.Add(new StubElement(100f, 50f));
        container.Add(new StubElement(80f, 90f));

        container.Measure(new Vector2(400f, 300f));

        Assert.Equal(new Vector2(100f, 90f), container.DesiredSize);
    }

    [Fact]
    public void Measure_PropagatesAvailableSizeToChildren()
    {
        var container = new UIContainer();
        var child = new StubElement(200f, 200f);
        container.Add(child);
        var available = new Vector2(400f, 300f);

        container.Measure(available);

        // Child.Measure was called — DesiredSize updated from fixed size
        Assert.Equal(new Vector2(200f, 200f), child.DesiredSize);
    }

    #endregion

    #region Arrange

    [Fact]
    public void Arrange_SetsBoundsOnContainer()
    {
        var container = new UIContainer();
        var bounds = new Rectangle(0, 0, 800, 600);
        container.Arrange(bounds);
        Assert.Equal(bounds, container.Bounds);
    }

    [Fact]
    public void Arrange_PropagatesBoundsToChildren()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Add(child);
        var bounds = new Rectangle(10, 20, 200, 100);

        container.Arrange(bounds);

        Assert.Equal(bounds, child.Bounds);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_CallsEnabledChildren()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Add(child);

        container.Update(AnyGameTime());

        Assert.True(child.UpdateCalled);
    }

    [Fact]
    public void Update_SkipsDisabledChildren()
    {
        var container = new UIContainer();
        var child = new StubElement { IsEnabled = false };
        container.Add(child);

        container.Update(AnyGameTime());

        Assert.False(child.UpdateCalled);
    }

    [Fact]
    public void Update_SkipsWhenContainerDisabled()
    {
        var container = new UIContainer { IsEnabled = false };
        var child = new StubElement();
        container.Add(child);

        container.Update(AnyGameTime());

        Assert.False(child.UpdateCalled);
    }

    #endregion

    #region Draw

    [Fact]
    public void Draw_CallsVisibleChildren()
    {
        var container = new UIContainer();
        var child = new StubElement();
        container.Add(child);

        container.Draw(null!);

        Assert.True(child.DrawCalled);
    }

    [Fact]
    public void Draw_SkipsInvisibleChildren()
    {
        var container = new UIContainer();
        var child = new StubElement { IsVisible = false };
        container.Add(child);

        container.Draw(null!);

        Assert.False(child.DrawCalled);
    }

    [Fact]
    public void Draw_SkipsWhenContainerInvisible()
    {
        var container = new UIContainer { IsVisible = false };
        var child = new StubElement();
        container.Add(child);

        container.Draw(null!);

        Assert.False(child.DrawCalled);
    }

    #endregion
}
