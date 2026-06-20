using Alca.MonoGame.Kernel.Graphics.Shaders;
using Alca.MonoGame.Kernel.UI.Controls.Display;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 23 — SpriteMaterial with tint color and alpha shader demo.</summary>
public sealed class SpriteMaterialScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;
    private Texture2D _testSprite = null!;

    private SpriteMaterial? _material;
    private bool _shaderLoaded;
    private bool _useMaterial = true;
    private Color _tintColor = Color.White;
    private float _intensity = 1f;
    private float _alpha = 1f;

    private Label _statusLabel = null!;
    private readonly System.Text.StringBuilder _sb = new(64);

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _testSprite = new Texture2D(Core.GraphicsDevice, 64, 64);
        Color[] spriteData = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            spriteData[y * 64 + x] = new Color(x * 4, y * 4, (x + y) * 2);
        _testSprite.SetData(spriteData);

        try
        {
            var effect = Content.Load<Effect>("Shaders/SpriteTint");
            _material = new SpriteMaterial(effect);
            _shaderLoaded = true;
        }
        catch { _shaderLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "SpriteMaterial Demo", Color = Color.Yellow });

        if (!_shaderLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Shaders/SpriteTint.fx no encontrado — compilar con MGCB", Color = Color.Orange });

        controls.Add(new Label { Font = _font, Text = "Activar material:", Color = Color.LightGray });
        var matChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _useMaterial, IsEnabled = _shaderLoaded };
        matChk.CheckedChanged += v => _useMaterial = v;
        controls.Add(matChk);

        controls.Add(new Label { Font = _font, Text = "Tint color:", Color = Color.LightGray });
        var colorPicker = new ColorPickerRGB(Core.GraphicsDevice, _font, _pixel);
        colorPicker.ColorChanged += c => _tintColor = c;
        controls.Add(colorPicker);

        controls.Add(new Label { Font = _font, Text = "Intensity (0–2):", Color = Color.LightGray });
        var intensitySlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 2f, Step = 0.05f };
        intensitySlider.Value = _intensity;
        intensitySlider.ValueChanged += v => _intensity = v;
        controls.Add(intensitySlider);

        controls.Add(new Label { Font = _font, Text = "Alpha (0–1):", Color = Color.LightGray });
        var alphaSlider = new Slider(_pixel) { MinValue = 0f, MaxValue = 1f, Step = 0.01f };
        alphaSlider.Value = _alpha;
        alphaSlider.ValueChanged += v => _alpha = v;
        controls.Add(alphaSlider);

        _statusLabel = new Label { Font = _font, Text = "Efecto: —", Color = Color.LightGreen };
        controls.Add(_statusLabel);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _sb.Clear();
        _sb.Append("Efecto: ");
        _sb.Append(_useMaterial && _shaderLoaded ? "SpriteTint activo" : "Sin material");
        _statusLabel.Text = _sb.ToString();

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(30, 30, 30));

        bool useEffect = _useMaterial && _material != null;
        if (useEffect)
        {
            _material!.TintColor = _tintColor;
            _material.Alpha = _alpha;
            _material.Apply();
        }

        Effect? activeEffect = useEffect ? _material!.Effect : null;
        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, activeEffect);
        Core.SpriteBatch.Draw(_testSprite, new Vector2(200, 180), Color.White * _alpha);
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        Core.SpriteBatch.Draw(_testSprite, new Vector2(200, 320), Color.White);
        Core.SpriteBatch.DrawString(_font, "Con material", new Vector2(200, 252), Color.LightGray);
        Core.SpriteBatch.DrawString(_font, "Sin material", new Vector2(200, 392), Color.LightGray);
        Core.SpriteBatch.End();

        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testSprite?.Dispose();
            _pixel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
