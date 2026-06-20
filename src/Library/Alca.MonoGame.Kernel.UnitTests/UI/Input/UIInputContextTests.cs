using Microsoft.Xna.Framework.Input;
using Alca.MonoGame.Kernel.UI.Input;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Input;

public sealed class UIInputContextTests
{
    #region Helpers

    private static KeyboardState KbDown(params Keys[] keys) => new(keys);
    private static KeyboardState KbUp() => new();

    private static MouseState MouseLeft(ButtonState state) =>
        new(0, 0, 0, state, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    private static MouseState MouseAt(int x, int y) =>
        new(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    private static MouseState MouseScroll(int scrollValue) =>
        new(0, 0, scrollValue, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    private static GamePadState PadDown(Buttons button) => new(
        new GamePadThumbSticks(), new GamePadTriggers(),
        new GamePadButtons(button), new GamePadDPad());

    private static GamePadState PadUp() => new(
        new GamePadThumbSticks(), new GamePadTriggers(),
        new GamePadButtons(), new GamePadDPad());

    private static readonly KeyboardState NoKb = new();
    private static readonly MouseState NoMouse = new();
    private static readonly GamePadState NoPad = PadUp();

    private static void Update(UIInputContext ctx,
        KeyboardState currKb, KeyboardState prevKb,
        MouseState currMouse, MouseState prevMouse,
        GamePadState? currPad = null, GamePadState? prevPad = null)
    {
        ctx.Update(currKb, prevKb, currPad ?? NoPad, prevPad ?? NoPad, currMouse, prevMouse);
    }

    #endregion

    #region CreateDefault

    [Fact]
    public void CreateDefault_AllActionsArePopulated()
    {
        var ctx = UIInputContext.CreateDefault();

        Assert.NotNull(ctx.MoveUp);
        Assert.NotNull(ctx.MoveDown);
        Assert.NotNull(ctx.MoveLeft);
        Assert.NotNull(ctx.MoveRight);
        Assert.NotNull(ctx.Confirm);
        Assert.NotNull(ctx.Cancel);
        Assert.NotNull(ctx.TabNext);
        Assert.NotNull(ctx.TabPrevious);
        Assert.NotNull(ctx.Home);
        Assert.NotNull(ctx.End);
        Assert.NotNull(ctx.Delete);
    }

    [Fact]
    public void CreateDefault_BeforeUpdate_PointerPositionIsNull()
    {
        var ctx = UIInputContext.CreateDefault();
        Assert.Null(ctx.PointerPosition);
    }

    [Fact]
    public void CreateDefault_BeforeUpdate_AllPointerButtonsFalse()
    {
        var ctx = UIInputContext.CreateDefault();

        Assert.False(ctx.IsPointerButtonPressed);
        Assert.False(ctx.WasPointerButtonJustPressed);
        Assert.False(ctx.WasPointerButtonJustReleased);
    }

    #endregion

    #region Current

    [Fact]
    public void Update_SetsCurrent_ToThisInstance()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse);

        Assert.Same(ctx, UIInputContext.Current);
    }

    [Fact]
    public void Update_MultipleTimes_CurrentAlwaysMatchesLastInstance()
    {
        var ctx1 = UIInputContext.CreateDefault();
        var ctx2 = UIInputContext.CreateDefault();

        Update(ctx1, NoKb, NoKb, NoMouse, NoMouse);
        Update(ctx2, NoKb, NoKb, NoMouse, NoMouse);

        Assert.Same(ctx2, UIInputContext.Current);
    }

    #endregion

    #region Pointer position

    [Fact]
    public void Update_MouseAtPosition_SetsPointerPosition()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseAt(320, 240), NoMouse);

