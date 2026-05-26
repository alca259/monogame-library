namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 3/10 — demos TextArea with character counter and clear button.</summary>
public sealed class UIScene_TextArea : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private TextArea _textArea = null!;
    private Label _charCountLabel = null!;

    private const int MaxChars = 500;

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
        root.Add(new Label { Font = _font, Text = "Scene 3/10: TextArea", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "TextArea Demo — escribe texto largo", Color = Color.Yellow, HAlign = HAlign.Center });

        _textArea = new TextArea(_font, _pixel, Core.Window)
        {
            WordWrap = true,
            MaxLength = MaxChars,
            TabIndex = 0,
        };
        _textArea.TextChanged += _ => UpdateCharCount();
        _focusManager.Register(_textArea);
        root.Add(_textArea);

        _charCountLabel = new Label { Font = _font, Text = $"0 / {MaxChars} chars", Color = Color.LightGray };
        root.Add(_charCountLabel);

        var clearBtn = new Button(_font, "Limpiar") { TabIndex = 1 };
        clearBtn.Clicked += () => { _textArea.SetText(string.Empty); };
        _focusManager.Register(clearBtn);
        root.Add(clearBtn);

        _uiRoot.Add(root);
    }

    private void UpdateCharCount()
    {
        _charCountLabel.Text = $"{_textArea.Text.Length} / {MaxChars} chars";
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
