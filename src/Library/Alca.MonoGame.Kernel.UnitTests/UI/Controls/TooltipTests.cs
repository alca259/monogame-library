using Alca.MonoGame.Kernel.UI.Controls.Display;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class TooltipTests
{
    #region Defaults

    [Fact]
    public void NewTooltip_IsNotVisible()
    {
        var tooltip = new Tooltip();
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void NewTooltip_HasDefaultScreenBounds()
    {
        var tooltip = new Tooltip();
        Assert.Equal(1920, tooltip.ScreenBounds.Width);
        Assert.Equal(1080, tooltip.ScreenBounds.Height);
    }

    #endregion

    #region Show / Hide

    [Fact]
    public void Show_WithoutFont_DoesNotSetVisible()
    {
        var tooltip = new Tooltip { Text = "Hello" };
        tooltip.Show(new Vector2(100f, 100f));
        Assert.False(tooltip.IsVisible);
    }

    [Fact]
    public void Hide_SetsInvisible()
    {
        var tooltip = new Tooltip();
        tooltip.IsVisible = true;
        tooltip.Hide();
        Assert.False(tooltip.IsVisible);
    }

    #endregion

    #region ComputeClampedBounds

    [Fact]
    public void ComputeClampedBounds_AnchorBeyondRightEdge_ClampsX()
    {
        var screen = new Rectangle(0, 0, 800, 600);
        var result = Tooltip.ComputeClampedBounds(new Vector2(750f, 100f), 112, 28, screen);
        Assert.True(result.Right <= 800, $"Expected Right <= 800, got {result.Right}");
    }

    [Fact]
    public void ComputeClampedBounds_AnchorBeyondBottomEdge_ClampsY()
    {
        var screen = new Rectangle(0, 0, 800, 600);
        var result = Tooltip.ComputeClampedBounds(new Vector2(100f, 590f), 100, 28, screen);
        Assert.True(result.Bottom <= 600, $"Expected Bottom <= 600, got {result.Bottom}");
    }

    [Fact]
    public void ComputeClampedBounds_AnchorAtOrigin_RemainsOnScreen()
    {
        var screen = new Rectangle(0, 0, 800, 600);
        var result = Tooltip.ComputeClampedBounds(new Vector2(0f, 0f), 100, 28, screen);
        Assert.True(result.X >= 0);
        Assert.True(result.Y >= 0);
    }

    [Fact]
    public void ComputeClampedBounds_NormalPosition_UsesAnchorDirectly()
    {
        var screen = new Rectangle(0, 0, 800, 600);
        var result = Tooltip.ComputeClampedBounds(new Vector2(100f, 100f), 80, 20, screen);
        Assert.Equal(100, result.X);
        Assert.Equal(100, result.Y);
    }

    [Fact]
    public void ComputeClampedBounds_AnchorBeforeLeftEdge_ClampsToLeft()
    {
        var screen = new Rectangle(0, 0, 800, 600);
        var result = Tooltip.ComputeClampedBounds(new Vector2(-20f, 50f), 80, 20, screen);
        Assert.Equal(0, result.X);
    }

    #endregion
}
