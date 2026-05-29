using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>A pressable button with hover/press animation and focus support.</summary>
public sealed class Button : UIElement, IUIInteractable, IFocusable
{
    #region Constants

    private const float ScaleNormal = 1.0f;
    private const float ScaleHovered = 1.05f;
    private const float ScalePressed = 0.97f;
    private const float ScaleLerpSpeed = 12f;
    private const int PaddingH = 8;
    private const int PaddingV = 4;

    private const int StateNormal = 0;
    private const int StateHovered = 1;
    private const int StatePressed = 2;

    #endregion

    #region Fields

    private readonly SpriteFont? _font;
    private readonly string _text;
    private Vector2 _textSize;

    /// <summary>Pre-allocated color array indexed by button state (Normal=0, Hovered=1, Pressed=2).</summary>
    private readonly Color[] _stateColors = new Color[3];

    /// <summary>Pre-allocated text color array indexed by button state (Normal=0, Hovered=1, Pressed=2).</summary>
    private readonly Color[] _textColors = new Color[3];

    private float _currentScale = ScaleNormal;
    private float _targetScale = ScaleNormal;
    private int _state = StateNormal;

    // IFocusable backing fields
    private bool _isFocused;

    #endregion

    #region Properties

    /// <summary>Color used when the button is in its normal state.</summary>
    public Color NormalColor
    {
        get => _stateColors[StateNormal];
        set => _stateColors[StateNormal] = value;
    }

    /// <summary>Color used when the pointer is over the button.</summary>
    public Color HoveredColor
    {
        get => _stateColors[StateHovered];
        set => _stateColors[StateHovered] = value;
    }

    /// <summary>Color used when the button is being pressed.</summary>
    public Color PressedColor
    {
        get => _stateColors[StatePressed];
        set => _stateColors[StatePressed] = value;
    }

    /// <summary>Color used when the button is disabled.</summary>
    public Color DisabledColor { get; set; } = Color.Gray;

    /// <summary>Text color used when the button is in its normal state.</summary>
    public Color NormalTextColor
    {
        get => _textColors[StateNormal];
        set => _textColors[StateNormal] = value;
    }

    /// <summary>Text color used when the pointer is over the button.</summary>
    public Color HoveredTextColor
    {
        get => _textColors[StateHovered];
        set => _textColors[StateHovered] = value;
    }

    /// <summary>Text color used when the button is being pressed.</summary>
    public Color PressedTextColor
    {
        get => _textColors[StatePressed];
        set => _textColors[StatePressed] = value;
    }

    /// <summary>Text color used when the button is disabled.</summary>
    public Color DisabledTextColor { get; set; } = Color.DarkGray;

    /// <summary>
    /// Fixed size override. When set, <see cref="Measure"/> returns this value instead of computing from text + padding.
    /// </summary>
    public Vector2? FixedSize { get; set; }

    /// <summary>Horizontal alignment of the label text within the button bounds.</summary>
    public HAlign HAlign { get; set; } = HAlign.Center;

    /// <summary>
    /// A 1×1 white pixel texture used to fill the button background with the active state color.
    /// When set, the background is drawn even if <see cref="Texture"/> is null.
    /// </summary>
    public Texture2D? BackgroundPixel { get; set; }

    /// <summary>Optional sprite texture drawn on top of the background. When null, only the solid background is used.</summary>
    public Texture2D? Texture { get; set; }

    /// <summary>Optional nine-slice texture drawn as the button background. When set, replaces <see cref="BackgroundPixel"/>.</summary>
    public Texture2D? NineSliceTexture { get; set; }

    /// <summary>Border insets used to divide <see cref="NineSliceTexture"/> into a 3×3 grid.</summary>
    public NineSliceBorderData NineSliceBorder { get; set; } = NineSliceBorderData.Uniform(8);

    /// <summary>Fired when the button is clicked (pointer up while hovered).</summary>
    public event Action? Clicked;

    #endregion

    #region Constructor

