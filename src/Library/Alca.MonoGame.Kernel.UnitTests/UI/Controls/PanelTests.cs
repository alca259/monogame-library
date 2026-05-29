using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Controls;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class PanelTests
{
    #region Defaults

    [Fact]
    public void NewPanel_HasTransparentBackground()
    {
        var panel = new Panel();
        Assert.Equal(Color.Transparent, panel.BackgroundColor);
    }

    [Fact]
    public void NewPanel_BorderThickness_IsZero()
    {
        var panel = new Panel();
        Assert.Equal(0, panel.BorderThickness);
    }

    [Fact]
    public void NewPanel_NineSliceBorder_IsUniformEight()
    {
        var panel = new Panel();
        NineSliceBorderData b = panel.NineSliceBorder;
        Assert.Equal(8, b.Left);
        Assert.Equal(8, b.Right);
        Assert.Equal(8, b.Top);
        Assert.Equal(8, b.Bottom);
        Assert.False(b.TileEdges);
        Assert.False(b.TileCenter);
    }

    #endregion

    #region Children

    [Fact]
    public void Add_Child_IsTrackedByPanel()
    {
        var panel = new Panel();
        var child = new StubElement();
        panel.Add(child);

        Assert.Single(panel.ChildrenReadOnly);
        Assert.Same(child, panel.ChildrenReadOnly[0]);
    }

    [Fact]
    public void Remove_Child_RemovesFromPanel()
    {
        var panel = new Panel();
        var child = new StubElement();
        panel.Add(child);
        panel.Remove(child);

        Assert.Empty(panel.ChildrenReadOnly);
    }

    #endregion

    #region Properties

    [Fact]
    public void BackgroundColor_CanBeSet()
    {
        var panel = new Panel { BackgroundColor = Color.Blue };
        Assert.Equal(Color.Blue, panel.BackgroundColor);
    }

    [Fact]
    public void BorderColor_CanBeSet()
    {
        var panel = new Panel { BorderColor = Color.Red };
        Assert.Equal(Color.Red, panel.BorderColor);
    }

    #endregion

    private sealed class StubElement : UIElement
    {
        public override void Measure(Vector2 availableSize) => DesiredSize = new Vector2(50f, 30f);
        public override void Draw(SpriteBatch spriteBatch) { }
    }
}
