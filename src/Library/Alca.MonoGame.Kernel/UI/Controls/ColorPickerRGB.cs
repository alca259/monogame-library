using Alca.MonoGame.Kernel.Graphics;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>A color picker with a color swatch, saturation/value square, hue slider, and hex input. Outputs RGB colors.</summary>
public sealed class ColorPickerRGB : UIContainer
{
    #region Constants

    private const int HueBarHeight = 24;
    private const int HexBoxHeight = 28;
    private const int Spacing = 6;
    private const int ThumbSize = 18;
    private const int RingDiameter = 15;
    private const float SwatchFraction = 0.32f;
    private const int MaxWidth = 380;

    #endregion

    #region Fields

    private readonly TextBox _hexInput;
    private readonly GraphicsDevice? _graphicsDevice;

    private Texture2D? _hueBarTexture;
    private Texture2D? _svSquareTexture;
    private Texture2D? _thumbTexture;
    private Texture2D? _ringTexture;

    // Pre-allocated pixel data arrays — reused across frames to avoid GC pressure.
    private Color[]? _hueBarData;
    private Color[]? _svData;

    // Last state used to generate each texture; skip regeneration when unchanged.
    private Rectangle _lastHueBarBounds;
    private Rectangle _lastSvBounds;
    private float _lastSvHue = -1f;

    private float _hue;
    private float _saturation = 1f;
    private float _brightness = 1f;
    private Color _selectedColor;

    private Rectangle _hueBarBounds;
    private Rectangle _svSquareBounds;
    private Rectangle _swatchBounds;

    private bool _draggingHue;
    private bool _draggingSv;
    private bool _suppressHexCallback;

    #endregion

    #region Properties

    /// <summary>1×1 white pixel texture for borders and the preview swatch.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Border color used around the hue bar and SV square.</summary>
    public Color BorderColor { get; set; } = new Color(120, 120, 120);

