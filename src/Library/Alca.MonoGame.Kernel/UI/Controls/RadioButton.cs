using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>A single-choice radio button; selection is managed exclusively by its <see cref="RadioGroup"/>.</summary>
public sealed class RadioButton : UIElement, IUIInteractable, IFocusable
{
    #region Constants

    private const int DefaultCircleSize = 20;
    private const int LabelSpacing = 8;
    private const int InnerInset = 5;

    #endregion

    #region Fields

    private readonly RadioGroup _group;
    private bool _isSelected;
    private bool _isHovered;
    private bool _isFocused;

    #endregion

    #region Properties

    /// <summary>Font used to draw the label text.</summary>
    public SpriteFont? Font { get; set; }

    /// <summary>Label text displayed to the right of the circle.</summary>
    public string Label { get; set; }

    /// <summary>1×1 white pixel for drawing circles and borders.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Size in pixels of the outer circle.</summary>
    public int CircleSize { get; set; } = DefaultCircleSize;

    /// <summary>Color of the outer circle border.</summary>
    public Color CircleColor { get; set; } = Color.White;

    /// <summary>Color of the inner filled dot when selected.</summary>
    public Color DotColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Label text color.</summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>Focus border color.</summary>
    public Color FocusBorderColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Whether this button is currently selected. Set only by <see cref="RadioGroup"/>.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        internal set => _isSelected = value;
    }

    /// <summary>The group this button belongs to.</summary>
    public RadioGroup Group => _group;

    #endregion

    #region Constructor

    /// <summary>Creates a RadioButton and registers it with the given group.</summary>
    public RadioButton(SpriteFont? font, Texture2D? pixel, string label, RadioGroup group)
    {
        Font = font;
        Pixel = pixel;
        Label = label;
        _group = group;
        _group.Register(this);
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        Vector2 labelSize = Font is not null ? Font.MeasureString(Label) : Vector2.Zero;
        int w = CircleSize + LabelSpacing + (int)labelSize.X;
        int h = Math.Max(CircleSize, (int)labelSize.Y);
        DesiredSize = new Vector2(w, h);
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime) { }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || Pixel is null) return;

        float opacity = EffectiveOpacity;
        int circleY = Bounds.Y + (Bounds.Height - CircleSize) / 2;
        var outerRect = new Rectangle(Bounds.X, circleY, CircleSize, CircleSize);

        // Outer circle (border)
        DrawHelper.DrawBorder(Pixel, spriteBatch, outerRect, CircleColor * opacity, 2);

        // Inner dot when selected
        if (_isSelected)
        {
            int inset = InnerInset;
            var innerRect = new Rectangle(
                outerRect.X + inset,
                outerRect.Y + inset,
                outerRect.Width - inset * 2,
                outerRect.Height - inset * 2);
            spriteBatch.Draw(Pixel, innerRect, DotColor * opacity);
        }

        // Focus highlight
        if (_isFocused)
            DrawHelper.DrawBorder(Pixel, spriteBatch,
                new Rectangle(outerRect.X - 2, outerRect.Y - 2, outerRect.Width + 4, outerRect.Height + 4),
                FocusBorderColor * opacity, 1);

        // Label
        if (Font is not null && Label.Length > 0)
        {
            Vector2 labelSize = Font.MeasureString(Label);
            float labelY = Bounds.Y + (Bounds.Height - labelSize.Y) / 2f;
            spriteBatch.DrawString(Font, Label,
                new Vector2(Bounds.X + CircleSize + LabelSpacing, labelY),
                LabelColor * opacity);
        }
    }

    #endregion

    #region IUIInteractable

    /// <inheritdoc/>
    public bool IsHovered => _isHovered;

    /// <inheritdoc/>
    public void OnPointerEnter() => _isHovered = true;

    /// <inheritdoc/>
    public void OnPointerLeave() => _isHovered = false;

    /// <inheritdoc/>
    public void OnPointerDown(ref UIPointerEventArgs args) { }

    /// <inheritdoc/>
    public void OnPointerUp(ref UIPointerEventArgs args)
    {
        if (!IsEnabled) return;
        _group.Select(this);
        args.Handled = true;
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
        _isHovered = true;
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        _isHovered = false;
    }

    #endregion
}
