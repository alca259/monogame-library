using Alca.MonoGame.Kernel.Graphics.Fonts;
using Alca.MonoGame.Kernel.UI.Core;

namespace Alca.MonoGame.Demo.Scenes;

/// <summary>Scene 27 — BitmapFontRenderer text rendering demo.</summary>
public sealed class BitmapFontScene : Scene
{
    private readonly UIRoot _uiRoot = new();
    private readonly UIInteractionManager _interactionManager = new();
    private Texture2D _pixel = null!;
    private SpriteFont _font = null!;

    private BitmapFontRenderer _bitmapRenderer = null!;
    private bool _bitmapLoaded;

    private string _previewText = "¡Hola Mundo! ABCDEFabcdef 0123456789";
    private float _scale = 1f;
    private Color _textColor = Color.White;
    private bool _rainbow;
    private float _rainbowTime;

    private readonly System.Text.StringBuilder _animSb = new(64);

    public override void LoadContent()
    {
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _font = Content.Load<SpriteFont>("DefaultFont");

        _bitmapRenderer = new BitmapFontRenderer();
        try
        {
            _bitmapRenderer.Load(Content, "Fonts/DefaultBitmapFont");
            _bitmapLoaded = true;
        }
        catch { _bitmapLoaded = false; }

        BuildUI();
    }

    private void BuildUI()
    {
        var controls = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

        var backBtn = new Button(_font, "← Menú") { BackgroundPixel = _pixel };
        backBtn.Clicked += () => Core.SceneManager.RequestChange(Core.GetService<UIScene_Menu>());
        controls.Add(backBtn);

        controls.Add(new Label { Font = _font, Text = "BitmapFont Demo", Color = Color.Yellow });

        if (!_bitmapLoaded)
            controls.Add(new Label { Font = _font, Text = "⚠ Fonts/DefaultBitmapFont.fnt no encontrado — usando SpriteFont", Color = Color.Orange });

        controls.Add(new Label { Font = _font, Text = "Texto de prueba:", Color = Color.LightGray });
        var textBox = new TextBox(_font, _pixel, Core.Window);
        textBox.SetText(_previewText);
        textBox.TextChanged += t => _previewText = t;
        controls.Add(textBox);

        controls.Add(new Label { Font = _font, Text = "Escala (0.5–4.0):", Color = Color.LightGray });
        var scaleSlider = new Slider(_pixel) { MinValue = 0.5f, MaxValue = 4f, Step = 0.1f };
        scaleSlider.Value = _scale;
        scaleSlider.ValueChanged += v => _scale = v;
        controls.Add(scaleSlider);

        controls.Add(new Label { Font = _font, Text = "Color:", Color = Color.LightGray });
        var colorPicker = new ColorPickerRGB(Core.GraphicsDevice, _font, _pixel);
        colorPicker.ColorChanged += c => _textColor = c;
        controls.Add(colorPicker);

        controls.Add(new Label { Font = _font, Text = "Animación arco iris:", Color = Color.LightGray });
        var rainbowChk = new Checkbox(_font, "") { Pixel = _pixel, IsChecked = _rainbow };
        rainbowChk.CheckedChanged += v => _rainbow = v;
        controls.Add(rainbowChk);

        var anchor = new AnchorLayout();
        anchor.SetAnchor(controls, Anchor.TopRight, new Vector2(10, 10));
        _uiRoot.Add(anchor);
    }

    public override void Update(GameTime gameTime)
    {
        _rainbowTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        _uiRoot.Update(gameTime);
        Rectangle screen = new(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
        _uiRoot.Measure(new Vector2(screen.Width, screen.Height));
        _uiRoot.Arrange(screen);
        _interactionManager.Update(_uiRoot, Core.Input.Mouse);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(20, 20, 30));

        Core.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        if (_bitmapLoaded && _bitmapRenderer.Font != null)
        {
            Color drawColor = _rainbow ? RainbowColor(_rainbowTime) : _textColor;
            _bitmapRenderer.DrawString(Core.SpriteBatch, _previewText, new Vector2(40, 120), drawColor, _scale, 0f);
            _bitmapRenderer.DrawString(Core.SpriteBatch, "Texto mediano (escala 1.5×)", new Vector2(40, 200), Color.LightBlue, 1.5f, 0f);
            _bitmapRenderer.DrawString(Core.SpriteBatch, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", new Vector2(40, 280), Color.LightGreen, 1f, 0f);
            _bitmapRenderer.DrawString(Core.SpriteBatch, "áéíóúüñ ¡¿ — caracteres especiales", new Vector2(40, 340), Color.Orange, 1f, 0f);
        }
        else
        {
            Color drawColor = _rainbow ? RainbowColor(_rainbowTime) : _textColor;
            Core.SpriteBatch.DrawString(_font, _previewText, new Vector2(40, 120), drawColor);
            Core.SpriteBatch.DrawString(_font, "(SpriteFont — BitmapFont no cargado)", new Vector2(40, 160), Color.DimGray);
            Core.SpriteBatch.DrawString(_font, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", new Vector2(40, 280), Color.LightGreen);
        }

        Core.SpriteBatch.End();
        _uiRoot.DrawAll(Core.SpriteBatch);
    }

    private static Color RainbowColor(float t)
    {
        float r = (MathF.Sin(t * 2f) + 1f) * 0.5f;
        float g = (MathF.Sin(t * 2f + MathHelper.TwoPi / 3f) + 1f) * 0.5f;
        float b = (MathF.Sin(t * 2f + MathHelper.TwoPi * 2f / 3f) + 1f) * 0.5f;
        return new Color(r, g, b);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _pixel?.Dispose();
        base.Dispose(disposing);
    }
}
