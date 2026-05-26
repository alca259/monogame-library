using Alca.MonoGame.Kernel.Input;
using Alca.MonoGame.Kernel.UI;
using Alca.MonoGame.Kernel.UI.Controls;
using Alca.MonoGame.Kernel.UI.Interaction;
namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class DropdownTests
{
    private static Dropdown MakeDropdown() => new(new UIOverlayManager());

    private static Dropdown MakeDropdownWithItems(params string[] items)
    {
        var d = MakeDropdown();
        foreach (string item in items)
            d.AddItem(item);
        return d;
    }

    #region Defaults

    [Fact]
    public void NewDropdown_SelectedIndex_IsMinusOne()
    {
        var d = MakeDropdown();
        Assert.Equal(-1, d.SelectedIndex);
    }

    [Fact]
    public void NewDropdown_IsNotExpanded()
    {
        var d = MakeDropdown();
        Assert.False(d.IsExpanded);
    }

    [Fact]
    public void NewDropdown_SelectedText_IsEmpty()
    {
        var d = MakeDropdown();
        Assert.Equal(string.Empty, d.SelectedText);
    }

    [Fact]
    public void NewDropdown_IsNotHovered_AndNotFocused()
    {
        var d = MakeDropdown();
        Assert.False(d.IsHovered);
        Assert.False(d.IsFocused);
    }

    #endregion

    #region AddItem / ClearItems

    [Fact]
    public void AddItem_SelectionChanged_NotFired()
    {
        var d = MakeDropdown();
        bool fired = false;
        d.SelectionChanged += _ => fired = true;
        d.AddItem("A");
        Assert.False(fired);
    }

    [Fact]
    public void ClearItems_ResetsSelectedIndex()
    {
        var d = MakeDropdownWithItems("A", "B");
        d.SelectedIndex = 1;
        d.ClearItems();
        Assert.Equal(-1, d.SelectedIndex);
    }

    [Fact]
    public void ClearItems_SelectedText_IsEmpty()
    {
        var d = MakeDropdownWithItems("X");
        d.SelectedIndex = 0;
        d.ClearItems();
        Assert.Equal(string.Empty, d.SelectedText);
    }

    #endregion

    #region SelectedIndex

    [Fact]
    public void SelectedIndex_InRange_SetsCorrectly()
    {
        var d = MakeDropdownWithItems("A", "B", "C");
        d.SelectedIndex = 2;
        Assert.Equal(2, d.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_BelowMinus1_ClampedToMinus1()
    {
        var d = MakeDropdownWithItems("A");
        d.SelectedIndex = -5;
        Assert.Equal(-1, d.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_AboveMax_ClampsToLastIndex()
    {
        var d = MakeDropdownWithItems("A", "B");
        d.SelectedIndex = 99;
        Assert.Equal(1, d.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_Change_FiresSelectionChanged()
    {
        var d = MakeDropdownWithItems("A", "B");
        int received = -99;
        d.SelectionChanged += i => received = i;
        d.SelectedIndex = 1;
        Assert.Equal(1, received);
    }

    [Fact]
    public void SelectedIndex_SameValue_DoesNotFireSelectionChanged()
    {
        var d = MakeDropdownWithItems("A", "B");
        d.SelectedIndex = 0;
        int count = 0;
        d.SelectionChanged += _ => count++;
        d.SelectedIndex = 0;
        Assert.Equal(0, count);
    }

    [Fact]
    public void SelectedText_MatchesOptionAtSelectedIndex()
    {
        var d = MakeDropdownWithItems("Alpha", "Beta", "Gamma");
        d.SelectedIndex = 1;
        Assert.Equal("Beta", d.SelectedText);
    }

    #endregion

    #region Open / Close

    [Fact]
    public void Open_WhenNoItems_DoesNotExpand()
    {
        var d = MakeDropdown();
        d.Open();
        Assert.False(d.IsExpanded);
    }

    [Fact]
    public void Open_WithItems_Expands()
    {
        var d = MakeDropdownWithItems("A");
        d.Open();
        Assert.True(d.IsExpanded);
    }

    [Fact]
    public void Close_AfterOpen_Collapses()
    {
        var d = MakeDropdownWithItems("A");
        d.Open();
        d.Close();
        Assert.False(d.IsExpanded);
    }

    [Fact]
    public void Open_WhenAlreadyOpen_IsIdempotent()
    {
        var d = MakeDropdownWithItems("A");
        d.Open();
        d.Open(); // second call should not throw
        Assert.True(d.IsExpanded);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_IsIdempotent()
    {
        var d = MakeDropdownWithItems("A");
        d.Close(); // should not throw
        Assert.False(d.IsExpanded);
    }

    #endregion

    #region OnPointerDown

    [Fact]
    public void OnPointerDown_OutsideBounds_WhenExpanded_Closes()
    {
        var d = MakeDropdownWithItems("A");
        d.Arrange(new Rectangle(0, 0, 100, 30));
        d.Open();

        // Click outside bounds
        var args = new UIPointerEventArgs { Position = new Point(200, 200), Button = MouseButton.Left };
        d.OnPointerDown(ref args);

        Assert.False(d.IsExpanded);
    }

    [Fact]
    public void OnPointerDown_OnHeader_TogglesExpanded()
    {
        var d = MakeDropdownWithItems("A");
        d.Arrange(new Rectangle(0, 0, 100, 30));

        var args = new UIPointerEventArgs { Position = new Point(50, 15), Button = MouseButton.Left };
        d.OnPointerDown(ref args);
        Assert.True(d.IsExpanded);

        d.OnPointerDown(ref args);
        Assert.False(d.IsExpanded);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var d = MakeDropdown();
        d.OnFocusGained();
        Assert.True(d.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocusAndClosesDropdown()
    {
        var d = MakeDropdownWithItems("A");
        d.OnFocusGained();
        d.Open();
        d.OnFocusLost();
        Assert.False(d.IsFocused);
        Assert.False(d.IsExpanded);
    }

    #endregion
}
