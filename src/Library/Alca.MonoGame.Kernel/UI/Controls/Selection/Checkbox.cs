using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls.Selection;

/// <summary>A toggle control with a checked/unchecked visual state.</summary>
public sealed class Checkbox : UIElement, IUIInteractable, IFocusable
{
    #region Constants

    private const int DefaultBoxSize = 20;
    private const int LabelSpacing = 6;
    private const int CheckInset = 4;

    #endregion

    #region Fields

    private readonly SpriteFont? _font;
    private readonly string _label;
    private readonly Vector2 _labelSize;
    private bool _isChecked;
    private bool _isHovered;
    private bool _isFocused;

    #endregion

    #region Properties

    /// <summary>Size of the checkbox box in pixels.</summary>
    public int BoxSize { get; set; } = DefaultBoxSize;

    /// <summary>Box outline and fill color when unchecked.</summary>
    public Color BoxColor { get; set; } = Color.White;

    /// <summary>Fill color of the inner check mark when checked.</summary>
    public Color CheckColor { get; set; } = Color.LimeGreen;

    /// <summary>Label text color.</summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>Optional texture used for the checkbox background (replaces solid box).</summary>
    public Texture2D? BoxTexture { get; set; }

    /// <summary>Optional texture drawn on top when checked (replaces solid fill).</summary>
    public Texture2D? CheckTexture { get; set; }

    /// <summary>Pixel texture for drawing solid-color rectangles.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Whether the checkbox is currently in the checked state.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            CheckedChanged?.Invoke(_isChecked);
        }
    }

    /// <summary>Fires when <see cref="IsChecked"/> changes, passing the new value.</summary>
    public event Action<bool>? CheckedChanged;

    #endregion

    #region Constructor

    /// <summary>Creates a Checkbox with an optional label.</summary>
    public Checkbox(SpriteFont? font, string label)
    {
        _font = font;
        _label = label;
        _labelSize = font is not null ? font.MeasureString(label) : Vector2.Zero;
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int w = BoxSize + LabelSpacing + (int)_labelSize.X;
        int h = Math.Max(BoxSize, (int)_labelSize.Y);
        DesiredSize = new Vector2(w, h);
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        // Focus keyboard toggle is handled via IFocusable — space key is managed by the game layer
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        float opacity = EffectiveOpacity;
        int boxY = Bounds.Y + (Bounds.Height - BoxSize) / 2;
        var boxRect = new Rectangle(Bounds.X, boxY, BoxSize, BoxSize);

        // Box background
        if (BoxTexture is not null)
            spriteBatch.Draw(BoxTexture, boxRect, BoxColor * opacity);
        else if (Pixel is not null)
            spriteBatch.Draw(Pixel, boxRect, BoxColor * opacity);

        // Checkmark
        if (_isChecked)
        {
            if (CheckTexture is not null)
            {
                spriteBatch.Draw(CheckTexture, boxRect, CheckColor * opacity);
            }
            else if (Pixel is not null)
            {
                var innerRect = new Rectangle(
                    boxRect.X + CheckInset, boxRect.Y + CheckInset,
                    boxRect.Width - CheckInset * 2, boxRect.Height - CheckInset * 2);
                spriteBatch.Draw(Pixel, innerRect, CheckColor * opacity);
            }
        }

        // Focus indicator
        if (_isFocused && Pixel is not null)
            spriteBatch.Draw(Pixel, new Rectangle(boxRect.X - 1, boxRect.Y - 1, BoxSize + 2, BoxSize + 2),
                Color.Yellow * (opacity * 0.5f));

        // Label
        if (_font is not null && _label.Length > 0)
        {
            float labelY = Bounds.Y + (Bounds.Height - _labelSize.Y) / 2f;
            spriteBatch.DrawString(_font, _label,
                new Vector2(Bounds.X + BoxSize + LabelSpacing, labelY),
                LabelColor * opacity);
        }
    }

    /// <summary>Toggles <see cref="IsChecked"/>. Call from the focus/keyboard system when Space is pressed.</summary>
    public void Toggle() => IsChecked = !IsChecked;

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
        Toggle();
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
