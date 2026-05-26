using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Controls;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class SliderTests
{
    private static Slider MakeSlider() => new(null);

    #region Defaults

    [Fact]
    public void NewSlider_DefaultValue_IsMinValue()
    {
        var s = MakeSlider();
        Assert.Equal(s.MinValue, s.Value);
    }

    [Fact]
    public void NewSlider_IsNotHoveredAndNotFocused()
    {
        var s = MakeSlider();
        Assert.False(s.IsHovered);
        Assert.False(s.IsFocused);
    }

    [Fact]
    public void NewSlider_DefaultRange_IsZeroToOne()
    {
        var s = MakeSlider();
        Assert.Equal(0f, s.MinValue);
        Assert.Equal(1f, s.MaxValue);
    }

    #endregion

    #region Value clamping

    [Fact]
    public void Value_ClampedToMinValue_WhenBelowMin()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 10f };
        s.Value = -5f;
        Assert.Equal(0f, s.Value);
    }

    [Fact]
    public void Value_ClampedToMaxValue_WhenAboveMax()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 10f };
        s.Value = 99f;
        Assert.Equal(10f, s.Value);
    }

    [Fact]
    public void Value_AcceptsMidRangeValue()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f };
        s.Value = 0.5f;
        Assert.Equal(0.5f, s.Value, 4);
    }

    #endregion

    #region Step snapping

    [Fact]
    public void Value_SnapsToStep_WhenStepIsPositive()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 10f, Step = 1f };
        s.Value = 3.6f;
        Assert.Equal(4f, s.Value, 4);
    }

    [Fact]
    public void Value_Continuous_WhenStepIsZero()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f, Step = 0f };
        s.Value = 0.333f;
        Assert.Equal(0.333f, s.Value, 4);
    }

    [Fact]
    public void Value_SnapsToHalfStep()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f, Step = 0.25f };
        s.Value = 0.4f;
        Assert.Equal(0.5f, s.Value, 4);
    }

    #endregion

    #region ValueChanged event

    [Fact]
    public void ValueChanged_FiredOnValueChange()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f };
        float received = -1f;
        s.ValueChanged += v => received = v;

        s.Value = 0.5f;

        Assert.Equal(0.5f, received, 4);
    }

    [Fact]
    public void ValueChanged_NotFired_WhenValueDoesNotChange()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f };
        s.Value = 0.5f;

        int count = 0;
        s.ValueChanged += _ => count++;
        s.Value = 0.5f;

        Assert.Equal(0, count);
    }

    [Fact]
    public void ValueChanged_NotFired_WhenClampedToSameValue()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f };
        s.Value = 0f;

        int count = 0;
        s.ValueChanged += _ => count++;
        s.Value = -99f;

        Assert.Equal(0, count);
    }

    #endregion

    #region Pointer interaction

    [Fact]
    public void OnPointerEnter_SetsIsHovered()
    {
        var s = MakeSlider();
        s.OnPointerEnter();
        Assert.True(s.IsHovered);
    }

    [Fact]
    public void OnPointerLeave_ClearsIsHovered()
    {
        var s = MakeSlider();
        s.OnPointerEnter();
        s.OnPointerLeave();
        Assert.False(s.IsHovered);
    }

    [Fact]
    public void OnPointerDown_SetsHandled_WhenEnabled()
    {
        var s = new Slider(null) { MinValue = 0f, MaxValue = 1f };
        s.Arrange(new Rectangle(0, 0, 200, 30));

        var args = new UIPointerEventArgs { Position = new Point(100, 15) };
        s.OnPointerDown(ref args);

        Assert.True(args.Handled);
    }

    [Fact]
    public void OnPointerDown_DoesNotSetHandled_WhenDisabled()
    {
        var s = new Slider(null) { IsEnabled = false };
        s.Arrange(new Rectangle(0, 0, 200, 30));

        var args = new UIPointerEventArgs { Position = new Point(100, 15) };
        s.OnPointerDown(ref args);

        Assert.False(args.Handled);
    }

    [Fact]
    public void OnPointerUp_SetsHandled_AfterPointerDown()
    {
        var s = MakeSlider();
        s.Arrange(new Rectangle(0, 0, 200, 30));

        var downArgs = new UIPointerEventArgs { Position = new Point(100, 15) };
        s.OnPointerDown(ref downArgs);

        var upArgs = new UIPointerEventArgs();
        s.OnPointerUp(ref upArgs);

        Assert.True(upArgs.Handled);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var s = MakeSlider();
        s.OnFocusGained();
        Assert.True(s.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocused()
    {
        var s = MakeSlider();
        s.OnFocusGained();
        s.OnFocusLost();
        Assert.False(s.IsFocused);
    }

    #endregion

    #region Layout

    [Fact]
    public void Arrange_SetsBounds()
    {
        var s = MakeSlider();
        var bounds = new Rectangle(10, 20, 300, 40);
        s.Arrange(bounds);
        Assert.Equal(bounds, s.Bounds);
    }

    [Fact]
    public void Measure_Horizontal_DesiredSizeMatchesAvailableWidth()
    {
        var s = new Slider(null) { Orientation = Orientation.Horizontal };
        s.Measure(new Vector2(400f, 100f));
        Assert.Equal(400f, s.DesiredSize.X);
        Assert.True(s.DesiredSize.Y > 0);
    }

    [Fact]
    public void Measure_Vertical_DesiredSizeMatchesAvailableHeight()
    {
        var s = new Slider(null) { Orientation = Orientation.Vertical };
        s.Measure(new Vector2(50f, 200f));
        Assert.Equal(200f, s.DesiredSize.Y);
        Assert.True(s.DesiredSize.X > 0);
    }

    #endregion

    #region Focus neighbor IDs

    [Fact]
    public void FocusNeighbors_CanBeSetAndRead()
    {
        var s = new Slider(null)
        {
            TabIndex = 2,
            FocusNeighborLeft = 1,
            FocusNeighborRight = 3,
            FocusNeighborUp = 5,
            FocusNeighborDown = 7
        };
        Assert.Equal(2, s.TabIndex);
        Assert.Equal(1, s.FocusNeighborLeft);
        Assert.Equal(3, s.FocusNeighborRight);
        Assert.Equal(5, s.FocusNeighborUp);
        Assert.Equal(7, s.FocusNeighborDown);
    }

    #endregion
}
