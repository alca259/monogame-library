namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 10/10 — demos UIFocusManager with Tab and arrow-key navigation over a 3×3 button grid.</summary>
public sealed class UIScene_Focus : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIFocusManager _focusManager = new();

    private SpriteFont _font = null!;

    private readonly Button[] _buttons = new Button[9];
    private Label _focusLabel = null!;
    private Label _clickLabel = null!;
    private int _lastFocusedIdx = -1;

    public override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 10/10: Focus", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Focus Demo  (Tab / arrows to navigate, Space/Enter to click)", Color = Color.Yellow, HAlign = HAlign.Center });

        _focusLabel = new Label { Font = _font, Text = "Foco actual: —", Color = Color.LightGreen };
        _clickLabel = new Label { Font = _font, Text = "Último click: —", Color = Color.Orange };
        root.Add(_focusLabel);
        root.Add(_clickLabel);

        // 3×3 grid of buttons
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(120));
        grid.ColumnDefinitions.Add(GridTrack.Fixed(120));
        grid.ColumnDefinitions.Add(GridTrack.Fixed(120));
        grid.RowDefinitions.Add(GridTrack.Auto());
        grid.RowDefinitions.Add(GridTrack.Auto());
        grid.RowDefinitions.Add(GridTrack.Auto());

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int idx = r * 3 + c;
                var btn = new Button(_font, $"F{idx + 1}")
                {
                    TabIndex = idx,
                    FocusNeighborUp = r > 0 ? idx - 3 : 6 + c,
                    FocusNeighborDown = r < 2 ? idx + 3 : c,
                    FocusNeighborLeft = c > 0 ? idx - 1 : r * 3 + 2,
                    FocusNeighborRight = c < 2 ? idx + 1 : r * 3,
                };
                int capturedIdx = idx;
                btn.Clicked += () =>
                {
                    _clickLabel.Text = $"Último click: F{capturedIdx + 1}";
                };
                _buttons[idx] = btn;
                _focusManager.Register(btn);
                grid.Add(btn);
                grid.SetCell(btn, r, c);
            }
        }

        root.Add(grid);

        _uiRoot.Add(root);
        _focusManager.SetFocus(_buttons[0]);
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse, _focusManager);
        _focusManager.Update(Core.Input.Keyboard, Core.Input.GamePads[0]);

        if (_focusManager.FocusedElement is Button focused && focused.TabIndex != _lastFocusedIdx)
        {
            _lastFocusedIdx = focused.TabIndex;
            _focusLabel.Text = $"Foco actual: F{_lastFocusedIdx + 1}";
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
