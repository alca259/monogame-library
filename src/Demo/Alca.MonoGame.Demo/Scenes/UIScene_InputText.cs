namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 2/10 — demos TextBox, NumericBox, and PasswordBox in a GridLayout.</summary>
public sealed class UIScene_InputText : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private TextBox _textBox = null!;
    private NumericBox _numericBox = null!;
    private PasswordBox _passwordBox = null!;
    private Label _valuesLabel = null!;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var outerStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        outerStack.Add(backBtn);
        outerStack.Add(new Label { Font = _font, Text = "Scene 2/10: Input Text", Color = Color.DimGray });
        outerStack.Add(new Label { Font = _font, Text = "Text Input Demo  (Tab: next field)", Color = Color.Yellow, HAlign = HAlign.Center });

        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(160));
        grid.ColumnDefinitions.Add(GridTrack.Star(1));
        grid.RowDefinitions.Add(GridTrack.Auto());
        grid.RowDefinitions.Add(GridTrack.Auto());
        grid.RowDefinitions.Add(GridTrack.Auto());
        grid.RowDefinitions.Add(GridTrack.Auto());

        var lbl0 = new Label { Font = _font, Text = "TextBox:" };
        grid.Add(lbl0);
        grid.SetCell(lbl0, 0, 0);

        _textBox = new TextBox(_font, _pixel, Core.Window) { Placeholder = "Escribe aquí...", TabIndex = 0 };
        _textBox.TextChanged += _ => UpdateValues();
        _focusManager.Register(_textBox);
        grid.Add(_textBox);
        grid.SetCell(_textBox, 0, 1);

        var lbl1 = new Label { Font = _font, Text = "NumericBox:" };
        grid.Add(lbl1);
        grid.SetCell(lbl1, 1, 0);

        _numericBox = new NumericBox(_font, _pixel, Core.Window) { MinValue = 0, MaxValue = 100, Step = 1, IsInt = true, TabIndex = 1 };
        _numericBox.SetText("50");
        _numericBox.TextChanged += _ => UpdateValues();
        _focusManager.Register(_numericBox);
        grid.Add(_numericBox);
        grid.SetCell(_numericBox, 1, 1);

        var lbl2 = new Label { Font = _font, Text = "Password:" };
        grid.Add(lbl2);
        grid.SetCell(lbl2, 2, 0);

        _passwordBox = new PasswordBox(_font, _pixel, Core.Window) { Placeholder = "Contraseña", TabIndex = 2 };
        _passwordBox.TextChanged += _ => UpdateValues();
        _focusManager.Register(_passwordBox);
        grid.Add(_passwordBox);
        grid.SetCell(_passwordBox, 2, 1);

        _valuesLabel = new Label { Font = _font, Text = "Valores: — | 50 | —", Color = Color.LightGreen };
        grid.Add(_valuesLabel);
        grid.SetCell(_valuesLabel, 3, 0, 1, 2);

        outerStack.Add(grid);
        _uiRoot.Add(outerStack);
    }

    private void UpdateValues()
    {
        _valuesLabel.Text = $"Valores: \"{_textBox.Text}\" | {_numericBox.IntValue} | ({_passwordBox.Text.Length} chars)";
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
