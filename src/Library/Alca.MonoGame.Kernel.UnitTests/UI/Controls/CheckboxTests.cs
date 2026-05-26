using Alca.MonoGame.Kernel.UI.Controls;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class CheckboxTests
{
    #region Defaults

    [Fact]
    public void NewCheckbox_IsNotChecked()
    {
        var cb = new Checkbox(null, "Enable");
        Assert.False(cb.IsChecked);
    }

    [Fact]
    public void NewCheckbox_IsNotHoveredOrFocused()
    {
        var cb = new Checkbox(null, "A");
        Assert.False(cb.IsHovered);
        Assert.False(cb.IsFocused);
    }

    #endregion

    #region Toggle

    [Fact]
    public void Toggle_FromFalse_SetsTrue()
    {
        var cb = new Checkbox(null, "A");
        cb.Toggle();
        Assert.True(cb.IsChecked);
    }

    [Fact]
    public void Toggle_FromTrue_SetsFalse()
    {
        var cb = new Checkbox(null, "A") { IsChecked = true };
        cb.Toggle();
        Assert.False(cb.IsChecked);
    }

    #endregion

    #region CheckedChanged event

    [Fact]
    public void Toggle_FiresCheckedChanged_WithNewValue()
    {
        var cb = new Checkbox(null, "A");
        bool? received = null;
        cb.CheckedChanged += v => received = v;

        cb.Toggle();

        Assert.Equal(true, received);
    }

    [Fact]
    public void IsChecked_SetSameValue_DoesNotFireEvent()
    {
        var cb = new Checkbox(null, "A") { IsChecked = false };
        int callCount = 0;
        cb.CheckedChanged += _ => callCount++;

        cb.IsChecked = false; // same value

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OnPointerUp_TogglesIsChecked()
    {
        var cb = new Checkbox(null, "A");
        var args = new UIPointerEventArgs();
        cb.OnPointerUp(ref args);

        Assert.True(cb.IsChecked);
        Assert.True(args.Handled);
    }

    [Fact]
    public void OnPointerUp_WhenDisabled_DoesNotToggle()
    {
        var cb = new Checkbox(null, "A") { IsEnabled = false };
        var args = new UIPointerEventArgs();
        cb.OnPointerUp(ref args);

        Assert.False(cb.IsChecked);
    }

    #endregion

    #region Hover

    [Fact]
    public void OnPointerEnter_SetsHovered()
    {
        var cb = new Checkbox(null, "A");
        cb.OnPointerEnter();
        Assert.True(cb.IsHovered);
    }

    [Fact]
    public void OnPointerLeave_ClearsHovered()
    {
        var cb = new Checkbox(null, "A");
        cb.OnPointerEnter();
        cb.OnPointerLeave();
        Assert.False(cb.IsHovered);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocusedAndHovered()
    {
        var cb = new Checkbox(null, "A");
        cb.OnFocusGained();
        Assert.True(cb.IsFocused);
        Assert.True(cb.IsHovered);
    }

    [Fact]
    public void OnFocusLost_ClearsFocusedAndHovered()
    {
        var cb = new Checkbox(null, "A");
        cb.OnFocusGained();
        cb.OnFocusLost();
        Assert.False(cb.IsFocused);
        Assert.False(cb.IsHovered);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NullFont_ReturnsOnlyBoxWidth()
    {
        var cb = new Checkbox(null, "") { BoxSize = 20 };
        cb.Measure(new Vector2(200f, 200f));
        // Width = boxSize + spacing + 0 (no text), Height = max(boxSize, 0)
        Assert.True(cb.DesiredSize.X > 0);
        Assert.True(cb.DesiredSize.Y > 0);
    }

    #endregion
}
