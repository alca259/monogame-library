namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>Displays a fill bar representing a normalized value between 0 and 1.</summary>
public sealed class ProgressBar : UIElement
{
    private const float MinValue = 0f;
    private const float MaxValue = 1f;
    private const float GradientMidPoint = 0.5f;
    private const float GradientScale = 2f;
    private const int DefaultWidth = 200;
    private const int DefaultHeight = 20;

    private float _value = 1f;

    /// <summary>Normalized fill level, clamped to [0, 1].</summary>
    public float Value
    {
        get => _value;
        set => _value = MathHelper.Clamp(value, MinValue, MaxValue);
    }

    /// <summary>Color of the fill region. Ignored when <see cref="ColorGradient"/> is true.</summary>
    public Color FillColor { get; set; } = Color.Green;

    /// <summary>Color of the empty background region.</summary>
    public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

    /// <summary>Pixel texture used for drawing solid-color rectangles.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Layout direction of the bar.</summary>
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    /// <summary>
    /// When true, the fill color interpolates through Red → Yellow → Green as Value increases.
    /// Overrides <see cref="FillColor"/>.
    /// </summary>
    public bool ColorGradient { get; set; }

    /// <summary>Low-value color used for gradient interpolation (default Red).</summary>
    public Color LowColor { get; set; } = Color.Red;

    /// <summary>High-value color used for gradient interpolation (default Green).</summary>
    public Color HighColor { get; set; } = Color.Green;

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        DesiredSize = Orientation == Orientation.Vertical
            ? new Vector2(DefaultHeight, DefaultWidth)
            : new Vector2(DefaultWidth, DefaultHeight);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || Pixel is null) return;

        float opacity = EffectiveOpacity;

        spriteBatch.Draw(Pixel, Bounds, BackgroundColor * opacity);

        Color barColor = ColorGradient ? ComputeGradientColor(_value) : FillColor;

        if (Orientation == Orientation.Vertical)
        {
            int fillHeight = (int)(_value * Bounds.Height);
            if (fillHeight > 0)
            {
                var fillRect = new Rectangle(Bounds.X, Bounds.Bottom - fillHeight, Bounds.Width, fillHeight);
                spriteBatch.Draw(Pixel, fillRect, barColor * opacity);
            }
        }
        else
        {
            int fillWidth = (int)(_value * Bounds.Width);
            if (fillWidth > 0)
            {
                var fillRect = new Rectangle(Bounds.X, Bounds.Y, fillWidth, Bounds.Height);
                spriteBatch.Draw(Pixel, fillRect, barColor * opacity);
            }
        }
    }

    private Color ComputeGradientColor(float t)
    {
        if (t > GradientMidPoint)
        {
            // LowColor→HighColor upper half uses Yellow as mid-point
            Color mid = Color.Yellow;
            return Color.Lerp(mid, HighColor, (t - GradientMidPoint) * GradientScale);
        }

        return Color.Lerp(LowColor, Color.Yellow, t * GradientScale);
    }
}