    /// <summary>Creates a new Button.</summary>
    /// <param name="font">Font for the label text. Can be null for an icon-only button.</param>
    /// <param name="text">Label text.</param>
    /// <param name="backgroundTexture">Optional background texture for the button. If null, a default pixel texture is used.</param>
    public Button(SpriteFont? font, string text, Texture2D? backgroundTexture = null)
    {
        _font = font;
        _text = text;
        _textSize = font is not null ? font.MeasureString(text) : Vector2.Zero;

        _stateColors[StateNormal] = Color.White;
        _stateColors[StateHovered] = Color.LightYellow;
        _stateColors[StatePressed] = new Color(180, 180, 180);

        _textColors[StateNormal] = Color.Black;
        _textColors[StateHovered] = Color.Black;
        _textColors[StatePressed] = Color.Black;

        BackgroundPixel = backgroundTexture ?? DrawHelper.DefaultPixelTexture;
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        DesiredSize = FixedSize ?? new Vector2(_textSize.X + PaddingH, _textSize.Y + PaddingV);
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;

        _targetScale = _state switch
        {
            StatePressed => ScalePressed,
            StateHovered => ScaleHovered,
            _ => ScaleNormal,
        };

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _currentScale += (_targetScale - _currentScale) * ScaleLerpSpeed * dt;
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        Color tint = !IsEnabled ? DisabledColor : _stateColors[_state];
        tint *= EffectiveOpacity;

        Vector2 center = Bounds.Center.ToVector2();

        if (NineSliceTexture is not null)
        {
            int hw = (int)(Bounds.Width  * _currentScale / 2);
            int hh = (int)(Bounds.Height * _currentScale / 2);
            Rectangle scaledBounds = new((int)center.X - hw, (int)center.Y - hh, hw * 2, hh * 2);
            DrawHelper.DrawNineSlice(spriteBatch, NineSliceTexture, scaledBounds, NineSliceBorder, tint);
        }
        else if (BackgroundPixel is not null)
        {
            Vector2 bgScale = new Vector2(Bounds.Width * _currentScale, Bounds.Height * _currentScale);
            spriteBatch.Draw(BackgroundPixel, center, null, tint, 0f, new Vector2(0.5f, 0.5f), bgScale, SpriteEffects.None, 0f);
        }

        if (Texture is not null)
        {
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, center, null, tint, 0f, origin, _currentScale, SpriteEffects.None, 0f);
        }

        if (_font is not null && _text.Length > 0)
        {
            Color textColor = !IsEnabled ? DisabledTextColor : _textColors[_state];
            float textX = HAlign switch
            {
                HAlign.Left  => Bounds.X + PaddingH / 2f,
                HAlign.Right => Bounds.Right - _textSize.X * _currentScale - PaddingH / 2f,
                _            => center.X - _textSize.X / 2f * _currentScale,
            };
            spriteBatch.DrawString(
                spriteFont: _font,
                text: _text,
                position: new Vector2(textX, center.Y - _textSize.Y / 2f * _currentScale),
                color: textColor * EffectiveOpacity,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: _currentScale,
                effects: SpriteEffects.None,
                layerDepth: 0f);
        }
    }

    #endregion

    #region IUIInteractable

    /// <inheritdoc/>
    public bool IsHovered => _state is StateHovered or StatePressed;

    /// <inheritdoc/>
    public void OnPointerEnter()
    {
        if (_state == StateNormal)
            _state = StateHovered;
    }

    /// <inheritdoc/>
    public void OnPointerLeave()
    {
        _state = StateNormal;
    }

    /// <inheritdoc/>
    public void OnPointerDown(ref UIPointerEventArgs args)
    {
        if (!IsEnabled) return;
        _state = StatePressed;
    }

    /// <inheritdoc/>
    public void OnPointerUp(ref UIPointerEventArgs args)
    {
        if (!IsEnabled) return;

        bool wasPressed = _state == StatePressed;
        _state = StateHovered;

        if (wasPressed)
        {
            Clicked?.Invoke();
            args.Handled = true;
        }
    }

    #endregion

    #region IFocusable

    /// <inheritdoc/>
    public int TabIndex { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborUp { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborDown { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborLeft { get; set; }

    /// <inheritdoc/>
    public int? FocusNeighborRight { get; set; }

    /// <inheritdoc/>
    public bool IsFocused => _isFocused;

    /// <inheritdoc/>
    public void OnFocusGained()
    {
        _isFocused = true;
        if (_state == StateNormal) _state = StateHovered;
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        _state = StateNormal;
    }

    #endregion
}
