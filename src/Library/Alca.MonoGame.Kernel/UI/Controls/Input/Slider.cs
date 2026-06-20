using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Focus;
using Alca.MonoGame.Kernel.UI.Input;
using Alca.MonoGame.Kernel.UI.Interaction;

namespace Alca.MonoGame.Kernel.UI.Controls.Input;

/// <summary>A draggable slider that lets the user pick a float value in a [MinValue, MaxValue] range.</summary>
public sealed class Slider : UIElement, IUIInteractable, IFocusable
{
    #region Fields

    private bool _isDragging;
    private bool _isFocused;
    private bool _isHovered;
    private float _value;

    private Rectangle _trackRect;
    private Rectangle _thumbVisualRect;

    #endregion

    #region Properties

    /// <summary>1×1 white pixel texture used for rendering track, fill, and thumb.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Minimum selectable value.</summary>
    public float MinValue { get; set; } = 0f;

    /// <summary>Maximum selectable value.</summary>
    public float MaxValue { get; set; } = 1f;

    /// <summary>Snap increment. 0 = continuous float.</summary>
    public float Step { get; set; } = 0f;

    /// <summary>Layout orientation of the track.</summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>Pixel thickness of the track bar.</summary>
    public int TrackThickness { get; set; } = 6;

    /// <summary>Size in pixels of the draggable thumb square.</summary>
    public int ThumbSize { get; set; } = 18;

    /// <summary>Color of the unfilled portion of the track.</summary>
    public Color TrackColor { get; set; } = new Color(60, 60, 60);

    /// <summary>Color of the filled portion of the track (from Min to current value).</summary>
    public Color FillColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Color of the thumb handle.</summary>
    public Color ThumbColor { get; set; } = Color.White;

    /// <summary>Tint applied to the thumb and focus border when the element is focused.</summary>
    public Color FocusBorderColor { get; set; } = new Color(100, 149, 237);

    /// <summary>Current value; clamped to [MinValue, MaxValue] and snapped to Step if Step > 0.</summary>
    public float Value
    {
        get => _value;
        set
        {
            float clamped = MathHelper.Clamp(value, MinValue, MaxValue);
            float snapped = Step > 0f ? MathF.Round(clamped / Step) * Step : clamped;
            if (MathF.Abs(snapped - _value) < 0.0001f) return;
            _value = snapped;
            UpdateThumbRects();
            ValueChanged?.Invoke(_value);
        }
    }

    /// <summary>Fired whenever Value changes due to drag, keyboard, or gamepad input.</summary>
    public event Action<float>? ValueChanged;

    #endregion

    #region Constructor

    /// <summary>Creates a new Slider.</summary>
    /// <param name="pixel">1×1 white pixel texture for rendering. May be null (no visuals drawn).</param>
    public Slider(Texture2D? pixel)
    {
        Pixel = pixel;
    }

