using Alca.MonoGame.Kernel.UI.Controls.Input;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class NumericBoxTests
{
    private static NumericBox MakeBox() => new(null, null, null);

    #region Defaults

    [Fact]
    public void NewNumericBox_TextIsEmpty()
    {
        Assert.Equal(string.Empty, MakeBox().Text);
    }

    [Fact]
    public void NewNumericBox_IsIntFalseByDefault()
    {
        Assert.False(MakeBox().IsInt);
    }

    #endregion

    #region Character filtering

    [Fact]
    public void HandleTextInput_AcceptsDigits()
    {
        var nb = MakeBox();
        nb.HandleTextInput('5');
        Assert.Equal("5", nb.Text);
    }

    [Fact]
    public void HandleTextInput_RejectsLetters()
    {
        var nb = MakeBox();
        nb.HandleTextInput('a');
        Assert.Equal(string.Empty, nb.Text);
    }

    [Fact]
    public void HandleTextInput_AcceptsDecimalPoint_WhenNotInt()
    {
        var nb = MakeBox();
        nb.HandleTextInput('3');
        nb.HandleTextInput('.');
        Assert.Equal("3.", nb.Text);
    }

    [Fact]
    public void HandleTextInput_RejectsSecondDecimalPoint()
    {
        var nb = MakeBox();
        nb.HandleTextInput('1');
        nb.HandleTextInput('.');
        nb.HandleTextInput('5');
        nb.HandleTextInput('.');
        Assert.Equal("1.5", nb.Text);
    }

    [Fact]
    public void HandleTextInput_RejectsDecimalPoint_WhenIsInt()
    {
        var nb = new NumericBox(null, null, null) { IsInt = true };
        nb.HandleTextInput('7');
        nb.HandleTextInput('.');
        Assert.Equal("7", nb.Text);
    }

    [Fact]
    public void HandleTextInput_AcceptsMinusSign_AtPositionZero()
    {
        var nb = MakeBox();
        nb.HandleTextInput('-');
        Assert.Equal("-", nb.Text);
    }

    [Fact]
    public void HandleTextInput_RejectsMinusSign_WhenAlreadyPresent()
    {
        var nb = MakeBox();
        nb.HandleTextInput('-');
        nb.HandleTextInput('5');
        // Try inserting another minus — should be rejected (minus already exists)
        nb.HandleTextInput('\b'); // remove '5'
        nb.HandleTextInput('\b'); // remove '-'
        nb.HandleTextInput('3');
        nb.HandleTextInput('-'); // cursor is at pos 1, not 0
        Assert.Equal("3", nb.Text);
    }

    [Fact]
    public void HandleTextInput_AcceptsBackspace()
    {
        var nb = MakeBox();
        nb.HandleTextInput('9');
        nb.HandleTextInput('\b');
        Assert.Equal(string.Empty, nb.Text);
    }

    #endregion

    #region FloatValue / IntValue

    [Fact]
    public void FloatValue_ParsesCurrentText()
    {
        var nb = MakeBox();
        nb.SetText("3.14");
        Assert.Equal(3.14f, nb.FloatValue, 4);
    }

    [Fact]
    public void IntValue_ParsesCurrentText()
    {
        var nb = new NumericBox(null, null, null) { IsInt = true };
        nb.SetText("42");
        Assert.Equal(42, nb.IntValue);
    }

    [Fact]
    public void FloatValue_ReturnsZero_WhenTextIsEmpty()
    {
        Assert.Equal(0f, MakeBox().FloatValue, 4);
    }

    #endregion

    #region OnBlur clamping

    [Fact]
    public void OnFocusLost_ClampsAboveMax()
    {
        var nb = new NumericBox(null, null, null) { MinValue = 0f, MaxValue = 10f };
        nb.SetText("99");
        nb.OnFocusGained(); // activate so OnFocusLost triggers OnBlur
        nb.OnFocusLost();
        Assert.Equal(10f, nb.FloatValue, 4);
    }

    [Fact]
    public void OnFocusLost_ClampsBelowMin()
    {
        var nb = new NumericBox(null, null, null) { MinValue = 5f, MaxValue = 100f };
        nb.SetText("1");
        nb.OnFocusGained();
        nb.OnFocusLost();
        Assert.Equal(5f, nb.FloatValue, 4);
    }

    [Fact]
    public void OnFocusLost_WithinRange_NoChange()
    {
        var nb = new NumericBox(null, null, null) { MinValue = 0f, MaxValue = 100f };
        nb.SetText("50");
        nb.OnFocusGained();
        nb.OnFocusLost();
        Assert.Equal(50f, nb.FloatValue, 4);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var nb = MakeBox();
        nb.OnFocusGained();
        Assert.True(nb.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocused()
    {
        var nb = MakeBox();
        nb.OnFocusGained();
        nb.OnFocusLost();
        Assert.False(nb.IsFocused);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_DesiredSizeIsPositive()
    {
        var nb = MakeBox();
        nb.Measure(new Vector2(300f, 60f));
        Assert.True(nb.DesiredSize.X > 0);
        Assert.True(nb.DesiredSize.Y > 0);
    }

    #endregion
}
