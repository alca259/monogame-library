using Alca.MonoGame.Kernel.UI.Controls.Input;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Controls;

public sealed class PasswordBoxTests
{
    private static PasswordBox MakeBox() => new(null, null, null);

    #region Defaults

    [Fact]
    public void NewPasswordBox_TextIsEmpty()
    {
        Assert.Equal(string.Empty, MakeBox().Text);
    }

    [Fact]
    public void NewPasswordBox_DefaultMaskChar_IsBullet()
    {
        Assert.Equal('•', MakeBox().MaskChar);
    }

    [Fact]
    public void NewPasswordBox_PasswordPropertyEqualsText()
    {
        var pb = MakeBox();
        pb.HandleTextInput('s');
        pb.HandleTextInput('e');
        pb.HandleTextInput('c');

        Assert.Equal("sec", pb.Password);
        Assert.Equal("sec", pb.Text);
    }

    #endregion

    #region Character input stored as plaintext

    [Fact]
    public void HandleTextInput_StoresActualChars_NotMasked()
    {
        var pb = MakeBox();
        pb.HandleTextInput('A');
        pb.HandleTextInput('b');
        pb.HandleTextInput('C');
        Assert.Equal("AbC", pb.Password);
    }

    [Fact]
    public void HandleTextInput_Backspace_DeletesRealChar()
    {
        var pb = MakeBox();
        pb.HandleTextInput('X');
        pb.HandleTextInput('Y');
        pb.HandleTextInput('\b');
        Assert.Equal("X", pb.Password);
    }

    #endregion

    #region MaxLength

    [Fact]
    public void HandleTextInput_RespectsMaxLength()
    {
        var pb = new PasswordBox(null, null, null) { MaxLength = 2 };
        pb.HandleTextInput('A');
        pb.HandleTextInput('B');
        pb.HandleTextInput('C');
        Assert.Equal("AB", pb.Password);
    }

    #endregion

    #region MaskChar

    [Fact]
    public void MaskChar_CanBeChanged()
    {
        var pb = MakeBox();
        pb.MaskChar = '*';
        Assert.Equal('*', pb.MaskChar);
    }

    #endregion

    #region Focus

    [Fact]
    public void OnFocusGained_SetsFocused()
    {
        var pb = MakeBox();
        pb.OnFocusGained();
        Assert.True(pb.IsFocused);
    }

    [Fact]
    public void OnFocusLost_ClearsFocused()
    {
        var pb = MakeBox();
        pb.OnFocusGained();
        pb.OnFocusLost();
        Assert.False(pb.IsFocused);
    }

    #endregion

    #region Measure

    [Fact]
    public void Measure_DesiredSizeIsPositive()
    {
        var pb = MakeBox();
        pb.Measure(new Vector2(200f, 40f));
        Assert.True(pb.DesiredSize.X >= 0);
        Assert.True(pb.DesiredSize.Y >= 0);
    }

    #endregion
}
