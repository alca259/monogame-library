using Alca.MonoGame.Kernel.Tweening;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 34 — TweeningManager and EasingCatalog demo with curve visualization.</summary>
public sealed class TweeningScene : Scene
{
    private sealed class AnimTarget { public float X { get; set; } }

    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private readonly AnimTarget _animObj = new();
    private float _duration = 1.5f;
    private int _selectedIdx;
    private Func<float, float> _selectedEasing = EasingCatalog.Linear;

    // Manual time tracking for continuous loop
    private float _elapsed;
    private const float AnimStartX = 80f;
    private const float AnimEndX   = 800f;

    private Label _progressLabel = null!;

    private static readonly (string Name, Func<float, float> Func)[] Easings =
    {
        ("Linear",      EasingCatalog.Linear),
        ("EaseIn",      EasingCatalog.EaseIn),
        ("EaseOut",     EasingCatalog.EaseOut),
        ("EaseInOut",   EasingCatalog.EaseInOut),
        ("QuadIn",      EasingCatalog.QuadIn),
        ("QuadOut",     EasingCatalog.QuadOut),
        ("QuadInOut",   EasingCatalog.QuadInOut),
        ("CubicIn",     EasingCatalog.CubicIn),
        ("CubicOut",    EasingCatalog.CubicOut),
        ("CubicInOut",  EasingCatalog.CubicInOut),
        ("BounceOut",   EasingCatalog.BounceOut),
        ("ElasticOut",  EasingCatalog.ElasticOut),
        ("BackOut",     EasingCatalog.BackOut),
        ("SineIn",      EasingCatalog.EaseIn),
        ("SineOut",     EasingCatalog.EaseOut),
        ("SineInOut",   EasingCatalog.EaseInOut),
        ("BackIn",      EasingCatalog.BackIn),
        ("BackInOut",   EasingCatalog.BackInOut),
        ("BounceIn",    EasingCatalog.BounceIn),
    };

    private const int CurveSamples = 30;
    private float[][] _easingCurves = null!;

    // Curve grid layout
    private const int CurveCols = 3;
    private const int CurveBoxW = 90;
    private const int CurveBoxH = 55;
    private const int CurveSpacing = 4;
    private const int CurveGridX = 860;
    private const int CurveGridY = 30;

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        // Pre-compute easing curves to avoid allocations in Draw
        _easingCurves = new float[Easings.Length][];
        for (int e = 0; e < Easings.Length; e++)
        {
            _easingCurves[e] = new float[CurveSamples];
            for (int s = 0; s < CurveSamples; s++)
                _easingCurves[e][s] = Math.Clamp(Easings[e].Func(s / (float)(CurveSamples - 1)), -0.3f, 1.3f);
        }

        _uiRoot.OverlayManager = _overlayManager;
        _animObj.X = AnimStartX;
        BuildUI();
    }

    private void BuildUI()
    {
        var left = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        left.Add(backBtn);

        left.Add(new Label { Font = _font, Text = "Tweening Demo", Color = Color.Yellow });

        var dropdown = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height,
        };
        for (int i = 0; i < Easings.Length; i++)
            dropdown.AddItem(Easings[i].Name);

        dropdown.SelectionChanged += i =>
        {
            _selectedIdx = i;
            _selectedEasing = Easings[i].Func;
            _elapsed = 0f;
        };
        left.Add(dropdown);

        var durationSlider = new Slider(_pixel) { MinValue = 0.3f, MaxValue = 3f, Step = 0.1f };
        durationSlider.Value = _duration;
        durationSlider.ValueChanged += v => { _duration = v; _elapsed = 0f; };
        left.Add(durationSlider);
        left.Add(new Label { Font = _font, Text = "Duración (s)", Color = Color.LightGray });

        var resetBtn = new Button(_font, "Play / Reset") { BackgroundPixel = _pixel };
        resetBtn.Clicked += () =>
        {
            _elapsed = 0f;
            _animObj.X = AnimStartX;
        };
        left.Add(resetBtn);

        _progressLabel = new Label { Font = _font, Text = "t: 0.00 | val: 0.000", Color = Color.LightGreen };
        left.Add(_progressLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(left, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    private const float AnimObjY = 450f;

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed += dt;
        if (_elapsed > _duration) _elapsed -= _duration;

        float t = _elapsed / _duration;
        float eased = _selectedEasing(t);
        _animObj.X = AnimStartX + (AnimEndX - AnimStartX) * eased;

        _progressLabel.Text = $"t: {t:F2} | val: {eased:F3}";

        _uiRoot.Update(gameTime);
        _overlayManager.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(15, 15, 25));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Track bar
        Core.SpriteBatch.Draw(_pixel,
            new Rectangle((int)AnimStartX, (int)AnimObjY + 18, (int)(AnimEndX - AnimStartX), 2),
            new Color(60, 60, 80));

        // Animated object
        Core.SpriteBatch.Draw(_pixel,
            new Rectangle((int)_animObj.X - 20, (int)AnimObjY, 40, 40),
            Color.CornflowerBlue);

        // Selected easing name
        Core.SpriteBatch.DrawString(_font,
            $"Easing: {Easings[_selectedIdx].Name}",
            new Vector2(AnimStartX, AnimObjY + 55), Color.Yellow);

        DrawCurveGrid();

        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawCurveGrid()
    {
        for (int e = 0; e < Easings.Length; e++)
        {
            int col = e % CurveCols;
            int row = e / CurveCols;
            int bx = CurveGridX + col * (CurveBoxW + CurveSpacing);
            int by = CurveGridY + row * (CurveBoxH + CurveSpacing + 14);

            bool selected = e == _selectedIdx;
            Color bg = selected ? new Color(40, 40, 70) : new Color(25, 25, 40);
            Color border = selected ? Color.Yellow : new Color(50, 55, 80);

            Core.SpriteBatch.Draw(_pixel, new Rectangle(bx, by, CurveBoxW, CurveBoxH), bg);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(bx, by, CurveBoxW, 1), border);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(bx, by + CurveBoxH - 1, CurveBoxW, 1), border);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(bx, by, 1, CurveBoxH), border);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(bx + CurveBoxW - 1, by, 1, CurveBoxH), border);

            Color curveColor = selected ? Color.Yellow : new Color(80, 160, 255);
            float[] samples = _easingCurves[e];
            for (int s = 0; s < CurveSamples - 1; s++)
            {
                float x1 = bx + 2 + s * (CurveBoxW - 4f) / (CurveSamples - 1);
                float y1 = by + CurveBoxH - 4 - samples[s]       * (CurveBoxH - 8f);
                float x2 = bx + 2 + (s + 1) * (CurveBoxW - 4f) / (CurveSamples - 1);
                float y2 = by + CurveBoxH - 4 - samples[s + 1]   * (CurveBoxH - 8f);
                DrawSegment(new Vector2(x1, y1), new Vector2(x2, y2), curveColor);
            }

            Core.SpriteBatch.DrawString(_font, Easings[e].Name,
                new Vector2(bx + 2, by + CurveBoxH + 1), selected ? Color.Yellow : Color.DimGray,
                0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }
    }

    private void DrawSegment(Vector2 a, Vector2 b, Color color)
    {
        Vector2 d = b - a;
        float len = d.Length();
        if (len < 0.5f) return;
        Core.SpriteBatch.Draw(_pixel, a, null, color, MathF.Atan2(d.Y, d.X), Vector2.Zero,
            new Vector2(len, 1f), SpriteEffects.None, 0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
