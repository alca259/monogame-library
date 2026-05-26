using Microsoft.Xna.Framework.Input;
using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UnitTests.Input;

public sealed class InputActionTests
{
    #region Helpers

    private static KeyboardState KbDown(params Keys[] keys) => new(keys);
    private static KeyboardState KbUp() => new();

    private static MouseState MouseBtn(MouseButton button, ButtonState state)
    {
        bool pressed = state == ButtonState.Pressed;
        return new MouseState(
            0, 0, 0,
            button == MouseButton.Left    ? state : ButtonState.Released,
            button == MouseButton.Middle  ? state : ButtonState.Released,
            button == MouseButton.Right   ? state : ButtonState.Released,
            button == MouseButton.XButton1 ? state : ButtonState.Released,
            button == MouseButton.XButton2 ? state : ButtonState.Released);
    }

    private static GamePadState PadDown(Buttons button) => new(
        new GamePadThumbSticks(), new GamePadTriggers(),
        new GamePadButtons(button), new GamePadDPad());

    private static GamePadState PadUp() => new(
        new GamePadThumbSticks(), new GamePadTriggers(),
        new GamePadButtons(), new GamePadDPad());

    private static readonly MouseState NoMouse = new();
    private static readonly GamePadState NoPad = PadUp();

    #endregion

    [Fact]
    public void Update_KeyHeld_IsHeldTrue()
    {
        var action = new InputAction("Test", keys: [Keys.Space]);
        action.Update(KbDown(Keys.Space), KbDown(Keys.Space), NoMouse, NoMouse, NoPad, NoPad);
        Assert.True(action.IsHeld);
        Assert.False(action.IsPressed);
        Assert.False(action.IsReleased);
    }

    [Fact]
    public void Update_KeyJustPressed_IsPressedTrue()
    {
        var action = new InputAction("Test", keys: [Keys.Space]);
        action.Update(KbDown(Keys.Space), KbUp(), NoMouse, NoMouse, NoPad, NoPad);
        Assert.True(action.IsPressed);
        Assert.True(action.IsHeld);
        Assert.False(action.IsReleased);
    }

    [Fact]
    public void Update_KeyJustReleased_IsReleasedTrue()
    {
        var action = new InputAction("Test", keys: [Keys.Space]);
        action.Update(KbUp(), KbDown(Keys.Space), NoMouse, NoMouse, NoPad, NoPad);
        Assert.True(action.IsReleased);
        Assert.False(action.IsHeld);
        Assert.False(action.IsPressed);
    }

    [Fact]
    public void Update_KeyUp_AllFalse()
    {
        var action = new InputAction("Test", keys: [Keys.Space]);
        action.Update(KbUp(), KbUp(), NoMouse, NoMouse, NoPad, NoPad);
        Assert.False(action.IsHeld);
        Assert.False(action.IsPressed);
        Assert.False(action.IsReleased);
    }

    [Fact]
    public void Update_MouseButtonJustPressed_IsPressedTrue()
    {
        var action = new InputAction("Test", mouseButtons: [MouseButton.Left]);
        MouseState down = MouseBtn(MouseButton.Left, ButtonState.Pressed);
        MouseState up   = MouseBtn(MouseButton.Left, ButtonState.Released);
        action.Update(KbUp(), KbUp(), down, up, NoPad, NoPad);
        Assert.True(action.IsPressed);
    }

    [Fact]
    public void Update_GamepadButtonJustPressed_IsPressedTrue()
    {
        var action = new InputAction("Test", padButtons: [Buttons.A]);
        action.Update(KbUp(), KbUp(), NoMouse, NoMouse, PadDown(Buttons.A), NoPad);
        Assert.True(action.IsPressed);
        Assert.True(action.IsHeld);
    }

    [Fact]
    public void Update_MultipleBindings_AnyPressedIsTrue()
    {
        var action = new InputAction("Test",
            keys: [Keys.Space],
            padButtons: [Buttons.A]);
        // Only gamepad pressed
        action.Update(KbUp(), KbUp(), NoMouse, NoMouse, PadDown(Buttons.A), NoPad);
        Assert.True(action.IsPressed);
    }

    [Fact]
    public void Update_NoBindings_AllFalse()
    {
        var action = new InputAction("Empty");
        action.Update(KbDown(Keys.Space), KbUp(), NoMouse, NoMouse, PadDown(Buttons.A), NoPad);
        Assert.False(action.IsHeld);
        Assert.False(action.IsPressed);
        Assert.False(action.IsReleased);
    }

    [Fact]
    public void GetBindings_ReturnsAllBindingsFlattened()
    {
        var action = new InputAction("Test",
            keys: [Keys.A],
            padButtons: [Buttons.B],
            mouseButtons: [MouseButton.Right]);

        InputBinding[] bindings = action.GetBindings();
        Assert.Equal(3, bindings.Length);
        Assert.Contains(bindings, b => b.DeviceType == DeviceType.Keyboard && b.Code == (int)Keys.A);
        Assert.Contains(bindings, b => b.DeviceType == DeviceType.Gamepad && b.Code == (int)Buttons.B);
        Assert.Contains(bindings, b => b.DeviceType == DeviceType.Mouse && b.Code == (int)MouseButton.Right);
    }

    [Fact]
    public void GetBindings_EmptyAction_ReturnsEmpty()
    {
        var action = new InputAction("Empty");
        Assert.Empty(action.GetBindings());
    }
}
