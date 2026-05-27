using Alca.MonoGame.Kernel.Graphics.Effects;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 24 — RenderTargetManager capture and post-process effect demo.</summary>
public sealed class PostProcessScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private readonly UIOverlayManager _overlayManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private RenderTargetManager? _rtm;
    private Effect? _vignetteEffect;
    private Effect? _grayscaleEffect;
    private bool _vignetteLoaded;
    private bool _grayscaleLoaded;

    private int _effectMode; // 0=None, 1=Vignette, 2=Grayscale, 3=Both
    private float _intensity = 0.5f;
    private float _orbitAngle;

    private Label _rtLabel = null!;
    private Label _effectLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(64);

    // Pre-allocated effect chain array (reused each frame, no GC)
    private readonly Effect?[] _effectChain = new Effect?[2];

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;
        _rtm = new RenderTargetManager(Core.GraphicsDevice, w, h);

        try { _vignetteEffect = Content.Load<Effect>("Shaders/Vignette"); _vignetteLoaded = true; }
        catch { _vignetteLoaded = false; }

        try { _grayscaleEffect = Content.Load<Effect>("Shaders/Grayscale"); _grayscaleLoaded = true; }
        catch { _grayscaleLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        _uiRoot.OverlayManager = _overlayManager;
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "PostProcess Demo", Color = Color.Yellow });

        if (!_vignetteLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Shaders/Vignette.fx no encontrado", Color = Color.Orange });
        if (!_grayscaleLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Shaders/Grayscale.fx no encontrado", Color = Color.Orange });

        bool anyShader = _vignetteLoaded || _grayscaleLoaded;

        controls.Add(new Label { Font = _font, Text = "Efecto:", Color = Color.LightGray });
        var effectDrop = new Dropdown(_overlayManager)
        {
            Pixel = _pixel,
            Font = _font,
            ScreenHeight = Core.GraphicsDevice.Viewport.Height,
            IsEnabled = anyShader
        };
        effectDrop.AddItem("Ninguno");
        effectDrop.AddItem("Vignette");
        effectDrop.AddItem("Grayscale");
        effectDrop.AddItem("Vignette+Grayscale");
        effectDrop.SelectedIndex = 0;
        effectDrop.SelectionChanged += idx => _effectMode = idx;
        controls.Add(effectDrop);

        controls.Add(new Label { Font = _font, Text = "Intensidad:", Color = Color.LightGray });
        var intSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        intSlider.Value = _intensity;
        intSlider.ValueChanged += v =>
        {
            _intensity = v;
            if (_vignetteEffect != null && _vignetteEffect.Parameters["Intensity"] != null)
                _vignetteEffect.Parameters["Intensity"].SetValue(v);
            if (_grayscaleEffect != null && _grayscaleEffect.Parameters["Intensity"] != null)
                _grayscaleEffect.Parameters["Intensity"].SetValue(v);
        };
        controls.Add(intSlider);

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;
        _rtLabel = new Label { Font = _font, Text = $"RenderTarget: {w}×{h}", Color = Color.LightGreen };
        _effectLabel = new Label { Font = _font, Text = "Efecto activo: Ninguno", Color = Color.LightGreen };
        controls.Add(_rtLabel);
        controls.Add(_effectLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopLeft, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _orbitAngle += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _sb.Clear();
        _sb.Append("Efecto activo: ");
        _sb.Append(_effectMode switch { 1 => "Vignette", 2 => "Grayscale", 3 => "Vignette+Grayscale", _ => "Ninguno" });
        _effectLabel.Text = _sb.ToString();

        _overlayManager.Update(gameTime);
        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        if (_rtm != null && _effectMode > 0)
        {
            _rtm.BeginCapture();
            DrawWorld();
            _rtm.EndCapture();
            ApplyEffects();
        }
        else
        {
            Core.GraphicsDevice.Clear(new Color(15, 20, 30));
            DrawWorld();
        }

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private void DrawWorld()
    {
        if (_effectMode == 0)
            Core.GraphicsDevice.Clear(new Color(15, 20, 30));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        Color[] palette = [Color.Red, Color.Lime, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan];
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            Core.SpriteBatch.Draw(_pixel, new Rectangle(300 + col * 200, 100 + row * 180, 160, 140), palette[i] * 0.7f);
        }

        int w = Core.GraphicsDevice.Viewport.Width;
        int h = Core.GraphicsDevice.Viewport.Height;
        for (int j = 0; j < 5; j++)
        {
            float a = _orbitAngle + j * MathHelper.TwoPi / 5;
            float rx = w / 2f + MathF.Cos(a) * 180f;
            float ry = h / 2f + MathF.Sin(a) * 180f;
            Core.SpriteBatch.Draw(_pixel, new Rectangle((int)rx - 15, (int)ry - 15, 30, 30), Color.White);
        }

        Core.SpriteBatch.End();
    }

    private void ApplyEffects()
    {
        if (_rtm == null) return;

        int chainCount = 0;
        if (_effectMode == 1 && _vignetteLoaded)
            _effectChain[chainCount++] = _vignetteEffect;
        else if (_effectMode == 2 && _grayscaleLoaded)
            _effectChain[chainCount++] = _grayscaleEffect;
        else if (_effectMode == 3)
        {
            if (_vignetteLoaded) _effectChain[chainCount++] = _vignetteEffect;
            if (_grayscaleLoaded) _effectChain[chainCount++] = _grayscaleEffect;
        }

        if (chainCount == 0) return;
        if (chainCount == 1)
            _rtm.Apply(_effectChain[0]!, Core.SpriteBatch);
        else
        {
            var span = new Effect[chainCount];
            for (int i = 0; i < chainCount; i++) span[i] = _effectChain[i]!;
            _rtm.ApplyChain(span, Core.SpriteBatch);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rtm?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
