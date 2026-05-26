namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 9/10 — demos Tooltip appearing on button hover via UIOverlayManager.</summary>
public sealed class UIScene_Tooltip : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();
    private readonly UIOverlayManager _overlayManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Tooltip _tooltip = null!;
    private Button _btn1 = null!;
    private Button _btn2 = null!;
    private Button _btn3 = null!;
    private Button _btn4 = null!;

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
        _tooltip = new Tooltip
        {
            Font = _font,
            Pixel = _pixel,
            ScreenBounds = new Rectangle(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height),
        };
        _overlayManager.Show(_tooltip);

        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 9/10: Tooltip", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Tooltip Demo  (hover buttons to see tooltips)", Color = Color.Yellow, HAlign = HAlign.Center });

        _btn1 = new Button(_font, "Hover 1") { TabIndex = 0 };
        _btn2 = new Button(_font, "Hover 2") { TabIndex = 1 };
        _btn3 = new Button(_font, "Hover 3") { TabIndex = 2 };
        _btn4 = new Button(_font, "Hover 4") { TabIndex = 3 };

        _focusManager.Register(_btn1);
        _focusManager.Register(_btn2);
        _focusManager.Register(_btn3);
        _focusManager.Register(_btn4);

        root.Add(_btn1);
        root.Add(_btn2);
        root.Add(_btn3);
        root.Add(_btn4);

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

        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        string? text = null;

        if (_btn1.IsHovered) text = "Este es el botón principal";
        else if (_btn2.IsHovered) text = "Haz click para continuar";
        else if (_btn3.IsHovered) text = "Texto largo que se clampea al borde de pantalla";
        else if (_btn4.IsHovered) text = "Cuarto botón con tooltip";

        if (text is not null)
        {
            _tooltip.Text = text;
            _tooltip.Show(new Vector2(Core.Input.Mouse.X + 12, Core.Input.Mouse.Y + 12));
        }
        else
        {
            _tooltip.Hide();
        }
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