    #endregion

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int slab = Math.Max(ThumbSize, TrackThickness) + 4;
        DesiredSize = Orientation == Orientation.Horizontal
            ? new Vector2(availableSize.X, slab)
            : new Vector2(slab, availableSize.Y);
    }

    /// <inheritdoc/>
    public override void Arrange(Rectangle finalBounds)
    {
        base.Arrange(finalBounds);
        RebuildTrackGeometry();
        UpdateThumbRects();
    }

    private void RebuildTrackGeometry()
    {
        if (Orientation == Orientation.Horizontal)
        {
            int trackY = Bounds.Y + (Bounds.Height - TrackThickness) / 2;
            _trackRect = new Rectangle(
                Bounds.X + ThumbSize / 2,
                trackY,
                Bounds.Width - ThumbSize,
                TrackThickness);
        }
        else
        {
            int trackX = Bounds.X + (Bounds.Width - TrackThickness) / 2;
            _trackRect = new Rectangle(
                trackX,
                Bounds.Y + ThumbSize / 2,
                TrackThickness,
                Bounds.Height - ThumbSize);
        }
    }

    private void UpdateThumbRects()
    {
        int center = GetThumbCenter();

        if (Orientation == Orientation.Horizontal)
        {
            _thumbVisualRect = new Rectangle(
                center - ThumbSize / 2,
                Bounds.Y + (Bounds.Height - ThumbSize) / 2,
                ThumbSize, ThumbSize);
        }
        else
        {
            _thumbVisualRect = new Rectangle(
                Bounds.X + (Bounds.Width - ThumbSize) / 2,
                center - ThumbSize / 2,
                ThumbSize, ThumbSize);
        }
    }

    private int GetThumbCenter()
    {
        float t = (MaxValue > MinValue) ? (_value - MinValue) / (MaxValue - MinValue) : 0f;
        return Orientation == Orientation.Horizontal
            ? _trackRect.X + (int)(t * _trackRect.Width)
            : _trackRect.Y + (int)((1f - t) * _trackRect.Height);
    }

    #endregion

    #region Value helpers

    private void SetValueFromPosition(int screenX, int screenY)
    {
        if (Orientation == Orientation.Horizontal)
        {
            float t = MathHelper.Clamp(
                (_trackRect.Width > 0) ? (screenX - _trackRect.X) / (float)_trackRect.Width : 0f,
                0f, 1f);
            Value = MinValue + t * (MaxValue - MinValue);
        }
        else
        {
            float t = 1f - MathHelper.Clamp(
                (_trackRect.Height > 0) ? (screenY - _trackRect.Y) / (float)_trackRect.Height : 0f,
                0f, 1f);
            Value = MinValue + t * (MaxValue - MinValue);
        }
    }

    #endregion

    #region Update / Draw

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;

        var input = UIInputContext.Current!;

        if (_isDragging)
        {
            if (!input.IsPointerButtonPressed)
            {
                _isDragging = false;
            }
            else if (input.PointerPosition is not null)
            {
                SetValueFromPosition(input.PointerPosition.Value.X, input.PointerPosition.Value.Y);
                UpdateThumbRects();
            }
        }

        if (_isFocused)
        {
            float step = Step > 0f ? Step : (MaxValue - MinValue) * 0.01f;

            if (Orientation == Orientation.Horizontal)
            {
                if (input.MoveLeft?.IsPressed == true)
                    Value -= step;
                else if (input.MoveRight?.IsPressed == true)
                    Value += step;
            }
            else
            {
                if (input.MoveDown?.IsPressed == true)
                    Value -= step;
                else if (input.MoveUp?.IsPressed == true)
                    Value += step;
            }

            if (input.Home?.IsPressed == true)
                Value = MinValue;
            else if (input.End?.IsPressed == true)
                Value = MaxValue;

            UpdateThumbRects();
        }
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || Pixel is null) return;

        float opacity = EffectiveOpacity;

        spriteBatch.Draw(Pixel, _trackRect, TrackColor * opacity);

        int thumbCenter = GetThumbCenter();
        if (Orientation == Orientation.Horizontal)
        {
            int fillW = thumbCenter - _trackRect.X;
            if (fillW > 0)
                spriteBatch.Draw(Pixel, new Rectangle(_trackRect.X, _trackRect.Y, fillW, _trackRect.Height), FillColor * opacity);
        }
        else
        {
            int fillH = _trackRect.Bottom - thumbCenter;
            if (fillH > 0)
                spriteBatch.Draw(Pixel, new Rectangle(_trackRect.X, thumbCenter, _trackRect.Width, fillH), FillColor * opacity);
        }

        Color thumbTint = _isFocused ? FocusBorderColor : ThumbColor;
        spriteBatch.Draw(Pixel, _thumbVisualRect, thumbTint * opacity);

        if (_isFocused)
            DrawHelper.DrawBorder(Pixel, spriteBatch, Bounds, FocusBorderColor * opacity, 1);
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
    public void OnPointerDown(ref UIPointerEventArgs args)
    {
        if (!IsEnabled) return;
        _isDragging = true;
        SetValueFromPosition(args.Position.X, args.Position.Y);
        UpdateThumbRects();
        args.Handled = true;
    }

    /// <inheritdoc/>
    public void OnPointerUp(ref UIPointerEventArgs args)
    {
        if (!_isDragging) return;
        _isDragging = false;
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
    }

    /// <inheritdoc/>
    public void OnFocusLost()
    {
        _isFocused = false;
        _isDragging = false;
    }

    #endregion
}
