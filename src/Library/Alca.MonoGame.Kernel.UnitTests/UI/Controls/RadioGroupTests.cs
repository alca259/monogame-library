using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.UI.Controls.Selection;
using Alca.MonoGame.Kernel.UI.Interaction;
namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class RadioGroupTests
{
    private static RadioGroup MakeGroup() => new();

    private static (RadioGroup group, RadioButton a, RadioButton b, RadioButton c) MakeGroupWithThree()
    {
        var g = MakeGroup();
        var a = new RadioButton(null, null, "A", g);
        var b = new RadioButton(null, null, "B", g);
        var c = new RadioButton(null, null, "C", g);
        return (g, a, b, c);
    }

    #region Defaults

    [Fact]
    public void NewGroup_SelectedButton_IsNull()
    {
        var g = MakeGroup();
        Assert.Null(g.SelectedButton);
    }

    [Fact]
    public void NewGroup_SelectedIndex_IsMinusOne()
    {
        var g = MakeGroup();
        Assert.Equal(-1, g.SelectedIndex);
    }

    [Fact]
    public void NewGroup_Count_IsZero()
    {
        var g = MakeGroup();
        Assert.Equal(0, g.Count);
    }

    #endregion

    #region Register / Unregister

    [Fact]
    public void Register_ViaConstructor_IncreasesCount()
    {
        var g = MakeGroup();
        _ = new RadioButton(null, null, "X", g);
        Assert.Equal(1, g.Count);
    }

    [Fact]
    public void Register_Duplicate_IsIgnored()
    {
        var g = MakeGroup();
        var btn = new RadioButton(null, null, "X", g);
        g.Register(btn); // duplicate call
        Assert.Equal(1, g.Count);
    }

    [Fact]
    public void Unregister_ExistingButton_DecreasesCount()
    {
        var g = MakeGroup();
        var btn = new RadioButton(null, null, "X", g);
        g.Unregister(btn);
        Assert.Equal(0, g.Count);
    }

    [Fact]
    public void Unregister_SelectedButton_ClearsSelection()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        g.Select(a);
        g.Unregister(a);
        Assert.Null(g.SelectedButton);
        Assert.Equal(-1, g.SelectedIndex);
    }

    [Fact]
    public void Unregister_NonSelectedButton_DoesNotClearSelection()
    {
        var (g, a, b, _) = MakeGroupWithThree();
        g.Select(a);
        g.Unregister(b);
        Assert.Equal(a, g.SelectedButton);
    }

    #endregion

    #region Select

    [Fact]
    public void Select_SetsSelectedButton()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        g.Select(a);
        Assert.Equal(a, g.SelectedButton);
        Assert.True(a.IsSelected);
    }

    [Fact]
    public void Select_DeselectedPrevious()
    {
        var (g, a, b, _) = MakeGroupWithThree();
        g.Select(a);
        g.Select(b);
        Assert.False(a.IsSelected);
        Assert.True(b.IsSelected);
    }

    [Fact]
    public void Select_FiresSelectionChanged()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        int received = -99;
        g.SelectionChanged += i => received = i;
        g.Select(a);
        Assert.Equal(0, received);
    }

    [Fact]
    public void Select_SameButton_DoesNotFireAgain()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        g.Select(a);
        int count = 0;
        g.SelectionChanged += _ => count++;
        g.Select(a);
        Assert.Equal(0, count);
    }

    [Fact]
    public void SelectAt_ValidIndex_SelectsCorrectButton()
    {
        var (g, _, b, _) = MakeGroupWithThree();
        g.SelectAt(1);
        Assert.Equal(b, g.SelectedButton);
    }

    [Fact]
    public void SelectAt_OutOfRange_IsNoOp()
    {
        var (g, _, _, _) = MakeGroupWithThree();
        g.SelectAt(99); // should not throw
        Assert.Null(g.SelectedButton);
    }

    [Fact]
    public void ClearSelection_RemovesSelection()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        g.Select(a);
        g.ClearSelection();
        Assert.Null(g.SelectedButton);
        Assert.False(a.IsSelected);
    }

    [Fact]
    public void ClearSelection_FiresSelectionChanged_WithMinusOne()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        g.Select(a);
        int received = 99;
        g.SelectionChanged += i => received = i;
        g.ClearSelection();
        Assert.Equal(-1, received);
    }

    #endregion

    #region Navigation helpers

    [Fact]
    public void NextAfter_FirstButton_ReturnsSecond()
    {
        var (g, a, b, _) = MakeGroupWithThree();
        Assert.Equal(b, g.NextAfter(a));
    }

    [Fact]
    public void NextAfter_LastButton_ReturnsNull()
    {
        var (g, _, _, c) = MakeGroupWithThree();
        Assert.Null(g.NextAfter(c));
    }

    [Fact]
    public void PrevBefore_LastButton_ReturnsSecond()
    {
        var (g, _, b, c) = MakeGroupWithThree();
        Assert.Equal(b, g.PrevBefore(c));
    }

    [Fact]
    public void PrevBefore_FirstButton_ReturnsNull()
    {
        var (g, a, _, _) = MakeGroupWithThree();
        Assert.Null(g.PrevBefore(a));
    }

    [Fact]
    public void GetAt_ValidIndex_ReturnsButton()
    {
        var (g, _, b, _) = MakeGroupWithThree();
        Assert.Equal(b, g.GetAt(1));
    }

    [Fact]
    public void GetAt_OutOfRange_ReturnsNull()
    {
        var (g, _, _, _) = MakeGroupWithThree();
        Assert.Null(g.GetAt(99));
    }

    #endregion
}