    /// <summary>The currently selected color.</summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            ColorPickerUtils.RgbToHsv(_selectedColor, out _hue, out _saturation, out _brightness);
            SyncHexFromColor();
            RegenerateSvTexture();
        }
    }

    /// <summary>Fired whenever the color changes during drag or hex input.</summary>
    public event Action<Color>? ColorChanged;

    /// <summary>Fired when the user releases the drag handle or commits the hex input.</summary>
    public event Action<Color>? ColorCommitted;

    #endregion

    #region Constructor

    /// <summary>Creates a ColorPickerRGB. Pass a GraphicsDevice to generate gradient textures on layout.</summary>
    public ColorPickerRGB(GraphicsDevice? graphicsDevice, SpriteFont? font, Texture2D? pixel)
    {
        _graphicsDevice = graphicsDevice;
        Pixel = pixel;

        _hexInput = new TextBox(font, pixel, null) { MaxLength = 7 };
        _hexInput.TextChanged += OnHexChanged;
        Add(_hexInput);

        _selectedColor = Color.Red;
        ColorPickerUtils.RgbToHsv(_selectedColor, out _hue, out _saturation, out _brightness);
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int w = Math.Min((int)availableSize.X, MaxWidth);
        int swatchW = (int)(w * SwatchFraction);
        int svW = w - swatchW - Spacing;
        int h = svW + Spacing + HueBarHeight + Spacing + HexBoxHeight;
        DesiredSize = new Vector2(w, h);
    }

    /// <inheritdoc/>
    public override void Arrange(Rectangle finalBounds)
    {
        int w = Math.Min(finalBounds.Width, MaxWidth);
        int x = finalBounds.X + (finalBounds.Width - w) / 2;
        Bounds = new Rectangle(x, finalBounds.Y, w, (int)DesiredSize.Y);

        int y = finalBounds.Y;
        int swatchW = (int)(w * SwatchFraction);
        int svW = w - swatchW - Spacing;

        _swatchBounds = new Rectangle(x, y, swatchW, svW);
        _svSquareBounds = new Rectangle(x + swatchW + Spacing, y, svW, svW);
        y += svW + Spacing;

        _hueBarBounds = new Rectangle(x, y, w, HueBarHeight);
        y += HueBarHeight + Spacing;

        _hexInput.Arrange(new Rectangle(x, y, w, HexBoxHeight));

        GenerateHueBarTexture();
        RegenerateSvTexture();
        GenerateThumbTextures();
        SyncHexFromColor();
    }

    #endregion

    #region Texture generation

    private void GenerateHueBarTexture()
    {
        if (_graphicsDevice is null || _hueBarBounds.Width <= 0 || _hueBarBounds.Height <= 0) return;
        if (_hueBarTexture is not null && _lastHueBarBounds == _hueBarBounds) return;

        int w = _hueBarBounds.Width;
        int h = _hueBarBounds.Height;
        if (_hueBarTexture is null || _hueBarTexture.Width != w || _hueBarTexture.Height != h)
        {
            _hueBarTexture?.Dispose();
            _hueBarTexture = new Texture2D(_graphicsDevice, w, h);
            _hueBarData = new Color[w * h];
        }

        for (int col = 0; col < w; col++)
        {
            Color c = ColorPickerUtils.HsvToRgb(col / (float)w * 360f, 1f, 1f);
            for (int row = 0; row < h; row++)
                _hueBarData![row * w + col] = c;
        }

        _hueBarTexture.SetData(_hueBarData);
        _lastHueBarBounds = _hueBarBounds;
    }

    private void RegenerateSvTexture()
    {
        if (_graphicsDevice is null || _svSquareBounds.Width <= 0 || _svSquareBounds.Height <= 0) return;

        int w = _svSquareBounds.Width;
        int h = _svSquareBounds.Height;
        bool sizeChanged = _svSquareTexture is null || _svSquareTexture.Width != w || _svSquareTexture.Height != h;
        bool contentChanged = _lastSvHue < 0f || _hue != _lastSvHue || _lastSvBounds != _svSquareBounds;
        if (!sizeChanged && !contentChanged) return;

        if (sizeChanged)
        {
            _svSquareTexture?.Dispose();
            _svSquareTexture = new Texture2D(_graphicsDevice, w, h);
            _svData = new Color[w * h];
        }

        for (int row = 0; row < h; row++)
        {
            float v = 1f - row / (float)(h - 1);
            for (int col = 0; col < w; col++)
            {
                float s = col / (float)(w - 1);
                _svData![row * w + col] = ColorPickerUtils.HsvToRgb(_hue, s, v);
            }
        }

        _svSquareTexture!.SetData(_svData);
        _lastSvHue = _hue;
        _lastSvBounds = _svSquareBounds;
    }

    private void GenerateThumbTextures()
    {
        if (_graphicsDevice is null) return;
        if (_thumbTexture is not null && _ringTexture is not null) return;

        // Hue-bar thumb: white filled circle with black outer border.
        _thumbTexture?.Dispose();
        _thumbTexture = new Texture2D(_graphicsDevice, ThumbSize, ThumbSize);
        Color[] td = new Color[ThumbSize * ThumbSize];
        float tr = ThumbSize / 2f;
        for (int py = 0; py < ThumbSize; py++)
        for (int px = 0; px < ThumbSize; px++)
        {
            float dx = px - tr + 0.5f, dy = py - tr + 0.5f;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            td[py * ThumbSize + px] = dist <= tr - 1.5f ? Color.White
                : dist <= tr ? Color.Black
                : Color.Transparent;
        }
        _thumbTexture.SetData(td);

        // SV crosshair: white ring with black inner and outer border for visibility on any color.
        _ringTexture?.Dispose();
        _ringTexture = new Texture2D(_graphicsDevice, RingDiameter, RingDiameter);
        Color[] rd = new Color[RingDiameter * RingDiameter];
        float rr = RingDiameter / 2f;
        for (int py = 0; py < RingDiameter; py++)
        for (int px = 0; px < RingDiameter; px++)
        {
            float dx = px - rr + 0.5f, dy = py - rr + 0.5f;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            Color c;
            if (dist < rr - 3.5f)      c = Color.Transparent;
            else if (dist < rr - 2.5f) c = Color.Black;
            else if (dist < rr - 0.5f) c = Color.White;
            else if (dist < rr + 0.5f) c = Color.Black;
            else                        c = Color.Transparent;
            rd[py * RingDiameter + px] = c;
        }
        _ringTexture.SetData(rd);
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;
        base.Update(gameTime);

        MouseState ms = Mouse.GetState();
        Point mousePos = ms.Position;
        bool held = ms.LeftButton == ButtonState.Pressed;

        if (_draggingHue)
        {
            if (!held)
            {
                _draggingHue = false;
                ColorCommitted?.Invoke(_selectedColor);
            }
            else
            {
                ApplyHueDrag(mousePos.X);
            }
        }
        else if (_draggingSv)
        {
            if (!held)
            {
                _draggingSv = false;
                ColorCommitted?.Invoke(_selectedColor);
            }
            else
            {
                ApplySvDrag(mousePos);
            }
        }
        else if (ms.LeftButton == ButtonState.Pressed)
        {
            if (_hueBarBounds.Contains(mousePos))
            {
                _draggingHue = true;
                ApplyHueDrag(mousePos.X);
            }
            else if (_svSquareBounds.Contains(mousePos))
            {
                _draggingSv = true;
                ApplySvDrag(mousePos);
            }
        }
    }

    private void ApplyHueDrag(int screenX)
    {
        if (_hueBarBounds.Width <= 0) return;
        float t = MathHelper.Clamp((screenX - _hueBarBounds.X) / (float)_hueBarBounds.Width, 0f, 1f);
        _hue = t * 360f;
        RebuildColor();
        RegenerateSvTexture();
    }

    private void ApplySvDrag(Point mousePos)
    {
        if (_svSquareBounds.Width <= 0 || _svSquareBounds.Height <= 0) return;
        _saturation = MathHelper.Clamp((mousePos.X - _svSquareBounds.X) / (float)_svSquareBounds.Width, 0f, 1f);
        _brightness = 1f - MathHelper.Clamp((mousePos.Y - _svSquareBounds.Y) / (float)_svSquareBounds.Height, 0f, 1f);
        RebuildColor();
    }

    private void RebuildColor()
    {
        _selectedColor = ColorPickerUtils.HsvToRgb(_hue, _saturation, _brightness);
        SyncHexFromColor();
        ColorChanged?.Invoke(_selectedColor);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        float opacity = EffectiveOpacity;

        // Swatch
        if (Pixel is not null)
        {
            spriteBatch.Draw(Pixel, _swatchBounds, _selectedColor * opacity);
            DrawHelper.DrawBorder(Pixel, spriteBatch, _swatchBounds, BorderColor * opacity, 1);
        }

        // SV square
        if (_svSquareTexture is not null)
            spriteBatch.Draw(_svSquareTexture, _svSquareBounds, Color.White * opacity);

        if (Pixel is not null)
            DrawHelper.DrawBorder(Pixel, spriteBatch, _svSquareBounds, BorderColor * opacity, 1);

        // SV crosshair ring
        if (_ringTexture is not null)
        {
            int crossX = _svSquareBounds.X + (int)(_saturation * (_svSquareBounds.Width - 1));
            int crossY = _svSquareBounds.Y + (int)((1f - _brightness) * (_svSquareBounds.Height - 1));
            int half = RingDiameter / 2;
            spriteBatch.Draw(_ringTexture,
                new Rectangle(crossX - half, crossY - half, RingDiameter, RingDiameter),
                Color.White * opacity);
        }

        // Hue bar
        if (_hueBarTexture is not null)
            spriteBatch.Draw(_hueBarTexture, _hueBarBounds, Color.White * opacity);

        if (Pixel is not null)
            DrawHelper.DrawBorder(Pixel, spriteBatch, _hueBarBounds, BorderColor * opacity, 1);

        // Hue thumb circle
        if (_thumbTexture is not null)
        {
            int thumbX = _hueBarBounds.X + (int)(_hue / 360f * _hueBarBounds.Width);
            int thumbY = _hueBarBounds.Y + _hueBarBounds.Height / 2;
            int half = ThumbSize / 2;
            spriteBatch.Draw(_thumbTexture,
                new Rectangle(thumbX - half, thumbY - half, ThumbSize, ThumbSize),
                Color.White * opacity);
        }

        // Children (hex input)
        for (int i = 0; i < ChildrenReadOnly.Count; i++)
        {
            if (ChildrenReadOnly[i].IsVisible)
                ChildrenReadOnly[i].Draw(spriteBatch);
        }
    }

    #endregion

    #region Hex sync

    private void SyncHexFromColor()
    {
        string hex = ColorPickerUtils.ColorToHex(_selectedColor);
        if (_hexInput.Text == hex) return;
        _suppressHexCallback = true;
        _hexInput.SetText(hex);
        _suppressHexCallback = false;
    }

    private void OnHexChanged(string text)
    {
        if (_suppressHexCallback) return;
        Color? parsed = ColorPickerUtils.HexToColor(text);
        if (parsed is null) return;
        _selectedColor = parsed.Value;
        ColorPickerUtils.RgbToHsv(_selectedColor, out _hue, out _saturation, out _brightness);
        RegenerateSvTexture();
        ColorChanged?.Invoke(_selectedColor);
        ColorCommitted?.Invoke(_selectedColor);
    }

    #endregion

    #region Dispose

    /// <summary>Releases generated gradient textures.</summary>
    public void Dispose()
    {
        _hueBarTexture?.Dispose();
        _svSquareTexture?.Dispose();
        _thumbTexture?.Dispose();
        _ringTexture?.Dispose();
    }

    #endregion
}
