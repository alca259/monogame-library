namespace Alca.MonoGame.Kernel.Input;

public sealed class MouseInfo
{
    /// <summary>Gets the state of mouse input during the previous update cycle.</summary>
    public MouseState PreviousState { get; private set; }
    
    /// <summary>Gets the state of mouse input during the current input cycle.</summary>
    public MouseState CurrentState { get; private set; }

    /// <summary>Gets or Sets the current position of the mouse cursor in screen space.</summary>
    public Point Position
    {
        get => CurrentState.Position;
        set => SetPosition(value.X, value.Y);
    }

    /// <summary>Gets or Sets the current x-coordinate position of the mouse cursor in screen space.</summary>
    public int X
    {
        get => CurrentState.X;
        set => SetPosition(value, CurrentState.Y);
    }

    /// <summary>Gets or Sets the current y-coordinate position of the mouse cursor in screen space.</summary>
    public int Y
    {
        get => CurrentState.Y;
        set => SetPosition(CurrentState.X, value);
    }

    /// <summary>Gets the difference in the mouse cursor position between the previous and current frame.</summary>
    public Point PositionDelta => CurrentState.Position - PreviousState.Position;

    /// <summary>Gets the difference in the mouse cursor x-position between the previous and current frame.</summary>
    public int XDelta => CurrentState.X - PreviousState.X;

    /// <summary>Gets the difference in the mouse cursor y-position between the previous and current frame.</summary>
    public int YDelta => CurrentState.Y - PreviousState.Y;

    /// <summary>Gets a value that indicates if the mouse cursor moved between the previous and current frames.</summary>
    public bool WasMoved => PositionDelta != Point.Zero;

    /// <summary>Gets the cumulative value of the mouse scroll wheel since the start of the game.</summary>
    public int ScrollWheel => CurrentState.ScrollWheelValue;

    /// <summary>Gets the value of the scroll wheel between the previous and current frame.</summary>
    public int ScrollWheelDelta => CurrentState.ScrollWheelValue - PreviousState.ScrollWheelValue;


    /// <summary>Creates a new MouseInfo instance.</summary>
    public MouseInfo()
    {
        PreviousState = new MouseState();
        CurrentState = Mouse.GetState();
    }
    /// <summary>Updates the state information about mouse input.</summary>
    public void Update()
    {
        PreviousState = CurrentState;
        CurrentState = Mouse.GetState();
    }

    /// <summary>Returns a value that indicates whether the specified mouse button is currently down.</summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>true if the specified mouse button is currently down; otherwise, false.</returns>
    public bool IsButtonDown(MouseButton button) => button switch
    {
        MouseButton.Left => CurrentState.LeftButton == ButtonState.Pressed,
        MouseButton.Middle => CurrentState.MiddleButton == ButtonState.Pressed,
        MouseButton.Right => CurrentState.RightButton == ButtonState.Pressed,
        MouseButton.XButton1 => CurrentState.XButton1 == ButtonState.Pressed,
        MouseButton.XButton2 => CurrentState.XButton2 == ButtonState.Pressed,
        _ => false,
    };

    /// <summary>Returns a value that indicates whether the specified mouse button is current up.</summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>true if the specified mouse button is currently up; otherwise, false.</returns>
    public bool IsButtonUp(MouseButton button) => button switch
    {
        MouseButton.Left => CurrentState.LeftButton == ButtonState.Released,
        MouseButton.Middle => CurrentState.MiddleButton == ButtonState.Released,
        MouseButton.Right => CurrentState.RightButton == ButtonState.Released,
        MouseButton.XButton1 => CurrentState.XButton1 == ButtonState.Released,
        MouseButton.XButton2 => CurrentState.XButton2 == ButtonState.Released,
        _ => false,
    };

    /// <summary>Returns a value that indicates whether the specified mouse button was just pressed on the current frame.</summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>true if the specified mouse button was just pressed on the current frame; otherwise, false.</returns>
    public bool WasButtonJustPressed(MouseButton button) => button switch
    {
        MouseButton.Left => CurrentState.LeftButton == ButtonState.Pressed && PreviousState.LeftButton == ButtonState.Released,
        MouseButton.Middle => CurrentState.MiddleButton == ButtonState.Pressed && PreviousState.MiddleButton == ButtonState.Released,
        MouseButton.Right => CurrentState.RightButton == ButtonState.Pressed && PreviousState.RightButton == ButtonState.Released,
        MouseButton.XButton1 => CurrentState.XButton1 == ButtonState.Pressed && PreviousState.XButton1 == ButtonState.Released,
        MouseButton.XButton2 => CurrentState.XButton2 == ButtonState.Pressed && PreviousState.XButton2 == ButtonState.Released,
        _ => false,
    };

    /// <summary>Returns a value that indicates whether the specified mouse button was just released on the current frame.</summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>true if the specified mouse button was just released on the current frame; otherwise, false.</returns>
    public bool WasButtonJustReleased(MouseButton button) => button switch
    {
        MouseButton.Left => CurrentState.LeftButton == ButtonState.Released && PreviousState.LeftButton == ButtonState.Pressed,
        MouseButton.Middle => CurrentState.MiddleButton == ButtonState.Released && PreviousState.MiddleButton == ButtonState.Pressed,
        MouseButton.Right => CurrentState.RightButton == ButtonState.Released && PreviousState.RightButton == ButtonState.Pressed,
        MouseButton.XButton1 => CurrentState.XButton1 == ButtonState.Released && PreviousState.XButton1 == ButtonState.Pressed,
        MouseButton.XButton2 => CurrentState.XButton2 == ButtonState.Released && PreviousState.XButton2 == ButtonState.Pressed,
        _ => false,
    };

    /// <summary>Sets the current position of the mouse cursor in screen space and updates the CurrentState with the new position.</summary>
    /// <param name="x">The x-coordinate location of the mouse cursor in screen space.</param>
    /// <param name="y">The y-coordinate location of the mouse cursor in screen space.</param>
    public void SetPosition(int x, int y)
    {
        Mouse.SetPosition(x, y);
        CurrentState = new MouseState(
            x: x,
            y: y,
            scrollWheel: CurrentState.ScrollWheelValue,
            leftButton: CurrentState.LeftButton,
            middleButton: CurrentState.MiddleButton,
            rightButton: CurrentState.RightButton,
            xButton1: CurrentState.XButton1,
            xButton2: CurrentState.XButton2
        );
    }

}