        Assert.Equal(new Point(320, 240), ctx.PointerPosition);
    }

    [Fact]
    public void Update_DefaultMouse_PointerPositionIsZero()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse);

        Assert.Equal(Point.Zero, ctx.PointerPosition);
    }

    #endregion

    #region Pointer button

    [Fact]
    public void Update_LeftButtonJustPressed_WasPointerButtonJustPressedTrue()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseLeft(ButtonState.Pressed), MouseLeft(ButtonState.Released));

        Assert.True(ctx.WasPointerButtonJustPressed);
        Assert.True(ctx.IsPointerButtonPressed);
        Assert.False(ctx.WasPointerButtonJustReleased);
    }

    [Fact]
    public void Update_LeftButtonHeld_IsPointerButtonPressedTrue()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseLeft(ButtonState.Pressed), MouseLeft(ButtonState.Pressed));

        Assert.True(ctx.IsPointerButtonPressed);
        Assert.False(ctx.WasPointerButtonJustPressed);
        Assert.False(ctx.WasPointerButtonJustReleased);
    }

    [Fact]
    public void Update_LeftButtonJustReleased_WasPointerButtonJustReleasedTrue()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseLeft(ButtonState.Released), MouseLeft(ButtonState.Pressed));

        Assert.True(ctx.WasPointerButtonJustReleased);
        Assert.False(ctx.IsPointerButtonPressed);
        Assert.False(ctx.WasPointerButtonJustPressed);
    }

    [Fact]
    public void Update_NoButtonInput_AllPointerButtonsFalse()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse);

        Assert.False(ctx.IsPointerButtonPressed);
        Assert.False(ctx.WasPointerButtonJustPressed);
        Assert.False(ctx.WasPointerButtonJustReleased);
    }

    #endregion

    #region Scroll delta

    [Fact]
    public void Update_ScrollWheelMoved_SetsPointerScrollDelta()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseScroll(120), MouseScroll(0));

        Assert.Equal(120, ctx.PointerScrollDelta);
    }

    [Fact]
    public void Update_ScrollWheelUnchanged_PointerScrollDeltaIsZero()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, MouseScroll(120), MouseScroll(120));

        Assert.Equal(0, ctx.PointerScrollDelta);
    }

    #endregion

    #region Shift

    [Fact]
    public void Update_LeftShiftHeld_IsShiftHeldTrue()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.LeftShift), NoKb, NoMouse, NoMouse);

        Assert.True(ctx.IsShiftHeld);
    }

    [Fact]
    public void Update_RightShiftHeld_IsShiftHeldTrue()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.RightShift), NoKb, NoMouse, NoMouse);

        Assert.True(ctx.IsShiftHeld);
    }

    [Fact]
    public void Update_NoShift_IsShiftHeldFalse()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse);

        Assert.False(ctx.IsShiftHeld);
    }

    #endregion

    #region Navigation actions — keyboard

    [Fact]
    public void Update_ArrowUpJustPressed_MoveUpIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Up), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.MoveUp!.IsPressed);
    }

    [Fact]
    public void Update_ArrowDownJustPressed_MoveDownIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Down), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.MoveDown!.IsPressed);
    }

    [Fact]
    public void Update_ArrowLeftJustPressed_MoveLeftIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Left), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.MoveLeft!.IsPressed);
    }

    [Fact]
    public void Update_ArrowRightJustPressed_MoveRightIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Right), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.MoveRight!.IsPressed);
    }

    [Fact]
    public void Update_EnterJustPressed_ConfirmIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Enter), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.Confirm!.IsPressed);
    }

    [Fact]
    public void Update_EscapeJustPressed_CancelIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Escape), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.Cancel!.IsPressed);
    }

    [Fact]
    public void Update_TabJustPressed_TabNextIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Tab), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.TabNext!.IsPressed);
    }

    [Fact]
    public void Update_HomeJustPressed_HomeIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Home), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.Home!.IsPressed);
    }

    [Fact]
    public void Update_EndJustPressed_EndIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.End), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.End!.IsPressed);
    }

    [Fact]
    public void Update_DeleteJustPressed_DeleteIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, KbDown(Keys.Delete), KbUp(), NoMouse, NoMouse);

        Assert.True(ctx.Delete!.IsPressed);
    }

    #endregion

    #region Navigation actions — gamepad

    [Fact]
    public void Update_DPadUpJustPressed_MoveUpIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.DPadUp), NoPad);

        Assert.True(ctx.MoveUp!.IsPressed);
    }

    [Fact]
    public void Update_DPadDownJustPressed_MoveDownIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.DPadDown), NoPad);

        Assert.True(ctx.MoveDown!.IsPressed);
    }

    [Fact]
    public void Update_DPadLeftJustPressed_MoveLeftIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.DPadLeft), NoPad);

        Assert.True(ctx.MoveLeft!.IsPressed);
    }

    [Fact]
    public void Update_DPadRightJustPressed_MoveRightIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.DPadRight), NoPad);

        Assert.True(ctx.MoveRight!.IsPressed);
    }

    [Fact]
    public void Update_ButtonAJustPressed_ConfirmIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.A), NoPad);

        Assert.True(ctx.Confirm!.IsPressed);
    }

    [Fact]
    public void Update_ButtonBJustPressed_CancelIsPressed()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse, PadDown(Buttons.B), NoPad);

        Assert.True(ctx.Cancel!.IsPressed);
    }

    #endregion

    #region No input baseline

    [Fact]
    public void Update_NoInput_AllNavigationActionsInactive()
    {
        var ctx = UIInputContext.CreateDefault();
        Update(ctx, NoKb, NoKb, NoMouse, NoMouse);

        Assert.False(ctx.MoveUp!.IsPressed);
        Assert.False(ctx.MoveDown!.IsPressed);
        Assert.False(ctx.MoveLeft!.IsPressed);
        Assert.False(ctx.MoveRight!.IsPressed);
        Assert.False(ctx.Confirm!.IsPressed);
        Assert.False(ctx.Cancel!.IsPressed);
        Assert.False(ctx.TabNext!.IsPressed);
        Assert.False(ctx.Home!.IsPressed);
        Assert.False(ctx.End!.IsPressed);
        Assert.False(ctx.Delete!.IsPressed);
    }

    #endregion
}
