using Alca.MonoGame.Kernel.Input;

namespace Alca.MonoGame.Kernel.UI.Input;

/// <summary>Abstraction over UI input that decouples controls from keyboard/mouse/gamepad specifics.</summary>
public sealed class UIInputContext
{
    /// <summary>The most recently updated UIInputContext. Set by Update(); available to controls inside the kernel assembly.</summary>
    internal static UIInputContext? Current { get; private set; }

    /// <summary>Current pointer position in screen space, or null if no pointer is available.</summary>
    public Point? PointerPosition { get; set; }

    /// <summary>Whether the primary pointer button is currently pressed.</summary>
    public bool IsPointerButtonPressed { get; set; }

    /// <summary>Whether the primary pointer button was just pressed this frame.</summary>
    public bool WasPointerButtonJustPressed { get; set; }

    /// <summary>Whether the primary pointer button was just released this frame.</summary>
    public bool WasPointerButtonJustReleased { get; set; }

    /// <summary>Mouse wheel scroll delta since last frame (positive = up, negative = down).</summary>
    public int PointerScrollDelta { get; set; }

    /// <summary>Action triggered when moving/navigating up (arrow key up, D-Pad up, gamepad stick up).</summary>
    public InputAction? MoveUp { get; set; }

    /// <summary>Action triggered when moving/navigating down.</summary>
    public InputAction? MoveDown { get; set; }

    /// <summary>Action triggered when moving/navigating left.</summary>
    public InputAction? MoveLeft { get; set; }

    /// <summary>Action triggered when moving/navigating right.</summary>
    public InputAction? MoveRight { get; set; }

    /// <summary>Action triggered for confirm/select (Enter, Space, gamepad A).</summary>
    public InputAction? Confirm { get; set; }

    /// <summary>Action triggered for cancel/back (Escape, gamepad B).</summary>
    public InputAction? Cancel { get; set; }

    /// <summary>Action triggered for Tab navigation (next focus element).</summary>
    public InputAction? TabNext { get; set; }

    /// <summary>Action triggered for Shift+Tab navigation (previous focus element).</summary>
    public InputAction? TabPrevious { get; set; }

    /// <summary>Action for text navigation (delete character, move cursor in text inputs).</summary>
    public InputAction? Home { get; set; }

    /// <summary>Action for text navigation (delete character, move cursor in text inputs).</summary>
    public InputAction? End { get; set; }

    /// <summary>Action for deleting characters in text inputs.</summary>
    public InputAction? Delete { get; set; }

    /// <summary>Returns true if Shift is currently held (for text selection).</summary>
    public bool IsShiftHeld { get; set; }

    /// <summary>Creates a default context with standard UI controls wired to keyboard/gamepad.</summary>
    public static UIInputContext CreateDefault()
    {
        var moveUp = new InputAction("UI_MoveUp", [Keys.Up, Keys.W], [Buttons.DPadUp], []);
        var moveDown = new InputAction("UI_MoveDown", [Keys.Down, Keys.S], [Buttons.DPadDown], []);
        var moveLeft = new InputAction("UI_MoveLeft", [Keys.Left, Keys.A], [Buttons.DPadLeft], []);
        var moveRight = new InputAction("UI_MoveRight", [Keys.Right, Keys.D], [Buttons.DPadRight], []);
        var confirm = new InputAction("UI_Confirm", [Keys.Enter, Keys.Space], [Buttons.A], []);
        var cancel = new InputAction("UI_Cancel", [Keys.Escape], [Buttons.B], []);
        var tabNext = new InputAction("UI_TabNext", [Keys.Tab], [], []);
        var tabPrevious = new InputAction("UI_TabPrevious", [Keys.LeftShift], [], []);
        var home = new InputAction("UI_Home", [Keys.Home], [], []);
        var end = new InputAction("UI_End", [Keys.End], [], []);
        var delete = new InputAction("UI_Delete", [Keys.Delete], [], []);

        return new UIInputContext
        {
            MoveUp = moveUp,
            MoveDown = moveDown,
            MoveLeft = moveLeft,
            MoveRight = moveRight,
            Confirm = confirm,
            Cancel = cancel,
            TabNext = tabNext,
            TabPrevious = tabPrevious,
            Home = home,
            End = end,
            Delete = delete,
        };
    }

    /// <summary>Updates all input actions from the current and previous input states.</summary>
    internal void Update(
        KeyboardState currKeyboard, KeyboardState prevKeyboard,
        GamePadState currGamePad, GamePadState prevGamePad,
        MouseState currMouse, MouseState prevMouse)
    {
        Current = this;
        PointerPosition = currMouse.Position;
        IsPointerButtonPressed = currMouse.LeftButton == ButtonState.Pressed;
        WasPointerButtonJustPressed = currMouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        WasPointerButtonJustReleased = currMouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;
        PointerScrollDelta = currMouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        IsShiftHeld = currKeyboard.IsKeyDown(Keys.LeftShift) || currKeyboard.IsKeyDown(Keys.RightShift);

        MoveUp?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        MoveDown?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        MoveLeft?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        MoveRight?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        Confirm?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        Cancel?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        TabNext?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        TabPrevious?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        Home?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        End?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
        Delete?.Update(currKeyboard, prevKeyboard, currMouse, prevMouse, currGamePad, prevGamePad);
    }
}
