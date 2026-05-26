namespace Alca.MonoGame.Kernel.Input;

/// <summary>Manages the state of all input devices and propagates frame-by-frame input to the active <see cref="InputActionMap"/>.</summary>
public sealed class InputManager
{
    /// <summary>The GamePads property is an array because MonoGame supports up to four gamepads simultaneously. Each gamepad is associated with a PlayerIndex (0-3).</summary>
    private const int MAX_GAMEPADS = 4;

    private InputActionMap? _activeMap;
    private Vector2 _mousePosition;

    /// <summary>Gets the state information of keyboard input.</summary>
    public KeyboardInfo Keyboard { get; private set; }

    /// <summary>Gets the state information of mouse input.</summary>
    public MouseInfo Mouse { get; private set; }

    /// <summary>Gets the state information of a gamepad.</summary>
    public GamePadInfo[] GamePads { get; private set; }

    /// <summary>Gets the current mouse position in screen space as a pre-cached Vector2 (no inline allocation).</summary>
    public Vector2 MousePosition => _mousePosition;

    /// <summary>Initializes a new instance of the InputManager class.</summary>
    public InputManager()
    {
        Keyboard = new KeyboardInfo();
        Mouse = new MouseInfo();
        GamePads = new GamePadInfo[MAX_GAMEPADS];
        for (int i = 0; i < MAX_GAMEPADS; i++)
        {
            GamePads[i] = new GamePadInfo((PlayerIndex)i);
        }
    }

    /// <summary>Loads the specified action map as the active map. Updated every frame until unloaded.</summary>
    /// <param name="map">The action map to activate.</param>
    public void LoadMap(InputActionMap map) => _activeMap = map;

    /// <summary>Unloads the active action map.</summary>
    public void UnloadMap() => _activeMap = null;

    /// <summary>Returns true if the specified key was just pressed this frame (up → down transition).</summary>
    public bool IsKeyPressed(Keys key) => Keyboard.WasKeyJustPressed(key);

    /// <summary>Returns true if the specified key is currently held down.</summary>
    public bool IsKeyHeld(Keys key) => Keyboard.IsKeyDown(key);

    /// <summary>Returns true if the specified key was just released this frame (down → up transition).</summary>
    public bool IsKeyReleased(Keys key) => Keyboard.WasKeyJustReleased(key);

    /// <summary>Updates the state information of all input devices and propagates to the active action map.</summary>
    public void Update(GameTime gameTime)
    {
        Keyboard.Update();
        Mouse.Update();

        _mousePosition.X = Mouse.Position.X;
        _mousePosition.Y = Mouse.Position.Y;

        for (int i = 0; i < MAX_GAMEPADS; i++)
        {
            GamePads[i].Update(gameTime);
        }

        _activeMap?.Update(
            Keyboard.CurrentState, Keyboard.PreviousState,
            Mouse.CurrentState, Mouse.PreviousState,
            GamePads[0].CurrentState, GamePads[0].PreviousState);
    }
}
