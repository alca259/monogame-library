namespace Alca.MonoGame.Kernel.Input;

/// <summary>A logical input action mapped to one or more keyboard keys, mouse buttons, or gamepad buttons.</summary>
public sealed class InputAction
{
    private readonly Keys[] _keyBindings;
    private readonly Buttons[] _padBindings;
    private readonly MouseButton[] _mouseBindings;

    /// <summary>Gets the unique identifier name for this action.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether any binding for this action was just pressed this frame (up → down transition).</summary>
    public bool IsPressed { get; private set; }

    /// <summary>Gets a value indicating whether any binding for this action was just released this frame (down → up transition).</summary>
    public bool IsReleased { get; private set; }

    /// <summary>Gets a value indicating whether any binding for this action is currently held down.</summary>
    public bool IsHeld { get; private set; }

    /// <summary>Creates a new InputAction with the given name and pre-allocated binding arrays.</summary>
    /// <param name="name">The unique identifier for this action.</param>
    /// <param name="keys">Keyboard keys that trigger this action.</param>
    /// <param name="padButtons">Gamepad buttons that trigger this action.</param>
    /// <param name="mouseButtons">Mouse buttons that trigger this action.</param>
    public InputAction(string name, Keys[]? keys = null, Buttons[]? padButtons = null, MouseButton[]? mouseButtons = null)
    {
        Name = name;
        _keyBindings = keys ?? [];
        _padBindings = padButtons ?? [];
        _mouseBindings = mouseButtons ?? [];
    }

    /// <summary>Updates IsPressed, IsReleased, and IsHeld from the provided input states. No heap allocations.</summary>
    internal void Update(
        KeyboardState currK, KeyboardState prevK,
        MouseState currM, MouseState prevM,
        GamePadState currP, GamePadState prevP)
    {
        bool held = false;
        bool pressed = false;
        bool released = false;

        for (int i = 0; i < _keyBindings.Length; i++)
        {
            Keys key = _keyBindings[i];
            bool down = currK.IsKeyDown(key);
            bool wasDown = prevK.IsKeyDown(key);
            held |= down;
            pressed |= down && !wasDown;
            released |= !down && wasDown;
        }

        for (int i = 0; i < _padBindings.Length; i++)
        {
            Buttons btn = _padBindings[i];
            bool down = currP.IsButtonDown(btn);
            bool wasDown = prevP.IsButtonDown(btn);
            held |= down;
            pressed |= down && !wasDown;
            released |= !down && wasDown;
        }

        for (int i = 0; i < _mouseBindings.Length; i++)
        {
            MouseButton mb = _mouseBindings[i];
            bool down = IsMouseDown(currM, mb);
            bool wasDown = IsMouseDown(prevM, mb);
            held |= down;
            pressed |= down && !wasDown;
            released |= !down && wasDown;
        }

        IsHeld = held;
        IsPressed = pressed;
        IsReleased = released;
    }

    /// <summary>Returns all bindings for this action as a new array of <see cref="InputBinding"/> instances.</summary>
    internal InputBinding[] GetBindings()
    {
        int count = _keyBindings.Length + _padBindings.Length + _mouseBindings.Length;
        InputBinding[] result = new InputBinding[count];
        int idx = 0;

        for (int i = 0; i < _keyBindings.Length; i++)
            result[idx++] = new InputBinding { DeviceType = DeviceType.Keyboard, Code = (int)_keyBindings[i] };

        for (int i = 0; i < _padBindings.Length; i++)
            result[idx++] = new InputBinding { DeviceType = DeviceType.Gamepad, Code = (int)_padBindings[i] };

        for (int i = 0; i < _mouseBindings.Length; i++)
            result[idx++] = new InputBinding { DeviceType = DeviceType.Mouse, Code = (int)_mouseBindings[i] };

        return result;
    }

    private static bool IsMouseDown(MouseState state, MouseButton button) => button switch
    {
        MouseButton.Left     => state.LeftButton   == ButtonState.Pressed,
        MouseButton.Right    => state.RightButton  == ButtonState.Pressed,
        MouseButton.Middle   => state.MiddleButton == ButtonState.Pressed,
        MouseButton.XButton1 => state.XButton1     == ButtonState.Pressed,
        MouseButton.XButton2 => state.XButton2     == ButtonState.Pressed,
        _                    => false
    };
}
