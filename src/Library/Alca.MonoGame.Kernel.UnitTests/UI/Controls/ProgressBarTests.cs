using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Controls;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class ProgressBarTests
{
    #region Defaults

    [Fact]
    public void NewProgressBar_HasDefaultValue_One()
    {
        var bar = new ProgressBar();
        Assert.Equal(1f, bar.Value, 3);
    }

    [Fact]
    public void NewProgressBar_DefaultOrientation_IsHorizontal()
    {
        var bar = new ProgressBar();
        Assert.Equal(Orientation.Horizontal, bar.Orientation);
    }

    [Fact]
    public void NewProgressBar_ColorGradient_IsFalseByDefault()
    {
        var bar = new ProgressBar();
        Assert.False(bar.ColorGradient);
    }

    #endregion

    #region Value clamping

    [Fact]
    public void Value_GreaterThanOne_ClampedToOne()
    {
        var bar = new ProgressBar { Value = 2.5f };
        Assert.Equal(1f, bar.Value, 3);
    }

    [Fact]
    public void Value_LessThanZero_ClampedToZero()
    {
        var bar = new ProgressBar { Value = -1f };
        Assert.Equal(0f, bar.Value, 3);
    }

    [Fact]
    public void Value_SetToHalf_RemainsHalf()
    {
        var bar = new ProgressBar { Value = 0.5f };
        Assert.Equal(0.5f, bar.Value, 3);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_Horizontal_ReturnsExpectedSize()
    {
        var bar = new ProgressBar { Orientation = Orientation.Horizontal };
        bar.Measure(new Vector2(400f, 200f));
        Assert.True(bar.DesiredSize.X > bar.DesiredSize.Y);
    }

    [Fact]
    public void Measure_Vertical_ReturnsTallerThanWide()
    {
        var bar = new ProgressBar { Orientation = Orientation.Vertical };
        bar.Measure(new Vector2(400f, 200f));
        Assert.True(bar.DesiredSize.Y > bar.DesiredSize.X);
    }

    #endregion

    #region Color properties

    [Fact]
    public void FillColor_CanBeSet()
    {
        var bar = new ProgressBar { FillColor = Color.Blue };
        Assert.Equal(Color.Blue, bar.FillColor);
    }

    [Fact]
    public void LowColor_HighColor_CanBeSet()
    {
        var bar = new ProgressBar { LowColor = Color.Red, HighColor = Color.Cyan };
        Assert.Equal(Color.Red, bar.LowColor);
        Assert.Equal(Color.Cyan, bar.HighColor);
    }

    #endregion
}
