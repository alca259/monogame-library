namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 4/10 — demos Slider (H + V) and ProgressBar (H + gradient V).</summary>
public sealed class UIScene_Sliders : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private Slider _sliderH = null!;
    private Slider _sliderV = null!;
    private ProgressBar _progressH = null!;
    private ProgressBar _progressV = null!;
    private Label _sliderHLabel = null!;
    private Label _sliderVLabel = null!;

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
        root.Add(new Label { Font = _font, Text = "Scene 4/10: Sliders", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Sliders & Progress Bars", Color = Color.Yellow, HAlign = HAlign.Center });

        _sliderH = new Slider(_pixel) { MinValue = 0, MaxValue = 100, Step = 1, TabIndex = 0 };
        _sliderH.ValueChanged += v =>
        {
            _sliderHLabel.Text = $"Slider H: {v:F0}";
            _progressH.Value = v / 100f;
        };
        _focusManager.Register(_sliderH);
        root.Add(_sliderH);

        _sliderHLabel = new Label { Font = _font, Text = "Slider H: 0", Color = Color.LightBlue };
        root.Add(_sliderHLabel);

        _progressH = new ProgressBar { Pixel = _pixel, Value = 0f };
        root.Add(_progressH);

        var hRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };

        _sliderV = new Slider(_pixel)
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.01f,
            Orientation = Orientation.Vertical,
            TabIndex = 1,
        };
        _sliderV.ValueChanged += v => { _sliderVLabel.Text = $"Slider V: {v:F2}"; };
        _focusManager.Register(_sliderV);
        hRow.Add(_sliderV);

        var vStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        _sliderVLabel = new Label { Font = _font, Text = "Slider V: 0.00", Color = Color.LightGreen };
        vStack.Add(_sliderVLabel);

        _progressV = new ProgressBar
        {
            Pixel = _pixel,
            Orientation = Orientation.Vertical,
            ColorGradient = true,
            LowColor = Color.Green,
            HighColor = Color.Red,
            Value = 0f,
        };
        vStack.Add(_progressV);
        hRow.Add(vStack);
        root.Add(hRow);

        var resetBtn = new Button(_font, "Reset") { TabIndex = 2 };
        resetBtn.Clicked += () =>
        {
            _sliderH.Value = 0f;
            _sliderV.Value = 0f;
            _progressH.Value = 0f;
            _progressV.Value = 0f;
            _sliderHLabel.Text = "Slider H: 0";
            _sliderVLabel.Text = "Slider V: 0.00";
        };
        _focusManager.Register(resetBtn);
        root.Add(resetBtn);

        _uiRoot.Add(root);
    }

    public override void Update(GameTime gameTime)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _progressV.Value += delta * 0.15f;
        if (_progressV.Value > 1f) _progressV.Value = 0f;

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
