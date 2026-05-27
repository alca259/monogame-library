namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 19 — InputManager, InputAction, InputActionMap, and rebinding demo.</summary>
public sealed class InputScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private InputActionMap _actionMap = null!;
    private InputAction _jumpAction = null!;
    private InputAction _fireAction = null!;

    private Label _keyLabel = null!;
    private Label _mouseLabel = null!;
    private Label _jumpLabel = null!;
    private Label _fireLabel = null!;
    private Label _bindingsLabel = null!;
    private Label _statusLabel = null!;

    private enum RebindTarget { None, Jump, Fire }
    private RebindTarget _rebinding = RebindTarget.None;
    private KeyboardState _prevKbState;

    private static readonly Keys[] AllKeys = (Keys[])Enum.GetValues(typeof(Keys));

    private static readonly string BindingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AlcaMonoGameDemo", "bindings.json");

    protected override void PostInitialize()
    {
        base.PostInitialize();
        RebuildActionMap(Keys.W, Keys.Space);
    }

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
        UpdateBindingsLabel();
    }

    private void RebuildActionMap(Keys jumpKey, Keys fireKey)
    {
        _jumpAction = new InputAction("Jump", new[] { jumpKey, Keys.Up });
        _fireAction = new InputAction("Fire", new[] { fireKey }, null, new[] { MouseButton.Left });

        _actionMap = new InputActionMap();
        _actionMap.Register(_jumpAction);
        _actionMap.Register(_fireAction);
        Core.Input.LoadMap(_actionMap);
    }

    private void BuildUI()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 30 };

        // Left: real-time state
        var left = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () =>
        {
            Core.Input.UnloadMap();
            Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        };
        left.Add(backBtn);

        left.Add(new Label { Font = _font, Text = "Input Demo — Actions & Rebinding", Color = Color.Yellow });

        _keyLabel   = new Label { Font = _font, Text = "Tecla pulsada: —",           Color = Color.LightGray };
        _mouseLabel = new Label { Font = _font, Text = "Mouse: 0,0 Btn: —",          Color = Color.LightGray };
        _jumpLabel  = new Label { Font = _font, Text = "Jump: false",                 Color = Color.White };
        _fireLabel  = new Label { Font = _font, Text = "Fire: false",                 Color = Color.White };
        left.Add(_keyLabel);
        left.Add(_mouseLabel);
        left.Add(_jumpLabel);
        left.Add(_fireLabel);
        left.Add(new Label { Font = _font, Text = "W/↑: Jump  |  Space/Click: Fire", Color = Color.DimGray });

        row.Add(left);

        // Right: rebinding
        var right = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        right.Add(new Label { Font = _font, Text = "Rebinding de acciones", Color = Color.Yellow });

        var rebindJumpBtn = new Button(_font, "Rebind Jump") { BackgroundPixel = _pixel };
        rebindJumpBtn.Clicked += () =>
        {
            _rebinding = RebindTarget.Jump;
            _statusLabel.Text = "Estado: Esperando tecla para Jump...";
        };
        right.Add(rebindJumpBtn);

        var rebindFireBtn = new Button(_font, "Rebind Fire") { BackgroundPixel = _pixel };
        rebindFireBtn.Clicked += () =>
        {
            _rebinding = RebindTarget.Fire;
            _statusLabel.Text = "Estado: Esperando tecla para Fire...";
        };
        right.Add(rebindFireBtn);

        _bindingsLabel = new Label { Font = _font, Text = "Jump: W/↑  |  Fire: Space/Click", Color = Color.LightBlue };
        right.Add(_bindingsLabel);

        var saveBtn = new Button(_font, "Guardar bindings") { BackgroundPixel = _pixel };
        saveBtn.Clicked += () =>
        {
            string dir = System.IO.Path.GetDirectoryName(BindingsPath)!;
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            _ = new InputSerializer().Save(_actionMap, BindingsPath);
            _statusLabel.Text = "Estado: Guardado";
        };
        right.Add(saveBtn);

        var loadBtn = new Button(_font, "Cargar bindings") { BackgroundPixel = _pixel };
        loadBtn.Clicked += () =>
        {
            if (!System.IO.File.Exists(BindingsPath)) { _statusLabel.Text = "Estado: Archivo no encontrado"; return; }
            _ = new InputSerializer().Load(BindingsPath).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    _actionMap = t.Result;
                    Core.Input.LoadMap(_actionMap);
                    _jumpAction = _actionMap.Get("Jump") ?? _jumpAction;
                    _fireAction = _actionMap.Get("Fire") ?? _fireAction;
                    UpdateBindingsLabel();
                }
            });
            _statusLabel.Text = "Estado: Cargando...";
        };
        right.Add(loadBtn);

        _statusLabel = new Label { Font = _font, Text = "Estado: Idle", Color = Color.LightGreen };
        right.Add(_statusLabel);

        row.Add(right);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(row, Anchor.TopLeft, new Vector2(20, 20));
        _uiRoot.Add(anchor);
    }

    private void UpdateBindingsLabel()
    {
        if (_bindingsLabel == null) return;
        _bindingsLabel.Text = $"Jump: {_jumpAction.Name}  |  Fire: {_fireAction.Name}";
    }

    public override void Update(GameTime gameTime)
    {
        KeyboardState kbState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        if (_rebinding != RebindTarget.None)
        {
            for (int i = 0; i < AllKeys.Length; i++)
            {
                Keys k = AllKeys[i];
                if (k == Keys.Escape) continue;
                if (kbState.IsKeyDown(k) && !_prevKbState.IsKeyDown(k))
                {
                    if (_rebinding == RebindTarget.Jump)
                        RebuildActionMap(k, _jumpAction.IsPressed ? Keys.Space : Keys.Space);
                    else
                        RebuildActionMap(Keys.W, k);

                    UpdateBindingsLabel();
                    _rebinding = RebindTarget.None;
                    _statusLabel.Text = "Estado: Idle";
                    break;
                }
            }
        }

        _prevKbState = kbState;

        // Update displayed state from pressed keys
        Keys[] pressed = kbState.GetPressedKeys();
        _keyLabel.Text = pressed.Length > 0 ? $"Tecla pulsada: {pressed[0]}" : "Tecla pulsada: —";

        bool lmbDown = mouseState.LeftButton == ButtonState.Pressed;
        Vector2 mPos = Core.Input.MousePosition;
        _mouseLabel.Text = $"Mouse: {(int)mPos.X},{(int)mPos.Y} Btn: {(lmbDown ? "L" : "—")}";

        bool jumpPressed = _jumpAction.IsPressed || _jumpAction.IsHeld;
        bool fireHeld    = _fireAction.IsHeld || _fireAction.IsPressed;
        _jumpLabel.Text = $"Jump: {jumpPressed}";
        _fireLabel.Text = $"Fire: {fireHeld}";

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Core.Input.UnloadMap();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
