namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 5/10 — demos Dropdown, RadioGroup, and RadioButton.</summary>
public sealed class UIScene_Selection : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();
    private readonly UIOverlayManager _overlayManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Label _dropdownLabel = null!;
    private Label _radioLabel = null!;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        _uiRoot.OverlayManager = _overlayManager;
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 5/10: Selection", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Dropdown & Radio Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var dropdown = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height,
            TabIndex = 0,
        };
        dropdown.AddItem("Opción A");
        dropdown.AddItem("Opción B");
        dropdown.AddItem("Opción C");
        dropdown.AddItem("Opción D");
        _dropdownLabel = new Label { Font = _font, Text = "Dropdown: —", Color = Color.LightGreen };
        dropdown.SelectionChanged += i => { _dropdownLabel.Text = $"Dropdown: {dropdown.SelectedText}"; };
        _focusManager.Register(dropdown);
        root.Add(dropdown);
        root.Add(_dropdownLabel);

        var group = new RadioGroup();
        var radio1 = new RadioButton(_font, _pixel, "Radio 1", group) { TabIndex = 1 };
        var radio2 = new RadioButton(_font, _pixel, "Radio 2", group) { TabIndex = 2 };
        var radio3 = new RadioButton(_font, _pixel, "Radio 3", group) { TabIndex = 3 };
        _radioLabel = new Label { Font = _font, Text = "Radio: —", Color = Color.LightBlue };
        group.SelectionChanged += i => { _radioLabel.Text = $"Radio: Radio {i + 1}"; };
        _focusManager.Register(radio1);
        _focusManager.Register(radio2);
        _focusManager.Register(radio3);
        root.Add(radio1);
        root.Add(radio2);
        root.Add(radio3);
        root.Add(_radioLabel);

        _uiRoot.Add(root);
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        _overlayManager.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse, _focusManager);
        _focusManager.Update(Core.Input.Keyboard, Core.Input.GamePads[0]);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
