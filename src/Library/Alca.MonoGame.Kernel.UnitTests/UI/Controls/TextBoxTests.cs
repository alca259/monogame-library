using Alca.MonoGame.Kernel.UI.Controls.Input;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class TextBoxTests
{
    private static TextBox MakeTextBox() => new(null, null, null);

    #region Defaults

    [Fact]
    public void NewTextBox_TextIsEmpty()
    {
        Assert.Equal(string.Empty, MakeTextBox().Text);
    }

    [Fact]
    public void NewTextBox_IsNotFocusedAndNotHovered()
    {
        var tb = MakeTextBox();
        Assert.False(tb.IsFocused);
        Assert.False(tb.IsHovered);
    }

    [Fact]
    public void NewTextBox_CursorIndexIsZero()
    {
        Assert.Equal(0, MakeTextBox().CursorIndex);
    }

    [Fact]
    public void NewTextBox_MaxLengthIsUnlimited()
    {
        Assert.Equal(-1, MakeTextBox().MaxLength);
    }

    #endregion

    #region Character input

    [Fact]
    public void HandleTextInput_AppendsCharacter()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('A');
        Assert.Equal("A", tb.Text);
    }

    [Fact]
    public void HandleTextInput_MultipleChars_BuildsString()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('H');
        tb.HandleTextInput('i');
        Assert.Equal("Hi", tb.Text);
    }

    [Fact]
    public void HandleTextInput_BackspaceChar_DeletesBeforeCursor()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('A');
        tb.HandleTextInput('B');
        tb.HandleTextInput('\b');
        Assert.Equal("A", tb.Text);
        Assert.Equal(1, tb.CursorIndex);
    }

    [Fact]
    public void HandleTextInput_Backspace_WhenEmpty_DoesNothing()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('\b');
        Assert.Equal(string.Empty, tb.Text);
    }

    [Fact]
    public void HandleTextInput_ControlChars_Ignored()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('\t');
        tb.HandleTextInput('\x01');
        Assert.Equal(string.Empty, tb.Text);
    }

    [Fact]
    public void HandleTextInput_Enter_FiresSubmitted()
    {
        var tb = MakeTextBox();
        bool fired = false;
        tb.Submitted += () => fired = true;

        tb.HandleTextInput('\r');

        Assert.True(fired);
        Assert.Equal(string.Empty, tb.Text); // enter doesn't add text in single-line
    }

    #endregion

    #region MaxLength

    [Fact]
    public void HandleTextInput_RejectsCharWhenMaxLengthReached()
    {
        var tb = new TextBox(null, null, null) { MaxLength = 3 };
        tb.HandleTextInput('A');
        tb.HandleTextInput('B');
        tb.HandleTextInput('C');
        tb.HandleTextInput('D');
        Assert.Equal("ABC", tb.Text);
    }

    [Fact]
    public void HandleTextInput_AcceptsCharWhenBelowMaxLength()
    {
        var tb = new TextBox(null, null, null) { MaxLength = 5 };
        tb.HandleTextInput('X');
        Assert.Equal("X", tb.Text);
    }

    #endregion

    #region IsReadOnly

    [Fact]
    public void HandleTextInput_DoesNothing_WhenReadOnly()
    {
        var tb = new TextBox(null, null, null) { IsReadOnly = true };
        tb.HandleTextInput('A');
        Assert.Equal(string.Empty, tb.Text);
    }

    #endregion

    #region SetText

    [Fact]
    public void SetText_ReplacesContent()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('A');
        tb.SetText("Hello");
        Assert.Equal("Hello", tb.Text);
    }

    [Fact]
    public void SetText_MovesCursorToEnd()
    {
        var tb = MakeTextBox();
        tb.SetText("Hi");
        Assert.Equal(2, tb.CursorIndex);
    }

    #endregion

    #region TextChanged event

    [Fact]
    public void TextChanged_FiredOnCharInsert()
    {
        var tb = MakeTextBox();
        string? received = null;
        tb.TextChanged += s => received = s;

        tb.HandleTextInput('Z');

        Assert.Equal("Z", received);
    }

    [Fact]
    public void TextChanged_FiredOnBackspace()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('A');
        string? received = null;
        tb.TextChanged += s => received = s;

        tb.HandleTextInput('\b');

        Assert.Equal(string.Empty, received);
    }

    #endregion

    #region Cursor position

    [Fact]
    public void CursorIndex_AdvancesOnCharInsert()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('X');
        Assert.Equal(1, tb.CursorIndex);
        tb.HandleTextInput('Y');
        Assert.Equal(2, tb.CursorIndex);
    }

    [Fact]
    public void CursorIndex_DecreasesOnBackspace()
    {
        var tb = MakeTextBox();
        tb.HandleTextInput('A');
        tb.HandleTextInput('B');
        tb.HandleTextInput('\b');
        Assert.Equal(1, tb.CursorIndex);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var tb = MakeTextBox();
        tb.OnFocusGained();
        Assert.True(tb.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocused()
    {
        var tb = MakeTextBox();
        tb.OnFocusGained();
        tb.OnFocusLost();
        Assert.False(tb.IsFocused);
    }

    #endregion

    #region Pointer interaction

    [Fact]
    public void OnPointerEnter_SetsHovered()
    {
        var tb = MakeTextBox();
        tb.OnPointerEnter();
        Assert.True(tb.IsHovered);
    }

    [Fact]
    public void OnPointerLeave_ClearsHovered()
    {
        var tb = MakeTextBox();
        tb.OnPointerEnter();
        tb.OnPointerLeave();
        Assert.False(tb.IsHovered);
    }

    [Fact]
    public void OnPointerDown_SetsHandled_WhenEnabled()
    {
        var tb = MakeTextBox();
        tb.Arrange(new Rectangle(0, 0, 200, 30));
        var args = new UIPointerEventArgs { Position = new Point(5, 15) };
        tb.OnPointerDown(ref args);
        Assert.True(args.Handled);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_NullFont_ReturnsNonNegativeSize()
    {
        var tb = MakeTextBox();
        tb.Measure(new Vector2(300f, 50f));
        Assert.True(tb.DesiredSize.X >= 0);
        Assert.True(tb.DesiredSize.Y >= 0);
    }

    #endregion

    #region Focus neighbor IDs

    [Fact]
    public void FocusNeighbors_CanBeSetAndRead()
    {
        var tb = new TextBox(null, null, null) { TabIndex = 3, FocusNeighborDown = 4 };
        Assert.Equal(3, tb.TabIndex);
        Assert.Equal(4, tb.FocusNeighborDown);
        Assert.Null(tb.FocusNeighborUp);
    }

    #endregion
}
