namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 8/10 — demos ScrollView (vertical and horizontal content overflow).</summary>
public sealed class UIScene_ScrollView : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();

    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");
        BuildUI();
    }

    private void BuildUI()
    {
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 16 };

        var backBtn = new Button(_font, "← Menú");
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        root.Add(backBtn);
        root.Add(new Label { Font = _font, Text = "Scene 8/10: ScrollView", Color = Color.DimGray });
        root.Add(new Label { Font = _font, Text = "Rueda del ratón para desplazar", Color = Color.Yellow, HAlign = HAlign.Center });

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };

        // ── Vertical scroll: 40 rows in a 200 px tall viewport ──────────────────
        var scrollV = new ScrollView(Core.GraphicsDevice)
        {
            FixedSize = new Vector2(300, 200),
            Pixel = _pixel,
            BackColor = new Color(25, 35, 50),
            BorderColor = new Color(80, 130, 200),
        };
        var contentV = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
        for (int i = 1; i <= 40; i++)
        {
            contentV.Add(new Label
            {
                Font = _font,
                Text = $"  Elemento {i:00}  —  entrada de lista vertical",
                Color = i % 5 == 0 ? new Color(100, 200, 255) : new Color(200, 210, 220),
            });
        }
        scrollV.Add(contentV);

        var vStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        vStack.Add(new Label { Font = _font, Text = "Scroll vertical  (40 elementos)", Color = new Color(80, 180, 255) });
        vStack.Add(scrollV);
        row.Add(vStack);

        // ── Horizontal scroll: wide entries clipped to a 260 px wide viewport ──
        var scrollH = new ScrollView(Core.GraphicsDevice)
        {
            FixedSize = new Vector2(260, 200),
            Pixel = _pixel,
            BackColor = new Color(35, 25, 50),
            BorderColor = new Color(180, 100, 220),
        };
        var contentH = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
        Color[] palette =
        [
            new Color(255, 100, 100), new Color(100, 220, 100), new Color(100, 140, 255),
            new Color(255, 220, 60),  new Color(255, 130, 40),  new Color(60, 220, 210),
            new Color(220, 100, 220), new Color(180, 255, 100), new Color(255, 180, 80),
            new Color(80, 200, 255),  new Color(255, 80, 160),  new Color(160, 255, 200),
        ];
        for (int i = 0; i < palette.Length; i++)
        {
            contentH.Add(new Label
            {
                Font = _font,
                Text = $"  ▌▌▌  Color {i + 1:00} — entrada con texto bastante largo que desborda  ▌▌▌  ",
                Color = palette[i],
            });
        }
        scrollH.Add(contentH);

        var hStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        hStack.Add(new Label { Font = _font, Text = "Scroll vertical  (contenido ancho clippeado)", Color = new Color(200, 100, 255) });
        hStack.Add(scrollH);
        row.Add(hStack);

        root.Add(row);
        _uiRoot.Add(root);
    }

    public override void Update(GameTime gameTime)
    {
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
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
