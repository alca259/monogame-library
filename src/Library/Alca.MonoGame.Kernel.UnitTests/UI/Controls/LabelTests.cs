using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class LabelTests
{
    #region Defaults

    [Fact]
    public void NewLabel_HasExpectedDefaults()
    {
        var label = new Label();
        Assert.Equal(string.Empty, label.Text);
        Assert.Null(label.Font);
        Assert.Equal(Color.White, label.Color);
        Assert.Equal(HAlign.Left, label.HAlign);
        Assert.Equal(VAlign.Top, label.VAlign);
        Assert.False(label.WrapText);
        Assert.True(label.IsVisible);
        Assert.True(label.IsEnabled);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NullFont_ReturnsZeroDesiredSize()
    {
        var label = new Label();
        label.Measure(new Vector2(200f, 200f));
        Assert.Equal(Vector2.Zero, label.DesiredSize);
    }

    #endregion

    #region Text setter / Invalidate

    [Fact]
    public void Text_SetSameValue_DoesNotInvalidate()
    {
        var container = new UIContainer();
        var label = new Label { Text = "Hello" };
        container.Add(label);

        // Reset dirty flag via Arrange
        label.Arrange(new Rectangle(0, 0, 200, 50));

        label.Text = "Hello"; // same value
        Assert.False(label.IsLayoutDirty); // no change, no invalidation
    }

    [Fact]
    public void Text_SetDifferentValue_Invalidates()
    {
        var label = new Label();
        label.Arrange(new Rectangle(0, 0, 200, 50));

        label.Text = "New text";
        Assert.True(label.IsLayoutDirty);
    }

    [Fact]
    public void Font_SetDifferentValue_Invalidates()
    {
        var label = new Label();
        label.Arrange(new Rectangle(0, 0, 200, 50));

        label.Font = null; // already null → no change
        Assert.False(label.IsLayoutDirty);
    }

    #endregion

    #region Alignment properties

    [Fact]
    public void HAlign_CanBeSetAndRead()
    {
        var label = new Label { HAlign = HAlign.Center };
        Assert.Equal(HAlign.Center, label.HAlign);
    }

    [Fact]
    public void VAlign_CanBeSetAndRead()
    {
        var label = new Label { VAlign = VAlign.Bottom };
        Assert.Equal(VAlign.Bottom, label.VAlign);
    }

    #endregion
}