public sealed class RadioButtonTests
{
    private static (RadioGroup group, RadioButton btn) MakeButton(string label = "Label")
    {
        var g = new RadioGroup();
        var b = new RadioButton(null, null, label, g);
        return (g, b);
    }

    #region Defaults

    [Fact]
    public void NewButton_IsNotSelected()
    {
        var (_, b) = MakeButton();
        Assert.False(b.IsSelected);
    }

    [Fact]
    public void NewButton_IsNotHovered_AndNotFocused()
    {
        var (_, b) = MakeButton();
        Assert.False(b.IsHovered);
        Assert.False(b.IsFocused);
    }

    [Fact]
    public void NewButton_RegisteredInGroup()
    {
        var (g, _) = MakeButton();
        Assert.Equal(1, g.Count);
    }

    #endregion

    #region Pointer interaction

    [Fact]
    public void OnPointerUp_SelectsButtonViaGroup()
    {
        var (g, b) = MakeButton();
        var args = new UIPointerEventArgs { Position = Point.Zero, Button = MouseButton.Left };
        b.OnPointerUp(ref args);
        Assert.Equal(b, g.SelectedButton);
    }

    [Fact]
    public void OnPointerUp_SetsHandled()
    {
        var (_, b) = MakeButton();
        var args = new UIPointerEventArgs { Position = Point.Zero, Button = MouseButton.Left };
        b.OnPointerUp(ref args);
        Assert.True(args.Handled);
    }

    [Fact]
    public void OnPointerUp_WhenDisabled_DoesNotSelect()
    {
        var (g, b) = MakeButton();
        b.IsEnabled = false;
        var args = new UIPointerEventArgs { Position = Point.Zero, Button = MouseButton.Left };
        b.OnPointerUp(ref args);
        Assert.Null(g.SelectedButton);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocusedAndHovered()
    {
        var (_, b) = MakeButton();
        b.OnFocusGained();
        Assert.True(b.IsFocused);
        Assert.True(b.IsHovered);
    }

    [Fact]
    public void OnFocusLost_ClearsFocusAndHovered()
    {
        var (_, b) = MakeButton();
        b.OnFocusGained();
        b.OnFocusLost();
        Assert.False(b.IsFocused);
        Assert.False(b.IsHovered);
    }

    #endregion

    #region Hover

    [Fact]
    public void OnPointerEnter_SetsHovered()
    {
        var (_, b) = MakeButton();
        b.OnPointerEnter();
        Assert.True(b.IsHovered);
    }

    [Fact]
    public void OnPointerLeave_ClearsHovered()
    {
        var (_, b) = MakeButton();
        b.OnPointerEnter();
        b.OnPointerLeave();
        Assert.False(b.IsHovered);
    }

    #endregion
}
