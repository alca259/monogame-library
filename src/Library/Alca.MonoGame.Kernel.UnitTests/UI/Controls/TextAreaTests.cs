using Alca.MonoGame.Kernel.UI.Controls;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class TextAreaTests
{
    private static TextArea MakeArea() => new(null, null, null);

    #region Defaults

    [Fact]
    public void NewTextArea_TextIsEmpty()
    {
        Assert.Equal(string.Empty, MakeArea().Text);
    }

    [Fact]
    public void NewTextArea_IsNotFocused()
    {
        Assert.False(MakeArea().IsFocused);
    }

    [Fact]
    public void NewTextArea_MaxLines_IsUnlimited()
    {
        Assert.Equal(-1, MakeArea().MaxLines);
    }

    #endregion

    #region Character input

    [Fact]
    public void HandleTextInput_InsertsChar()
    {
        var ta = MakeArea();
        ta.HandleTextInput('H');
        ta.HandleTextInput('i');
        Assert.Equal("Hi", ta.Text);
    }

    [Fact]
    public void HandleTextInput_Backspace_DeletesChar()
    {
        var ta = MakeArea();
        ta.HandleTextInput('A');
        ta.HandleTextInput('\b');
        Assert.Equal(string.Empty, ta.Text);
    }

    #endregion

    #region Enter key — line breaks

    [Fact]
    public void HandleTextInput_Enter_InsertsNewline()
    {
        var ta = MakeArea();
        ta.HandleTextInput('A');
        ta.HandleTextInput('\r');
        ta.HandleTextInput('B');
        Assert.Equal("A\nB", ta.Text);
    }

    [Fact]
    public void HandleTextInput_Enter_IncrementsCursorPastNewline()
    {
        var ta = MakeArea();
        ta.HandleTextInput('X');
        ta.HandleTextInput('\r');
        // cursor should be at position 2 (after 'X' and '\n')
        Assert.Equal(2, ta.CursorIndex);
    }

    #endregion

    #region MaxLines

    [Fact]
    public void HandleTextInput_Enter_RespectedByMaxLines()
    {
        var ta = new TextArea(null, null, null) { MaxLines = 2 };
        ta.HandleTextInput('L');
        ta.HandleTextInput('1');
        ta.HandleTextInput('\r'); // now 2 lines — at limit
        ta.HandleTextInput('L');
        ta.HandleTextInput('2');
        ta.HandleTextInput('\r'); // should be rejected
        ta.HandleTextInput('L');
        ta.HandleTextInput('3');

        // Text should be "L1\nL2L3" — third Enter was blocked
        Assert.Equal("L1\nL2L3", ta.Text);
    }

    #endregion

    #region SetText

    [Fact]
    public void SetText_ReplacesContent()
    {
        var ta = MakeArea();
        ta.SetText("line1\nline2");
        Assert.Equal("line1\nline2", ta.Text);
    }

    [Fact]
    public void SetText_MovesCursorToEnd()
    {
        var ta = MakeArea();
        ta.SetText("abc");
        Assert.Equal(3, ta.CursorIndex);
    }

    #endregion

    #region TextChanged event

    [Fact]
    public void TextChanged_FiredOnInput()
    {
        var ta = MakeArea();
        string? received = null;
        ta.TextChanged += s => received = s;

        ta.HandleTextInput('Z');

        Assert.Equal("Z", received);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var ta = MakeArea();
        ta.OnFocusGained();
        Assert.True(ta.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocused()
    {
        var ta = MakeArea();
        ta.OnFocusGained();
        ta.OnFocusLost();
        Assert.False(ta.IsFocused);
    }

    #endregion

    #region ReadOnly

    [Fact]
    public void HandleTextInput_DoesNothing_WhenReadOnly()
    {
        var ta = new TextArea(null, null, null) { IsReadOnly = true };
        ta.HandleTextInput('A');
        Assert.Equal(string.Empty, ta.Text);
    }

    [Fact]
    public void HandleTextInput_Enter_DoesNothing_WhenReadOnly()
    {
        var ta = new TextArea(null, null, null) { IsReadOnly = true };
        ta.SetText("line1");
        ta.HandleTextInput('\r');
        Assert.Equal("line1", ta.Text);
    }

    #endregion

    #region Measure / Arrange

    [Fact]
    public void Measure_DesiredSizeIsPositive()
    {
        var ta = MakeArea();
        ta.Measure(new Vector2(400f, 200f));
        Assert.True(ta.DesiredSize.X > 0);
        Assert.True(ta.DesiredSize.Y > 0);
    }

    [Fact]
    public void Arrange_SetsBounds()
    {
        var ta = MakeArea();
        var bounds = new Rectangle(0, 0, 300, 120);
        ta.Arrange(bounds);
        Assert.Equal(bounds, ta.Bounds);
    }

    #endregion
}
