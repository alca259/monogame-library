namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 6/10 — demos the color picker with a live preview swatch.</summary>
public sealed class UIScene_ColorPicker : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Panel _swatchPanel = null!;
    private Label _hexLabel = null!;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 6/10: Color Picker", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Color Picker Demo", Color = Color.Yellow, HAlign = HAlign.Center });

        var picker = new ColorPickerRGB(Core.GraphicsDevice, _font, _pixel);
        picker.ColorChanged += UpdateSwatch;
        root.Add(picker);

        var swatchRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };

        _swatchPanel = new Panel
        {
            BackgroundTexture = _pixel,
            BackgroundColor = Color.Red,
            BorderColor = Color.Gray,
            BorderThickness = 1,
        };
        swatchRow.Add(_swatchPanel);

        _hexLabel = new Label { Font = _font, Text = "#FF0000", Color = Color.LightGray };
        swatchRow.Add(_hexLabel);

        root.Add(swatchRow);
        _uiRoot.Add(root);
    }

    private void UpdateSwatch(Color color)
    {
        _swatchPanel.BackgroundColor = color;
        _hexLabel.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
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
