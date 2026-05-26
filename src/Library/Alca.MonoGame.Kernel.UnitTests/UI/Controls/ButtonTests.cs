using Alca.MonoGame.Kernel.UI.Controls;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class ButtonTests
{
    #region Defaults

    [Fact]
    public void NewButton_IsNotHoveredAndNotFocused()
    {
        var btn = new Button(null, "OK");
        Assert.False(btn.IsHovered);
        Assert.False(btn.IsFocused);
        Assert.Equal(0, btn.TabIndex);
        Assert.Null(btn.FocusNeighborUp);
        Assert.Null(btn.FocusNeighborDown);
        Assert.Null(btn.FocusNeighborLeft);
        Assert.Null(btn.FocusNeighborRight);
    }

    [Fact]
    public void NewButton_HasDefaultColors()
    {
        var btn = new Button(null, "OK");
        Assert.Equal(Color.White, btn.NormalColor);
        Assert.Equal(Color.LightYellow, btn.HoveredColor);
    }

    #endregion

    #region Hover states

    [Fact]
    public void OnPointerEnter_SetsHovered()
    {
        var btn = new Button(null, "OK");
        btn.OnPointerEnter();
        Assert.True(btn.IsHovered);
    }

    [Fact]
    public void OnPointerLeave_ClearsHovered()
    {
        var btn = new Button(null, "OK");
        btn.OnPointerEnter();
        btn.OnPointerLeave();
        Assert.False(btn.IsHovered);
    }

    #endregion

    #region Pressed state

    [Fact]
    public void OnPointerDown_MakesButtonPressed_AndStillHovered()
    {
        var btn = new Button(null, "OK");
        btn.OnPointerEnter();
        var args = new UIPointerEventArgs();
        btn.OnPointerDown(ref args);
        Assert.True(btn.IsHovered); // pressed counts as hovered
    }

    #endregion

    #region Clicked event

    [Fact]
    public void OnPointerUp_AfterPress_FiresClicked()
    {
        var btn = new Button(null, "OK");
        btn.OnPointerEnter();
        var downArgs = new UIPointerEventArgs();
        btn.OnPointerDown(ref downArgs);

        bool clicked = false;
        btn.Clicked += () => clicked = true;

        var upArgs = new UIPointerEventArgs();
        btn.OnPointerUp(ref upArgs);

        Assert.True(clicked);
        Assert.True(upArgs.Handled);
    }

    [Fact]
    public void OnPointerUp_WithoutPress_DoesNotFireClicked()
    {
        var btn = new Button(null, "OK");
        btn.OnPointerEnter();

        bool clicked = false;
        btn.Clicked += () => clicked = true;

        var upArgs = new UIPointerEventArgs();
        btn.OnPointerUp(ref upArgs);

        Assert.False(clicked);
    }

    [Fact]
    public void OnPointerUp_WhenDisabled_DoesNotFireClicked()
    {
        var btn = new Button(null, "OK") { IsEnabled = false };
        btn.OnPointerEnter();
        var downArgs = new UIPointerEventArgs();
        btn.OnPointerDown(ref downArgs);

        bool clicked = false;
        btn.Clicked += () => clicked = true;

        var upArgs = new UIPointerEventArgs();
        btn.OnPointerUp(ref upArgs);

        Assert.False(clicked);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocusedAndHovered()
    {
        var btn = new Button(null, "OK");
        btn.OnFocusGained();
        Assert.True(btn.IsFocused);
        Assert.True(btn.IsHovered);
    }

    [Fact]
    public void OnFocusLost_ClearsFocusedAndHovered()
    {
        var btn = new Button(null, "OK");
        btn.OnFocusGained();
        btn.OnFocusLost();
        Assert.False(btn.IsFocused);
        Assert.False(btn.IsHovered);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NullFont_ReturnsNonNegativeSize()
    {
        var btn = new Button(null, "OK");
        btn.Measure(new Vector2(400f, 100f));
        // With null font, textSize is Zero, desired = (0 + padding, 0 + padding)
        Assert.True(btn.DesiredSize.X >= 0);
        Assert.True(btn.DesiredSize.Y >= 0);
    }

    #endregion

    #region Neighbor IDs

    [Fact]
    public void FocusNeighbors_CanBeSetAndRead()
    {
        var btn = new Button(null, "A") { TabIndex = 1, FocusNeighborDown = 2, FocusNeighborRight = 3 };
        Assert.Equal(1, btn.TabIndex);
        Assert.Equal(2, btn.FocusNeighborDown);
        Assert.Equal(3, btn.FocusNeighborRight);
        Assert.Null(btn.FocusNeighborUp);
        Assert.Null(btn.FocusNeighborLeft);
    }

    #endregion
}
